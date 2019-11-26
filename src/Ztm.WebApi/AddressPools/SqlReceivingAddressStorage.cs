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

        public Task<IEnumerable<ReceivingAddress>> ListReceivingAddressAsync(CancellationToken cancellationToken)
        {
            using (var db = this.databaseFactory.CreateDbContext())
            {
                return Task.FromResult<IEnumerable<ReceivingAddress>>(db.ReceivingAddresses.Select(r => ToDomain(r)).ToList());
            }
        }

        public async Task ReleaseAsync(Guid id, CancellationToken cancellationToken)
        {
            using (var db = this.databaseFactory.CreateDbContext())
            using (var tx = db.Database.BeginTransaction())
            {
                var reservation = await db.ReceivingAddressReservations
                    .Include(r => r.ReceivingAddress)
                    .FirstAsync(r => r.Id == id);

                if (reservation == null)
                {
                    throw new KeyNotFoundException("The reservation id is not found.");
                }

                if (reservation.ReleasedAt != null)
                {
                    throw new InvalidOperationException("The reservation is already released.");
                }

                reservation.ReleasedAt = DateTime.UtcNow;
                reservation.ReceivingAddress.IsLocked = false;

                await db.SaveChangesAsync();
                tx.Commit();
            }
        }

        public async Task<ReceivingAddressReservation> TryLockAsync(Guid id, CancellationToken cancellationToken)
        {
            using (var db = this.databaseFactory.CreateDbContext())
            using (var tx = db.Database.BeginTransaction())
            {
                var recv = await db.ReceivingAddresses.FirstAsync(a => a.Id == id, cancellationToken);
                if (recv == null)
                {
                    return null;
                }

                if (recv.IsLocked)
                {
                    return null;
                }

                recv.IsLocked = true;

                var lockedAt = DateTime.UtcNow;
                var reservation = await db.AddAsync
                (
                    new ReceivingAddressReservationModel{
                        Id = Guid.NewGuid(),
                        LockedAt = lockedAt,
                        ReceivingAddressId = id,
                        ReleasedAt = null
                    }
                );

                await db.SaveChangesAsync(cancellationToken);
                tx.Commit();

                return ToDomain(reservation.Entity);
            }
        }

        ReceivingAddress ToDomain(ReceivingAddressModel receivingAddress)
        {
            var r = new ReceivingAddress
            (
                receivingAddress.Id,
                BitcoinAddress.Create(receivingAddress.Address, this.network),
                receivingAddress.IsLocked,
                new List<ReceivingAddressReservation>()
            );

            if (receivingAddress.ReceivingAddressReservations != null)
            {
                foreach (var reservation in receivingAddress.ReceivingAddressReservations)
                {
                    r.ReceivingAddressReservations.Add(ToDomain(reservation, r));
                }
            }

            return r;
        }

        ReceivingAddressReservation ToDomain(ReceivingAddressReservationModel reservationModel)
        {
            return new ReceivingAddressReservation
            (
                reservationModel.Id,
                ToDomain(reservationModel.ReceivingAddress),
                DateTime.SpecifyKind(reservationModel.LockedAt, DateTimeKind.Utc),
                reservationModel.ReleasedAt.HasValue
                    ? DateTime.SpecifyKind(reservationModel.ReleasedAt.Value, DateTimeKind.Utc)
                    : new Nullable<DateTime>()
            );
        }

        ReceivingAddressReservation ToDomain(ReceivingAddressReservationModel reservationModel, ReceivingAddress address)
        {
            return new ReceivingAddressReservation
            (
                reservationModel.Id,
                address,
                DateTime.SpecifyKind(reservationModel.LockedAt, DateTimeKind.Utc),
                reservationModel.ReleasedAt.HasValue
                    ? DateTime.SpecifyKind(reservationModel.ReleasedAt.Value, DateTimeKind.Utc)
                    : new Nullable<DateTime>()
            );
        }
    }
}