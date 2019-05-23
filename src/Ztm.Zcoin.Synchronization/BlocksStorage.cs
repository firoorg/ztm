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
                // Block.
                await db.Blocks.AddAsync(new Ztm.Data.Entity.Contexts.Main.Block()
                {
                    Height = height,
                    Hash = block.GetHash(),
                    Version = block.Header.Version,
                    Bits = block.Header.Bits,
                    Nonce = block.Header.Nonce,
                    Time = block.Header.BlockTime.UtcDateTime,
                    MerkleRoot = block.Header.HashMerkleRoot
                }, cancellationToken);

                // Transactions.
                var transactions = new List<Ztm.Data.Entity.Contexts.Main.Transaction>(block.Transactions.Count);
                var blockTransactions = new List<Ztm.Data.Entity.Contexts.Main.BlockTransaction>(block.Transactions.Count);

                for (int i = 0; i < block.Transactions.Count; i++)
                {
                    var tx = block.Transactions[i];

                    tx.PrecomputeHash(invalidateExisting: true, lazily: false);

                    transactions.Add(new Ztm.Data.Entity.Contexts.Main.Transaction()
                    {
                        Hash = tx.GetHash(),
                        Version = tx.Version,
                        LockTime = tx.LockTime
                    });

                    blockTransactions.Add(new Ztm.Data.Entity.Contexts.Main.BlockTransaction()
                    {
                        BlockHash = block.GetHash(),
                        TransactionHash = tx.GetHash(),
                        Index = i
                    });
                }

                await db.Transactions.AddRangeAsync(transactions, cancellationToken);
                await db.BlockTransactions.AddRangeAsync(blockTransactions, cancellationToken);

                // Transaction outputs.
                var outputs = (
                    from tx in block.Transactions
                    from output in tx.Outputs.Select((vout, i) => new Ztm.Data.Entity.Contexts.Main.Output()
                    {
                        TransactionHash = tx.GetHash(),
                        Index = i,
                        Value = vout.Value,
                        Script = vout.ScriptPubKey
                    })
                    select output
                ).ToArray();

                await db.Outputs.AddRangeAsync(outputs, cancellationToken);

                // Inputs.
                var inputs = (
                    from tx in block.Transactions
                    from input in tx.Inputs.Select((vin, i) => new Ztm.Data.Entity.Contexts.Main.Input()
                    {
                        TransactionHash = tx.GetHash(),
                        Index = i,
                        OutputHash = vin.PrevOut.Hash,
                        OutputIndex = vin.PrevOut.N,
                        Script = vin.ScriptSig,
                        Sequence = vin.Sequence
                    })
                    select input
                ).ToArray();

                await db.Inputs.AddRangeAsync(inputs, cancellationToken);

                // Commit.
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
    }
}
