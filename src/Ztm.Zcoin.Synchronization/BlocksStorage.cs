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

        public async Task AddAsync(ZcoinBlock block, int height, CancellationToken cancellationToken)
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
            {
                await db.Blocks.AddAsync(ToEntity(block, height), cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<ZcoinBlock> GetAsync(uint256 hash, CancellationToken cancellationToken)
        {
            Ztm.Data.Entity.Contexts.Main.Block data, previous = null;

            if (hash == null)
            {
                throw new ArgumentNullException(nameof(hash));
            }

            using (var db = this.db.CreateDbContext())
            using (var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken))
            {
                data = await db.Blocks.IncludeAll().SingleAsync(e => e.Hash == hash, cancellationToken);

                if (data.Height != 0)
                {
                    previous = await db.Blocks.SingleAsync(e => e.Height == data.Height - 1, cancellationToken);
                }
            }

            return ToDomain(data, previous);
        }

        public async Task<ZcoinBlock> GetAsync(int height, CancellationToken cancellationToken)
        {
            Ztm.Data.Entity.Contexts.Main.Block data, previous = null;

            if (height < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), "The value is negative.");
            }

            using (var db = this.db.CreateDbContext())
            using (var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken))
            {
                data = await db.Blocks.IncludeAll().SingleAsync(e => e.Height == height, cancellationToken);

                if (height != 0)
                {
                    previous = await db.Blocks.SingleAsync(e => e.Height == height - 1, cancellationToken);
                }
            }

            return ToDomain(data, previous);
        }

        public async Task<ZcoinBlock> GetFirstAsync(CancellationToken cancellationToken)
        {
            Ztm.Data.Entity.Contexts.Main.Block data;

            using (var db = this.db.CreateDbContext())
            {
                data = await db.Blocks.IncludeAll().SingleAsync(e => e.Height == 0, cancellationToken);
            }

            return ToDomain(data);
        }

        public async Task<ZcoinBlock> GetLastAsync(CancellationToken cancellationToken)
        {
            Ztm.Data.Entity.Contexts.Main.Block data, previous = null;

            using (var db = this.db.CreateDbContext())
            using (var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken))
            {
                data = await db.Blocks
                    .IncludeAll()
                    .OrderByDescending(e => e.Height)
                    .Take(1)
                    .SingleAsync(cancellationToken);

                if (data.Height != 0)
                {
                    previous = await db.Blocks.SingleAsync(e => e.Height == data.Height - 1, cancellationToken);
                }
            }

            return ToDomain(data, previous);
        }

        public async Task RemoveAsync(uint256 hash, CancellationToken cancellationToken)
        {
            if (hash == null)
            {
                throw new ArgumentNullException(nameof(hash));
            }

            using (var db = this.db.CreateDbContext())
            {
                var block = await db.Blocks.IncludeAll().SingleAsync(e => e.Hash == hash, cancellationToken);

                db.RemoveBlock(block);

                await db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task RemoveAsync(int height, CancellationToken cancellationToken)
        {
            if (height < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), "The value is negative.");
            }

            using (var db = this.db.CreateDbContext())
            {
                var block = await db.Blocks.IncludeAll().SingleAsync(e => e.Height == height, cancellationToken);

                db.RemoveBlock(block);

                await db.SaveChangesAsync(cancellationToken);
            }
        }

        ZcoinBlock ToDomain(Ztm.Data.Entity.Contexts.Main.Block data, Ztm.Data.Entity.Contexts.Main.Block previous = null)
        {
            var block = ZcoinBlock.CreateBlock(this.zcoinNetwork);

            // Block properties.
            block.Header.Version = data.Version;
            block.Header.HashMerkleRoot = data.MerkleRoot;
            block.Header.BlockTime = data.Time;
            block.Header.Bits = data.Bits;
            block.Header.Nonce = (uint)data.Nonce;

            if (previous != null)
            {
                block.Header.HashPrevBlock = previous.Hash;
            }

            block.Transactions = data.Transactions.Select(e =>
            {
                // Transaction properties.
                var tx = new ZcoinTransaction()
                {
                    Version = (uint)e.Transaction.Version,
                    LockTime = (uint)e.Transaction.LockTime
                };

                // Transaction outputs.
                foreach (var output in e.Transaction.Outputs)
                {
                    tx.Outputs.Add(new ZcoinTxOut()
                    {
                        ScriptPubKey = output.Script,
                        Value = output.Value
                    });
                }

                // Transaction inputs.
                foreach (var input in e.Transaction.Inputs)
                {
                    tx.Inputs.Add(new ZcoinTxIn()
                    {
                        Sequence = (uint)input.Sequence,
                        PrevOut = new OutPoint(input.OutputHash, (uint)input.OutputIndex),
                        ScriptSig = input.Script
                    });
                }

                return tx;
            }).Cast<Transaction>().ToList();

            return block;
        }

        Ztm.Data.Entity.Contexts.Main.Block ToEntity(ZcoinBlock block, int height)
        {
            var entity = new Ztm.Data.Entity.Contexts.Main.Block()
            {
                Height = height,
                Hash = block.GetHash(),
                Version = block.Header.Version,
                Bits = block.Header.Bits,
                Nonce = block.Header.Nonce,
                Time = block.Header.BlockTime.UtcDateTime,
                MerkleRoot = block.Header.HashMerkleRoot
            };

            // Transactions.
            var transactions = new Dictionary<uint256, Ztm.Data.Entity.Contexts.Main.Transaction>();

            for (int i = 0; i < block.Transactions.Count; i++)
            {
                Ztm.Data.Entity.Contexts.Main.Transaction tx;
                Ztm.Data.Entity.Contexts.Main.BlockTransaction blockTx;

                block.Transactions[i].PrecomputeHash(invalidateExisting: true, lazily: false);
                var hash = block.Transactions[i].GetHash();

                if (!transactions.TryGetValue(hash, out tx))
                {
                    tx = ToEntity((ZcoinTransaction)block.Transactions[i]);
                    transactions.Add(hash, tx);
                }

                blockTx = new Ztm.Data.Entity.Contexts.Main.BlockTransaction()
                {
                    BlockHash = block.GetHash(),
                    TransactionHash = tx.Hash,
                    Index = i,
                    Block = entity,
                    Transaction = tx
                };

                tx.Blocks.Add(blockTx);
                entity.Transactions.Add(blockTx);
            }

            return entity;
        }

        Ztm.Data.Entity.Contexts.Main.Transaction ToEntity(ZcoinTransaction tx)
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
