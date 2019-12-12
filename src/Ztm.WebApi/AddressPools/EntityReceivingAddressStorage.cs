using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NBitcoin;
using Ztm.Data.Entity.Contexts;
using ReceivingAddressModel = Ztm.Data.Entity.Contexts.Main.ReceivingAddress;
using ReceivingAddressReservationModel = Ztm.Data.Entity.Contexts.Main.ReceivingAddressReservation;

namespace Ztm.WebApi.AddressPools
{
    public sealed class EntityReceivingAddressStorage : IReceivingAddressStorage
    {
        readonly IMainDatabaseFactory databaseFactory;
        readonly Network network;

        public EntityReceivingAddressStorage(IMainDatabaseFactory databaseFactory, Network network)
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

        public async Task<ReceivingAddress> AddAsync(BitcoinAddress address, CancellationToken cancellationToken)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            using (var db = this.databaseFactory.CreateDbContext())
            using (var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead))
            {
                var exist = await db.ReceivingAddresses.FirstOrDefaultAsync(a => a.Address == address.ToString());
                if (exist != null)
                {
                    throw new ArgumentException("The address is duplicated.", nameof(address));
                }

                var recAddress = await db.ReceivingAddresses.AddAsync(
                    new ReceivingAddressModel
                    {
                        Address = address.ToString(),
                        IsLocked = false
                    },
                    cancellationToken
                );

                await db.SaveChangesAsync(cancellationToken);
                tx.Commit();

                return ToDomain(recAddress.Entity);
            }
        }

        public async Task<ReceivingAddress> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            using (var db = this.databaseFactory.CreateDbContext())
            {
                var recv = await db.ReceivingAddresses
                    .Include(e => e.Reservations)
                    .SingleOrDefaultAsync(r => r.Id == id);

                return recv == null ? null : ToDomain(recv);
            }
        }

        public async Task<IEnumerable<ReceivingAddress>> ListAsync(AddressFilter filter, CancellationToken cancellationToken)
        {
            using (var db = this.databaseFactory.CreateDbContext())
            {
                IQueryable<ReceivingAddressModel> query = db.ReceivingAddresses.Include(a => a.Reservations);

                if (filter.HasFlag(AddressFilter.Available))
                {
                    query = query.Where(a => !a.IsLocked);
                }

                if (filter.HasFlag(AddressFilter.NeverUsed))
                {
                    query = query.Where(a => !a.Reservations.Any());
                }

                return await query.Select(r => ToDomain(r)).ToListAsync();
            }
        }

        public async Task ReleaseAsync(Guid id, CancellationToken cancellationToken)
        {
            using (var db = this.databaseFactory.CreateDbContext())
            using (var tx = db.Database.BeginTransaction(IsolationLevel.RepeatableRead))
            {
                var reservation = await db.ReceivingAddressReservations
                    .Include(r => r.Address)
                    .SingleOrDefaultAsync(r => r.Id == id);

                if (reservation == null)
                {
                    throw new KeyNotFoundException("Reservation id is not found.");
                }

                if (reservation.ReleasedAt != null)
                {
                    throw new InvalidOperationException("The reservation is already released.");
                }

                reservation.ReleasedAt = DateTime.UtcNow;
                reservation.Address.IsLocked = false;

                await db.SaveChangesAsync(cancellationToken);
                tx.Commit();
            }
        }

        public async Task<ReceivingAddressReservation> TryLockAsync(Guid id, CancellationToken cancellationToken)
        {
            using (var db = this.databaseFactory.CreateDbContext())
            using (var tx = db.Database.BeginTransaction(IsolationLevel.RepeatableRead))
            {
                var recv = await db
                    .ReceivingAddresses
                    .SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

                if (recv == null)
                {
                    throw new KeyNotFoundException("Address id is not found.");
                }

                if (recv.IsLocked)
                {
                    return null;
                }

                recv.IsLocked = true;

                var reservation = await db.ReceivingAddressReservations.AddAsync
                (
                    new ReceivingAddressReservationModel
                    {
                        Id = Guid.NewGuid(),
                        LockedAt = DateTime.UtcNow,
                        AddressId = id,
                        ReleasedAt = null
                    }
                );

                await db.SaveChangesAsync(cancellationToken);
                tx.Commit();

                return ToDomain(reservation.Entity);
            }
        }

        ReceivingAddress ToDomain(ReceivingAddressModel entity)
        {
            var r = new ReceivingAddress
            (
                entity.Id,
                BitcoinAddress.Create(entity.Address, this.network),
                entity.IsLocked,
                new List<ReceivingAddressReservation>()
            );

            foreach (var reservation in entity.Reservations)
            {
                r.Reservations.Add(ToDomain(reservation, r));
            }

            return r;
        }

        ReceivingAddressReservation ToDomain(ReceivingAddressReservationModel entity, ReceivingAddress address = null)
        {
            return new ReceivingAddressReservation
            (
                entity.Id,
                address != null ? address : ToDomain(entity.Address),
                DateTime.SpecifyKind(entity.LockedAt, DateTimeKind.Utc),
                entity.ReleasedAt.HasValue
                    ? DateTime.SpecifyKind(entity.ReleasedAt.Value, DateTimeKind.Utc)
                    : new Nullable<DateTime>()
            );
        }
    }
}