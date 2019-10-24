using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NBitcoin;
using Xunit;
using Ztm.Data.Entity.Testing;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Zcoin.Synchronization.Tests
{
    public sealed class BlocksStorageTests : IDisposable
    {
        readonly IConfiguration config;
        readonly TestMainDatabaseFactory dbFactory;
        readonly BlocksStorage subject;

        public BlocksStorageTests()
        {
            var builder = new ConfigurationBuilder();

            builder.AddInMemoryCollection(new Dictionary<string, string>()
            {
                {"Zcoin:Network:Type", "Regtest"}
            });

            this.config = builder.Build();
            this.dbFactory = new TestMainDatabaseFactory();
            this.subject = new BlocksStorage(this.config, this.dbFactory);
        }

        public void Dispose()
        {
            this.dbFactory.Dispose();
        }

        [Fact]
        public void Constructor_PassNullForConfig_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "config",
                () => new BlocksStorage(config: null, db: this.dbFactory)
            );
        }

        [Fact]
        public void Constructor_PassNullForDb_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(
                "db",
                () => new BlocksStorage(config: this.config, db: null)
            );
        }

        [Fact]
        public async Task AddAsync_PassNullForBlock_ShouldThrow()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                "block",
                () => this.subject.AddAsync(null, 0, CancellationToken.None)
            );
        }

        [Fact]
        public async Task AddAsync_PassNegativeForHeight_ShouldThrow()
        {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                "height",
                () => this.subject.AddAsync(Block.CreateBlock(ZcoinNetworks.Instance.Regtest), -1, CancellationToken.None)
            );
        }

        [Fact]
        public async Task AddAsync_WithGenesisBlock_ShouldSuccess()
        {
            // Arrange.
            var block = ZcoinNetworks.Instance.Regtest.GetGenesis();

            // Act.
            await this.subject.AddAsync(block, 0, CancellationToken.None);

            // Assert.
            var (saved, height) = await this.subject.GetAsync(block.GetHash(), CancellationToken.None);

            Assert.Equal(block.GetHash(), saved.GetHash());
            Assert.Equal(0, height);
        }

        [Fact]
        public async Task AddAsync_WithNonGenesisBlock_ShouldSuccess()
        {
            // Arrange.

            // {
            //   "hash": "00a10f5a28e8058528af5dd773fa6604dfca5519a2d999b9b2f32550626655ad",
            //   "confirmations": 2,
            //   "strippedsize": 350,
            //   "size": 350,
            //   "weight": 350,
            //   "height": 102,
            //   "version": 536870912,
            //   "versionHex": "20000000",
            //   "merkleroot": "128d362e6cc577d1149485c115a25b73be2eecf9b0125656ba6c339284b2efd4",
            //   "tx": [
            //     "128d362e6cc577d1149485c115a25b73be2eecf9b0125656ba6c339284b2efd4"
            //   ],
            //   "time": 1558694943,
            //   "mediantime": 1558694887,
            //   "nonce": 395,
            //   "bits": "2000ffff",
            //   "difficulty": 5.960464477539062e-08,
            //   "chainwork": "0000000000000000000000000000000000000000000000000000000000006602",
            //   "previousblockhash": "00db6c087d793b5857d131c62840d886e5236a641142bc7078fff6ae84185951",
            //   "nextblockhash": "000b76c1fce28e82079d561bc22ca2d1300d6aef4c5b247c780a0c3547550ff3"
            // }
            var block102 = Block.Parse(
                "0000002051591884aef6ff7870bc4211646a23e586d84028c631d157583b797d086cdb00d4efb28492336cba565612b0f9ec2ebe735ba215c1859414d177c56c2e368d121fcce75cffff00208b0100000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff0401660101ffffffff0600286bee00000000232102605ee01828020ba1331a832e7583b4f081958329b81ce500c6a84227c412a06dac00c2eb0b000000001976a914296134d2415bf1f2b518b3f673816d7e603b160088ac00c2eb0b000000001976a914e1e1dc06a889c1b6d3eb00eef7a96f6a7cfb884888ac00c2eb0b000000001976a914ab03ecfddee6330497be894d16c29ae341c123aa88ac00c2eb0b000000001976a9144281a58a1d5b2d3285e00cb45a8492debbdad4c588ac00c2eb0b000000001976a9141fd264c0bb53bd9fef18e2248ddf1383d6e811ae88ac00000000",
                ZcoinNetworks.Instance.Regtest
            );

            // {
            //   "hash": "000b76c1fce28e82079d561bc22ca2d1300d6aef4c5b247c780a0c3547550ff3",
            //   "confirmations": 1,
            //   "strippedsize": 654,
            //   "size": 654,
            //   "weight": 654,
            //   "height": 103,
            //   "version": 536870912,
            //   "versionHex": "20000000",
            //   "merkleroot": "f830daa04a0202a8b0c8b83090b23e749d36d9d909b1b2a61b966a105a04576b",
            //   "tx": [
            //     "70a083c52fa6e05e04d4b08113955719c661edf735cd0ead55f4f65422acf309",
            //     "2b749d8b1ceafa7a7a8494403443a2d295279d1760c6ef1f7a5ca8788926b46e"
            //   ],
            //   "time": 1558694989,
            //   "mediantime": 1558694888,
            //   "nonce": 3,
            //   "bits": "2000ffff",
            //   "difficulty": 5.960464477539062e-08,
            //   "chainwork": "0000000000000000000000000000000000000000000000000000000000006702",
            //   "previousblockhash": "00a10f5a28e8058528af5dd773fa6604dfca5519a2d999b9b2f32550626655ad"
            // }
            // {
            //   "hex": "0100000002e424d1c6617ab0c0f42bc21b3b2df34b21c10fadde6b26f03f5ab3283c852a6d00000000484730440220178932e198a67fc104e8eae8d784c124185f75135eef03c79ef969110c557db3022018cee13be50f53ae3b02a4db2263906f84ff9b5d8d378c73097c328445047a1201feffffffee4893410d9794cc48916b911a60a47eabd02c71fa3b3958e9c93fe35d7c2d33000000004847304402203f34f35c7cf43c44b95735c6dc26f99e7914c21c56f7d210a072a8081939b2bd022041ccbeeef92f2dca86c3cac85811346f58042f1542761995c82ed8a2201d04b201feffffff0200bca065010000001976a914a2580429ee776b92db747218a97bfe4bc8a1a50888ac18903577000000001976a9148505a05a12fd894e61876c68f0ba0050f3b78db488ac66000000",
            //   "txid": "2b749d8b1ceafa7a7a8494403443a2d295279d1760c6ef1f7a5ca8788926b46e",
            //   "hash": "2b749d8b1ceafa7a7a8494403443a2d295279d1760c6ef1f7a5ca8788926b46e",
            //   "size": 304,
            //   "vsize": 304,
            //   "version": 1,
            //   "locktime": 102,
            //   "vin": [
            //     {
            //       "txid": "6d2a853c28b35a3ff0266bdead0fc1214bf32d3b1bc22bf4c0b07a61c6d124e4",
            //       "vout": 0,
            //       "scriptSig": {
            //         "asm": "30440220178932e198a67fc104e8eae8d784c124185f75135eef03c79ef969110c557db3022018cee13be50f53ae3b02a4db2263906f84ff9b5d8d378c73097c328445047a12[ALL]",
            //         "hex": "4730440220178932e198a67fc104e8eae8d784c124185f75135eef03c79ef969110c557db3022018cee13be50f53ae3b02a4db2263906f84ff9b5d8d378c73097c328445047a1201"
            //       },
            //       "value": 40.00000000,
            //       "valueSat": 4000000000,
            //       "address": "TAPMwZA83HHdnb8cz8aTRGxzZcAbd39WB5",
            //       "sequence": 4294967294
            //     },
            //     {
            //       "txid": "332d7c5de33fc9e958393bfa712cd0ab7ea4601a916b9148cc94970d419348ee",
            //       "vout": 0,
            //       "scriptSig": {
            //         "asm": "304402203f34f35c7cf43c44b95735c6dc26f99e7914c21c56f7d210a072a8081939b2bd022041ccbeeef92f2dca86c3cac85811346f58042f1542761995c82ed8a2201d04b2[ALL]",
            //         "hex": "47304402203f34f35c7cf43c44b95735c6dc26f99e7914c21c56f7d210a072a8081939b2bd022041ccbeeef92f2dca86c3cac85811346f58042f1542761995c82ed8a2201d04b201"
            //       },
            //       "value": 40.00000000,
            //       "valueSat": 4000000000,
            //       "address": "TGD6YReAGEzT3QHMSazuBU6G9R4J2hjR4z",
            //       "sequence": 4294967294
            //     }
            //   ],
            //   "vout": [
            //     {
            //       "value": 60.00000000,
            //       "n": 0,
            //       "scriptPubKey": {
            //         "asm": "OP_DUP OP_HASH160 a2580429ee776b92db747218a97bfe4bc8a1a508 OP_EQUALVERIFY OP_CHECKSIG",
            //         "hex": "76a914a2580429ee776b92db747218a97bfe4bc8a1a50888ac",
            //         "reqSigs": 1,
            //         "type": "pubkeyhash",
            //         "addresses": [
            //           "TQmbucVmyc8YWrxA8YcirCdJwcFLYK9PPH"
            //         ]
            //       }
            //     },
            //     {
            //       "value": 19.99999000,
            //       "n": 1,
            //       "scriptPubKey": {
            //         "asm": "OP_DUP OP_HASH160 8505a05a12fd894e61876c68f0ba0050f3b78db4 OP_EQUALVERIFY OP_CHECKSIG",
            //         "hex": "76a9148505a05a12fd894e61876c68f0ba0050f3b78db488ac",
            //         "reqSigs": 1,
            //         "type": "pubkeyhash",
            //         "addresses": [
            //           "TN6ZcVwZNmxaRzKWTDXBBihArSLXxKrQTx"
            //         ]
            //       }
            //     }
            //   ],
            //   "blockhash": "000b76c1fce28e82079d561bc22ca2d1300d6aef4c5b247c780a0c3547550ff3",
            //   "height": 103,
            //   "confirmations": 1,
            //   "time": 1558694989,
            //   "blocktime": 1558694989
            // }
            var block103 = Block.Parse(
                "00000020ad5566625025f3b2b999d9a21955cadf0466fa73d75daf288505e8285a0fa1006b57045a106a961ba6b2b109d9d9369d743eb29030b8c8b0a802024aa0da30f84dcce75cffff0020030000000201000000010000000000000000000000000000000000000000000000000000000000000000ffffffff0401670101ffffffff06e82b6bee00000000232103d920519b3932691f684cccabe8097ae2d705df23137ae2cf09b6b3767be70adfac00c2eb0b000000001976a914296134d2415bf1f2b518b3f673816d7e603b160088ac00c2eb0b000000001976a914e1e1dc06a889c1b6d3eb00eef7a96f6a7cfb884888ac00c2eb0b000000001976a914ab03ecfddee6330497be894d16c29ae341c123aa88ac00c2eb0b000000001976a9144281a58a1d5b2d3285e00cb45a8492debbdad4c588ac00c2eb0b000000001976a9141fd264c0bb53bd9fef18e2248ddf1383d6e811ae88ac000000000100000002e424d1c6617ab0c0f42bc21b3b2df34b21c10fadde6b26f03f5ab3283c852a6d00000000484730440220178932e198a67fc104e8eae8d784c124185f75135eef03c79ef969110c557db3022018cee13be50f53ae3b02a4db2263906f84ff9b5d8d378c73097c328445047a1201feffffffee4893410d9794cc48916b911a60a47eabd02c71fa3b3958e9c93fe35d7c2d33000000004847304402203f34f35c7cf43c44b95735c6dc26f99e7914c21c56f7d210a072a8081939b2bd022041ccbeeef92f2dca86c3cac85811346f58042f1542761995c82ed8a2201d04b201feffffff0200bca065010000001976a914a2580429ee776b92db747218a97bfe4bc8a1a50888ac18903577000000001976a9148505a05a12fd894e61876c68f0ba0050f3b78db488ac66000000",
                ZcoinNetworks.Instance.Regtest
            );

            // Act.
            await this.subject.AddAsync(block102, 102, CancellationToken.None);
            await this.subject.AddAsync(block103, 103, CancellationToken.None);

            // Assert.
            var (saved, height) = await this.subject.GetAsync(block103.GetHash(), CancellationToken.None);

            Assert.Equal(block103.GetHash(), saved.GetHash());
            Assert.Equal(103, height);
        }

        [Fact]
        public async Task AddAsync_WithValidMtpBlock_ShouldSuccess()
        {
            // Arrange.
            var builder = new ConfigurationBuilder();

            builder.AddInMemoryCollection(new Dictionary<string, string>()
            {
                {"Zcoin:Network:Type", "Mainnet"} // We cannot use Regtest since MTP is never activated.
            });

            var config = builder.Build();
            var subject = new BlocksStorage(config, this.dbFactory);
            var block = Block.CreateBlock(ZcoinNetworks.Instance.Mainnet);

            block.Header.BlockTime = new DateTimeOffset(
                year: 2019,
                month: 5,
                day: 31,
                hour: 21,
                minute: 38,
                second: 5,
                offset: TimeSpan.Zero
            );

            block.Header.SetMtpVersion(99);
            block.Header.SetMtpHashValue(new uint256(1));
            block.Header.SetReserved1(new uint256(2));
            block.Header.SetReserved2(new uint256(3));

            // Act.
            await subject.AddAsync(block, 0, CancellationToken.None);

            // Assert.
            var (saved, _) = await subject.GetAsync(block.GetHash(), CancellationToken.None);

            Assert.NotNull(saved);
            Assert.Equal(block.GetHash(), saved.GetHash());
        }

        [Fact]
        public async Task GetAsync_PassNullForHash_ShouldThrow()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                "hash",
                () => this.subject.GetAsync(null, CancellationToken.None)
            );
        }

        [Fact]
        public async Task GetAsync_PassNegativeHeight_ShouldThrow()
        {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                "height",
                () => this.subject.GetAsync(-1, CancellationToken.None)
            );
        }

        [Fact]
        public async Task GetAsync_WithEmptyTable_ShouldReturnNull()
        {
            // Act.
            var (byHash, height) = await this.subject.GetAsync(ZcoinNetworks.Instance.Regtest.GetGenesis().GetHash(), CancellationToken.None);
            var byHeight = await this.subject.GetAsync(0, CancellationToken.None);

            // Assert.
            Assert.Null(byHash);
            Assert.Equal(0, height);
            Assert.Null(byHeight);
        }

        [Fact]
        public async Task GetAsync_WithInvalidHash_ShouldReturnNull()
        {
            // Arrange.
            var block = ZcoinNetworks.Instance.Regtest.GetGenesis();

            await this.subject.AddAsync(block, 0, CancellationToken.None);

            // Act.
            var (saved, height) = await this.subject.GetAsync(uint256.One, CancellationToken.None);

            // Assert.
            Assert.Null(saved);
            Assert.Equal(0, height);
        }

        [Fact]
        public async Task GetAsync_WithValidHeight_ShouldSuccess()
        {
            // Arrange.
            var block = ZcoinNetworks.Instance.Regtest.GetGenesis();

            await this.subject.AddAsync(block, 0, CancellationToken.None);

            // Act.
            var saved = await this.subject.GetAsync(0, CancellationToken.None);

            // Assert.
            Assert.Equal(block.GetHash(), saved.GetHash());
        }

        [Fact]
        public async Task GetFirstAsync_WithEmptyTable_ShouldReturnNull()
        {
            // Act.
            var saved = await this.subject.GetFirstAsync(CancellationToken.None);

            // Assert.
            Assert.Null(saved);
        }

        [Fact]
        public async Task GetFirstAsync_HadGenesisBlock_ShouldSuccess()
        {
            // Arrange.
            var block = ZcoinNetworks.Instance.Regtest.GetGenesis();

            await this.subject.AddAsync(block, 0, CancellationToken.None);

            // Act.
            var saved = await this.subject.GetFirstAsync(CancellationToken.None);

            // Assert.
            Assert.Equal(block.GetHash(), saved.GetHash());
        }

        [Fact]
        public async Task GetLastAsync_WithEmptyTable_ShouldReturnNull()
        {
            // Act.
            var (saved, height) = await this.subject.GetLastAsync(CancellationToken.None);

            // Assert.
            Assert.Null(saved);
            Assert.Equal(0, height);
        }

        [Fact]
        public async Task GetLastAsync_HadNonGenesisBlock_ShouldSuccess()
        {
            // Arrange.
            var genesis = ZcoinNetworks.Instance.Regtest.GetGenesis();
            var block1 = Block.Parse(
                "000000205b88ee36b68031f3d28e7405c29b62c427228adbd9b5df8a6e91c24cf0982ba4e424d1c6617ab0c0f42bc21b3b2df34b21c10fadde6b26f03f5ab3283c852a6d4bcae75cffff00203f0000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff03510101ffffffff0600286bee00000000232102a0908147cd9c4f6f8d8004ff04d033b4f67ca2c26bb8193f266b15c4948f4702ac00c2eb0b000000001976a914296134d2415bf1f2b518b3f673816d7e603b160088ac00c2eb0b000000001976a914e1e1dc06a889c1b6d3eb00eef7a96f6a7cfb884888ac00c2eb0b000000001976a914ab03ecfddee6330497be894d16c29ae341c123aa88ac00c2eb0b000000001976a9144281a58a1d5b2d3285e00cb45a8492debbdad4c588ac00c2eb0b000000001976a9141fd264c0bb53bd9fef18e2248ddf1383d6e811ae88ac00000000",
                ZcoinNetworks.Instance.Regtest
            );

            await this.subject.AddAsync(genesis, 0, CancellationToken.None);
            await this.subject.AddAsync(block1, 1, CancellationToken.None);

            // Act.
            var (saved, height) = await this.subject.GetLastAsync(CancellationToken.None);

            // Assert.
            Assert.Equal(block1.GetHash(), saved.GetHash());
            Assert.Equal(1, height);
        }

        [Fact]
        public async Task GetTransactionAsync_PassNullForHash_ShouldThrow()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                "hash",
                () => this.subject.GetTransactionAsync(null, CancellationToken.None)
            );
        }

        [Fact]
        public async Task GetTransactionAsync_WithNonExistent_ShouldReturnNull()
        {
            // Act.
            var tx = await this.subject.GetTransactionAsync(uint256.One, CancellationToken.None);

            // Assert.
            Assert.Null(tx);
        }

        [Fact]
        public async Task GetTransactionAsync_WithExistent_ShouldReturnNonNull()
        {
            // Arrange.
            var block0 = ZcoinNetworks.Instance.Regtest.GetGenesis();

            await this.subject.AddAsync(block0, 0, CancellationToken.None);

            // Act.
            var tx = await this.subject.GetTransactionAsync(block0.Transactions[0].GetHash(), CancellationToken.None);

            // Assert.
            Assert.NotNull(tx);
            Assert.Equal(block0.Transactions[0].GetHash(), tx.GetHash());
        }

        [Fact]
        public async Task RemoveLastAsync_WithEmptyTable_ShouldNotThrow()
        {
            // Act.
            await this.subject.RemoveLastAsync(CancellationToken.None);
        }

        [Fact]
        public async Task RemoveLastAsync_WithNonEmptyTable_ShouldRemoveLastBlockAndAnyAssociatedData()
        {
            // Arrange.
            var network = ZcoinNetworks.Instance.Regtest;
            var sign = new byte[30];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(sign);
            }

            // Genesis.
            var genesis = network.GetGenesis();

            await this.subject.AddAsync(genesis, 0, CancellationToken.None);

            // Block 1.
            var block1 = genesis.CreateNextBlockWithCoinbase(BitcoinAddress.Create("TQmbucVmyc8YWrxA8YcirCdJwcFLYK9PPH", network), 1);

            await this.subject.AddAsync(block1, 1, CancellationToken.None);

            // Block 2.
            var block2 = block1.CreateNextBlockWithCoinbase(BitcoinAddress.Create("TQmbucVmyc8YWrxA8YcirCdJwcFLYK9PPH", network), 2);
            var tx1 = Transaction.Create(network);

            tx1.Inputs.Add(
                new OutPoint(block1.Transactions[0].GetHash(), 0),
                new Script(Op.GetPushOp(sign))
            );

            tx1.Outputs.Add(
                Money.Coins(10),
                BitcoinAddress.Create("THMdcCZXJvUGMHo4BVumsPvPQbzr87Wah7", network).ScriptPubKey
            );

            block2.AddTransaction(tx1);

            await this.subject.AddAsync(block2, 2, CancellationToken.None);

            // Block 3.
            var block3 = block2.CreateNextBlockWithCoinbase(BitcoinAddress.Create("TQmbucVmyc8YWrxA8YcirCdJwcFLYK9PPH", network), 3);
            var tx2 = Transaction.Create(network);

            tx2.Inputs.Add(
                new OutPoint(block2.Transactions[0].GetHash(), 0),
                new Script(Op.GetPushOp(sign))
            );

            tx2.Outputs.Add(
                Money.Coins(5),
                BitcoinAddress.Create("THMdcCZXJvUGMHo4BVumsPvPQbzr87Wah7", network).ScriptPubKey
            );

            block3.AddTransaction(tx1); // For testing duplicate TX bug in separated block.
            block3.AddTransaction(tx2);
            block3.AddTransaction(tx2); // For testing duplicate TX bug in the same block.

            await this.subject.AddAsync(block3, 3, CancellationToken.None);

            // Act.
            await this.subject.RemoveLastAsync(CancellationToken.None);

            // Assert.
            int height;
            uint256 hash;
            Block first, last;

            (block3, height) = await this.subject.GetAsync(block3.GetHash(), CancellationToken.None);
            Assert.Null(block3);
            Assert.Equal(0, height);

            block3 = await this.subject.GetAsync(3, CancellationToken.None);
            Assert.Null(block3);

            hash = block2.GetHash();
            (block2, height) = await this.subject.GetAsync(hash, CancellationToken.None);
            Assert.NotNull(block2);
            Assert.Equal(hash, block2.GetHash());
            Assert.Equal(2, height);
            Assert.Equal(tx1.GetHash(), block2.Transactions[1].GetHash());

            first = await this.subject.GetFirstAsync(CancellationToken.None);
            (last, height) = await this.subject.GetLastAsync(CancellationToken.None);

            Assert.Equal(genesis.GetHash(), first.GetHash());
            Assert.Equal(block2.GetHash(), last.GetHash());
            Assert.Equal(2, height);
        }
    }
}
