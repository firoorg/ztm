using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NBitcoin;
using Ztm.Configuration;
using Ztm.Data.Entity.Contexts;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization
{
    public class BlocksStorage : IBlocksStorage
    {
        readonly IMainDatabaseFactory db;
        readonly Network zcoinNetwork;

        public BlocksStorage(IConfiguration config, IMainDatabaseFactory db)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (db == null)
            {
                throw new ArgumentNullException(nameof(db));
            }

            this.db = db;
            this.zcoinNetwork = ZcoinNetworks.Instance.GetNetwork(config.GetZcoinSection().Network.Type);
        }

        public async Task AddAsync(Block block, int height, CancellationToken cancellationToken)
        {
            if (block == null)
            {
                throw new ArgumentNullException(nameof(block));
            }

            if (height < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), "The value is negative.");
            }

            block.Header.PrecomputeHash(invalidateExisting: true, lazily: false);

            using (var db = this.db.CreateDbContext())
            using (var dbtx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken))
            {
                var entity = ToEntity(block, height);

                // Do not insert transactions that already exists.
                var transactions = entity.Transactions.Select(t => t.TransactionHash).ToArray();
                var existed = await db.Transactions
                    .Where(t => transactions.Contains(t.Hash))
                    .ToDictionaryAsync(t => t.Hash, cancellationToken);

                foreach (var tx in entity.Transactions.Where(t => existed.ContainsKey(t.TransactionHash)))
                {
                    tx.Transaction = null;
                }

                // Add block.
                await db.Blocks.AddAsync(entity, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
                dbtx.Commit();
            }
        }

        public async Task<(Block block, int height)> GetAsync(uint256 hash, CancellationToken cancellationToken)
        {
            Ztm.Data.Entity.Contexts.Main.Block data, previous;

            if (hash == null)
            {
                throw new ArgumentNullException(nameof(hash));
            }

            using (var db = this.db.CreateDbContext())
            using (await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken))
            {
                data = await db.Blocks.IncludeAll().SingleOrDefaultAsync(b => b.Hash == hash, cancellationToken);

                if (data == null)
                {
                    return (block: null, height: 0);
                }

                previous = (data.Height == 0)
                    ? null
                    : await db.Blocks.SingleAsync(b => b.Height == data.Height - 1, cancellationToken);
            }

            return (block: ToDomain(data, previous), height: data.Height);
        }

        public async Task<Block> GetAsync(int height, CancellationToken cancellationToken)
        {
            Ztm.Data.Entity.Contexts.Main.Block data, previous;

            if (height < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), "The value is negative.");
            }

            using (var db = this.db.CreateDbContext())
            {
                var rows = await db.Blocks
                    .IncludeAll()
                    .Where(b => b.Height == height || b.Height == height - 1)
                    .ToArrayAsync(cancellationToken);

                if (rows.Length == 0 || rows[0].Height != height)
                {
                    return null;
                }

                data = rows[0];
                previous = (rows.Length > 1) ? rows[1] : null;
            }

            return ToDomain(data, previous);
        }

        public async Task<Block> GetFirstAsync(CancellationToken cancellationToken)
        {
            Ztm.Data.Entity.Contexts.Main.Block data;

            using (var db = this.db.CreateDbContext())
            {
                data = await db.Blocks.IncludeAll().SingleOrDefaultAsync(e => e.Height == 0, cancellationToken);

                if (data == null)
                {
                    return null;
                }
            }

            return ToDomain(data);
        }

        public async Task<(Block block, int height)> GetLastAsync(CancellationToken cancellationToken)
        {
            Ztm.Data.Entity.Contexts.Main.Block data, previous;

            using (var db = this.db.CreateDbContext())
            {
                var rows = await db.Blocks
                    .IncludeAll()
                    .OrderByDescending(e => e.Height)
                    .Take(2)
                    .ToArrayAsync(cancellationToken);

                if (rows.Length == 0)
                {
                    return (block: null, height: 0);
                }

                data = rows[0];
                previous = (rows.Length > 1) ? rows[1] : null;
            }

            return (block: ToDomain(data, previous), height: data.Height);
        }

        public async Task<Transaction> GetTransactionAsync(uint256 hash, CancellationToken cancellationToken)
        {
            Ztm.Data.Entity.Contexts.Main.Transaction row;

            if (hash == null)
            {
                throw new ArgumentNullException(nameof(hash));
            }

            using (var db = this.db.CreateDbContext())
            {
                row = await db.Transactions
                    .Include(t => t.Inputs)
                    .Include(t => t.Outputs)
                    .SingleOrDefaultAsync(t => t.Hash == hash, cancellationToken);

                if (row == null)
                {
                    return null;
                }
            }

            return ToDomain(row);
        }

        public async Task RemoveLastAsync(CancellationToken cancellationToken)
        {
            using (var db = this.db.CreateDbContext())
            using (var dbtx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken))
            {
                // Remove block.
                var block = await db.Blocks
                    .Include(b => b.Transactions)
                    .ThenInclude(t => t.Transaction)
                    .ThenInclude(t => t.Blocks)
                    .OrderByDescending(b => b.Height)
                    .Take(1)
                    .SingleOrDefaultAsync(cancellationToken);

                if (block == null)
                {
                    return;
                }

                db.Blocks.Remove(block);

                // Remove referenced transactions if no other blocks referenced it.
                foreach (var transaction in block.Transactions.Select(t => t.Transaction))
                {
                    if (transaction.Blocks.Select(t => t.BlockHash).Distinct().Count() > 1)
                    {
                        continue;
                    }

                    db.Transactions.Remove(transaction);
                }

                await db.SaveChangesAsync(cancellationToken);
                dbtx.Commit();
            }
        }

        Block ToDomain(Ztm.Data.Entity.Contexts.Main.Block data, Ztm.Data.Entity.Contexts.Main.Block previous = null)
        {
            var block = Block.CreateBlock(this.zcoinNetwork);

            // Block properties.
            block.Header.Version = data.Version;
            block.Header.HashMerkleRoot = data.MerkleRoot;
            block.Header.BlockTime = DateTime.SpecifyKind(data.Time, DateTimeKind.Utc);
            block.Header.Bits = data.Bits;
            block.Header.Nonce = (uint)data.Nonce;

            if (data.MtpVersion != null)
            {
                if (!block.Header.IsMtp())
                {
                    throw new ArgumentException(
                        "The data is MTP-enabled but the consensus not activated for this data.",
                        nameof(data)
                    );
                }

                if (data.MtpHashValue == null || data.Reserved1 == null || data.Reserved2 == null)
                {
                    throw new ArgumentException(
                        "The data has MtpVersion but some other required properties is null.",
                        nameof(data)
                    );
                }

                block.Header.SetMtpVersion(data.MtpVersion.Value);
                block.Header.SetMtpHashValue(data.MtpHashValue);
                block.Header.SetReserved1(data.Reserved1);
                block.Header.SetReserved2(data.Reserved2);
            }

            if (previous != null)
            {
                block.Header.HashPrevBlock = previous.Hash;
            }

            block.Transactions = data.Transactions
                .Select(e => ToDomain(e.Transaction))
                .Cast<Transaction>()
                .ToList();

            return block;
        }

        Transaction ToDomain(Ztm.Data.Entity.Contexts.Main.Transaction entity)
        {
            // Common properties.
            var domain = Transaction.Create(this.zcoinNetwork);

            domain.Version = (uint)entity.Version;
            domain.LockTime = (uint)entity.LockTime;

            // Outputs.
            foreach (var output in entity.Outputs)
            {
                domain.Outputs.Add(output.Value, output.Script);
            }

            // Inputs.
            foreach (var input in entity.Inputs)
            {
                domain.Inputs.Add(
                    outpoint: new OutPoint(input.OutputHash, (uint)input.OutputIndex),
                    scriptSig: input.Script,
                    sequence: (uint)input.Sequence
                );
            }

            return domain;
        }

        Ztm.Data.Entity.Contexts.Main.Block ToEntity(Block block, int height)
        {
            var header = block.Header;

            var entity = new Ztm.Data.Entity.Contexts.Main.Block()
            {
                Height = height,
                Hash = block.GetHash(),
                Version = header.Version,
                Bits = header.Bits,
                Nonce = header.Nonce,
                Time = header.BlockTime.UtcDateTime,
                MerkleRoot = header.HashMerkleRoot
            };

            if (header.IsMtp())
            {
                if (header.GetMtpHashValue() == null || header.GetReserved1() == null || header.GetReserved2() == null)
                {
                    throw new ArgumentException(
                        "Block is MTP-enabled but some required fields is null.",
                        nameof(block)
                    );
                }

                entity.MtpVersion = header.GetMtpVersion();
                entity.MtpHashValue = header.GetMtpHashValue();
                entity.Reserved1 = header.GetReserved1();
                entity.Reserved2 = header.GetReserved2();
            }

            // Transactions.
            var transactions = new Dictionary<uint256, Ztm.Data.Entity.Contexts.Main.Transaction>();

            for (int i = 0; i < block.Transactions.Count; i++)
            {
                Ztm.Data.Entity.Contexts.Main.Transaction tx;

                block.Transactions[i].PrecomputeHash(invalidateExisting: true, lazily: false);

                var hash = block.Transactions[i].GetHash();
                var blockTx = new Ztm.Data.Entity.Contexts.Main.BlockTransaction()
                {
                    BlockHash = block.GetHash(),
                    TransactionHash = hash,
                    Index = i,
                    Block = entity
                };

                if (!transactions.TryGetValue(hash, out tx))
                {
                    tx = ToEntity(block.Transactions[i]);
                    transactions.Add(hash, tx);

                    blockTx.Transaction = tx;
                    tx.Blocks.Add(blockTx);
                }

                entity.Transactions.Add(blockTx);
            }

            return entity;
        }

        Ztm.Data.Entity.Contexts.Main.Transaction ToEntity(Transaction tx)
        {
            var entity = new Ztm.Data.Entity.Contexts.Main.Transaction()
            {
                Hash = tx.GetHash(),
                Version = tx.Version,
                LockTime = tx.LockTime
            };

            // Outputs.
            for (int i = 0; i < tx.Outputs.Count; i++)
            {
                var output = new Ztm.Data.Entity.Contexts.Main.Output()
                {
                    TransactionHash = entity.Hash,
                    Index = i,
                    Value = tx.Outputs[i].Value,
                    Script = tx.Outputs[i].ScriptPubKey,
                    Transaction = entity
                };

                entity.Outputs.Add(output);
            }

            // Inputs.
            for (int i = 0; i < tx.Inputs.Count; i++)
            {
                var input = new Ztm.Data.Entity.Contexts.Main.Input()
                {
                    TransactionHash = entity.Hash,
                    Index = i,
                    OutputHash = tx.Inputs[i].PrevOut.Hash,
                    OutputIndex = tx.Inputs[i].PrevOut.N,
                    Script = tx.Inputs[i].ScriptSig,
                    Sequence = tx.Inputs[i].Sequence,
                    Transaction = entity
                };

                entity.Inputs.Add(input);
            }

            return entity;
        }
    }
}
