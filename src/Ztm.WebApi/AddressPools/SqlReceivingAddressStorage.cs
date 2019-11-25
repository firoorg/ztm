using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NBitcoin;
using Ztm.Data.Entity.Contexts;

namespace Ztm.WebApi.AddressPools
{
    using ReceivingAddressModel = Ztm.Data.Entity.Contexts.Main.ReceivingAddress;
    using ReceivingAddressReservationModel = Ztm.Data.Entity.Contexts.Main.ReceivingAddressReservation;

    public sealed class SqlReceivingAddressStorage : IReceivingAddressStorage
    {
        readonly IMainDatabaseFactory databaseFactory;
        readonly Network network;

        public SqlReceivingAddressStorage(IMainDatabaseFactory databaseFactory, Network network)
        {
            if (databaseFactory == null)
            {
                throw new ArgumentNullException(nameof(databaseFactory));
            }

            if (network == null)
            {
                throw new ArgumentNullException(nameof(network));
            }

            this.databaseFactory = databaseFactory;
            this.network = network;
        }

        public async Task<ReceivingAddress> AddAddressAsync(BitcoinAddress address, CancellationToken cancellationToken)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            using (var db = this.databaseFactory.CreateDbContext())
            {
                var recAddress = await db.AddAsync<ReceivingAddressModel>(
                    new ReceivingAddressModel
                    {
                        Address = address.ToString(),
                        IsLocked = false
                    },
                    cancellationToken
                );

                await db.SaveChangesAsync(cancellationToken);

                return ToDomain(recAddress.Entity);
            }
        }

        public async Task<ReceivingAddressReservation> CreateReservationAsync(Guid id, CancellationToken cancellationToken)
        {
            using (var db = this.databaseFactory.CreateDbContext())
            {
                var reservation = await db.AddAsync
                (
                    new ReceivingAddressReservationModel{
                        Id = Guid.NewGuid(),
                        LockedAt = DateTime.UtcNow,
                        ReceivingAddressId = id,
                        ReleasedAt = null
                    }
                );

                await db.SaveChangesAsync(cancellationToken);

                return ToDomain(reservation.Entity);
            }
        }

        public async Task<ReceivingAddress> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            using (var db = this.databaseFactory.CreateDbContext())
            {
                var recv = await db.ReceivingAddresses
                    .Include(e => e.ReceivingAddressReservations)
                    .FirstAsync(r => r.Id == id);

                return ToDomain(recv);
            }
        }

        public async Task<ReceivingAddressReservation> GetReservationAsync(Guid id, CancellationToken cancellationToken)
        {
            using (var db = this.databaseFactory.CreateDbContext())
            {
                var resv = await db.ReceivingAddressReservations
                    .Include(e => e.ReceivingAddress)
                    .FirstAsync(r => r.Id == id);

                return ToDomain(resv);
            }
        }

        public Task<IEnumerable<ReceivingAddress>> ListReceivingAddressAsync(CancellationToken cancellationToken)
        {
            using (var db = this.databaseFactory.CreateDbContext())
            {
                return Task.FromResult<IEnumerable<ReceivingAddress>>(db.ReceivingAddresses.Select(r => ToDomain(r, true)).ToList());
            }
        }

        public async Task SetLockedStatusAsync(Guid id, bool locked, CancellationToken cancellationToken)
        {
            using (var db = this.databaseFactory.CreateDbContext())
            {
                var receivingAddress = await db.ReceivingAddresses
                    .FirstAsync(r => r.Id == id);

                if (receivingAddress == null)
                {
                    throw new KeyNotFoundException("The reserving address id is not found.");
                }

                receivingAddress.IsLocked = locked;

                await db.SaveChangesAsync();
            }
        }

        public async Task SetReleasedTimeAsync(Guid id, CancellationToken cancellationToken)
        {
            using (var db = this.databaseFactory.CreateDbContext())
            {
                var reservation = await db.ReceivingAddressReservations
                    .FirstAsync(r => r.Id == id);

                if (reservation == null)
                {
                    throw new KeyNotFoundException("The reservation id is not found.");
                }

                reservation.ReleasedAt = DateTime.UtcNow;

                await db.SaveChangesAsync();
            }
        }

        ReceivingAddress ToDomain(ReceivingAddressModel receivingAddress, bool recursive = true)
        {
            return new ReceivingAddress
            (
                receivingAddress.Id,
                BitcoinAddress.Create(receivingAddress.Address, this.network),
                receivingAddress.IsLocked,
                recursive && receivingAddress.ReceivingAddressReservations != null
                    ? receivingAddress.ReceivingAddressReservations.Select
                    (
                        r => ToDomain(r, false)
                    ).ToList()
                    : null
            );
        }

        ReceivingAddressReservation ToDomain(ReceivingAddressReservationModel reservationModel, bool recursive = true)
        {
            return new ReceivingAddressReservation
            (
                reservationModel.Id,
                recursive && reservationModel.ReceivingAddress != null
                    ? ToDomain(reservationModel.ReceivingAddress, false)
                    : null,
                DateTime.SpecifyKind(reservationModel.LockedAt, DateTimeKind.Utc),
                reservationModel.ReleasedAt.HasValue
                    ? DateTime.SpecifyKind(reservationModel.ReleasedAt.Value, DateTimeKind.Utc)
                    : new Nullable<DateTime>()
            );
        }
    }
}