using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NBitcoin;
using NBitcoin.DataEncoders;
using Xunit;

namespace Ztm.Data.Entity.Postgres.Tests
{
    public sealed class MainDatabaseTests : IClassFixture<MainDatabaseFixture>, IDisposable
    {
        readonly MainDatabaseFixture fixture;

        public MainDatabaseTests(MainDatabaseFixture fixture)
        {
            this.fixture = fixture;
        }

        public void Dispose()
        {
            this.fixture.CleanUp();
        }

        [ConditionalTheory(RequiredEnv = "ZTM_MAIN_DATABASE")]
        [InlineData("0000000000000000000000000000000000000000000000000000000000000000")]
        [InlineData("0000000000000000000000000000000000000000000000000000000000000001")]
        [InlineData("47EAB73075941E140A7F60AC9029220E27464335F94EF717927D10BB79019ED4")]
        [InlineData("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF")]
        public async Task SaveAndLoadUint256_ToDatabase_ShouldBeStoreAsBigEndian(string data)
        {
            using (var db = this.fixture.CreateMainDatabase())
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

                // Store to database.
                await db.Blocks.AddAsync(entity, CancellationToken.None);
                await db.SaveChangesAsync();

                // Assert Endianness.
                var blockHashes = this.fixture.ExecuteSql("SELECT \"Hash\" FROM \"Blocks\" LIMIT 1;")
                                              .Select(r => r[0] as byte[]);

                var hash = Assert.Single(blockHashes);
                Assert.Equal(binary, hash);

                // Get uint256 back.
                var block = await db.Blocks.FirstOrDefaultAsync();
                Assert.NotNull(block);
                Assert.Equal(parsed, block.Hash);
            }
        }
    }
}
