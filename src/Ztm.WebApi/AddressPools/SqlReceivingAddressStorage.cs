using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
                return Task.FromResult<IEnumerable<ReceivingAddress>>(db.ReceivingAddresses.Select(r => ToDomain(r, true)).ToList());
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

                if (reservation.ReleasedAt != DateTime.MinValue)
                {
                    throw new InvalidOperationException("The reservation is already released.");
                }

                reservation.ReleasedAt = DateTime.UtcNow;
                reservation.ReceivingAddress.IsLocked = false;

                await db.SaveChangesAsync();
                tx.Commit();
            }
        }

        public async Task<ReceivingAddressReservation> TryLockAsync(Guid id, TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (timeout < TimeSpan.Zero)
            {
                throw new ArgumentException(nameof(timeout), "Timeout can not less than or equal zero.");
            }

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
                        Due = lockedAt.Add(timeout),
                        LockedAt = lockedAt,
                        ReceivingAddressId = id,
                        ReleasedAt = DateTime.MinValue
                    }
                );

                await db.SaveChangesAsync(cancellationToken);
                tx.Commit();

                return ToDomain(reservation.Entity);
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
                DateTime.SpecifyKind(reservationModel.ReleasedAt, DateTimeKind.Utc),
                DateTime.SpecifyKind(reservationModel.Due, DateTimeKind.Utc)
            );
        }
    }
}