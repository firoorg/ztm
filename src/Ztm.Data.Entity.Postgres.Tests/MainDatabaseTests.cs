using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NBitcoin;
using NBitcoin.DataEncoders;
using Xunit;

namespace Ztm.Data.Entity.Postgres.Tests
{
    public sealed class MainDatabaseTests : IDisposable
    {
        readonly MainDatabase subject;

        public MainDatabaseTests()
        {
            var builder = new ConfigurationBuilder();

            var connectionString = Environment.GetEnvironmentVariable("ZTM_MAIN_DATABASE");

            if (connectionString == null)
            {
                throw new Exception("No ZTM_MAIN_DATABASE environment variable is set.");
            }

            builder.AddInMemoryCollection(new Dictionary<string, string>()
            {
                {"Database:Main:ConnectionString", connectionString}
            });

            var config = builder.Build();

            var dbFactory = new MainDatabaseFactory(config);
            this.subject = (MainDatabase)dbFactory.CreateDbContext();
        }

        public void Dispose()
        {
            this.subject.Dispose();
        }

        [TheoryWhenEnvIsSet("ZTM_MAIN_DATABASE")]
        [InlineData("0000000000000000000000000000000000000000000000000000000000000000")]
        [InlineData("0000000000000000000000000000000000000000000000000000000000000001")]
        [InlineData("47EAB73075941E140A7F60AC9029220E27464335F94EF717927D10BB79019ED4")]
        [InlineData("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF")]
        public async Task SaveAndLoadUint256_ToDatabase_ShouldBeStoreAsBigEndian(string data)
        {
            var binary = Encoders.Hex.DecodeData(data); // Big endian
            var parsed = uint256.Parse(data);

            var entity = new Ztm.Data.Entity.Contexts.Main.Block
            {
                Height = 0,
                Hash = parsed,
                Version = 0,
                Bits = new Target(0),
                Nonce = 0,
                Time = DateTime.UtcNow,
                MerkleRoot = uint256.Zero
            };

            try
            {
                // Store to database.
                await this.subject.Blocks.AddAsync(entity, CancellationToken.None);
                await this.subject.SaveChangesAsync();

                // Assert Endianness.
                using (var command = this.subject.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "SELECT \"Hash\" FROM \"Blocks\" LIMIT 1;";
                    this.subject.Database.OpenConnection();
                    using (var result = command.ExecuteReader())
                    {
                        Assert.True(result.Read(), "No available data");
                        var record = (IDataRecord)result;
                        var hash = record[0] as byte[]; // Get raw data.

                        Assert.Equal(binary, hash);
                    }
                }

                // Get uint256 back.
                var block = await this.subject.Blocks.FirstOrDefaultAsync();
                Assert.NotNull(block);
                Assert.Equal(parsed, block.Hash);
            }
            finally
            {
                this.subject.Database.ExecuteSqlCommand("TRUNCATE TABLE \"Blocks\" CASCADE");
            }
        }
    }
}
