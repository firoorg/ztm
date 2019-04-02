using System;
using NBitcoin;

namespace Ztm.Zcoin.NBitcoin
{
    public sealed class ZcoinNetworks : NetworkSetBase
    {
        private ZcoinNetworks()
        {
        }

        public static ZcoinNetworks Instance { get; } = new ZcoinNetworks();

        public override string CryptoCode => "XZC";

        protected override NetworkBuilder CreateMainnet()
        {
            var builder = new NetworkBuilder();

            builder.AddAlias("xzc-mainnet");
            builder.AddAlias("zcoin-mainnet");
            builder.AddAlias("zcoin-main");

            builder.AddDNSSeeds(new[]
            {
                new DNSSeedData("amsterdam.zcoin.io", "amsterdam.zcoin.io"),
                new DNSSeedData("australia.zcoin.io", "australia.zcoin.io"),
                new DNSSeedData("chicago.zcoin.io", "chicago.zcoin.io"),
                new DNSSeedData("london.zcoin.io", "london.zcoin.io"),
                new DNSSeedData("frankfurt.zcoin.io", "frankfurt.zcoin.io"),
                new DNSSeedData("newjersey.zcoin.io", "newjersey.zcoin.io"),
                new DNSSeedData("sanfrancisco.zcoin.io", "sanfrancisco.zcoin.io"),
                new DNSSeedData("tokyo.zcoin.io", "tokyo.zcoin.io"),
                new DNSSeedData("singapore.zcoin.io", "singapore.zcoin.io")
            });

            builder.AddSeeds(ToSeed(new[]
            {
                Tuple.Create(new byte[] { 0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x2d,0x4d,0xcd,0x05 }, 8168),
                Tuple.Create(new byte[] { 0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x2d,0x20,0xe8,0xc3 }, 8168),
                Tuple.Create(new byte[] { 0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x68,0xee,0xac,0xa4 }, 8168),
                Tuple.Create(new byte[] { 0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x2d,0x3f,0x5b,0x97 }, 8168),
                Tuple.Create(new byte[] { 0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x2d,0x4d,0xb8,0xbb }, 8168),
                Tuple.Create(new byte[] { 0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x2d,0x4d,0x33,0x62 }, 8168),
                Tuple.Create(new byte[] { 0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x2d,0x4d,0x3d,0xcb }, 8168),
                Tuple.Create(new byte[] { 0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x2d,0x4c,0xce,0x0f }, 8168),
                Tuple.Create(new byte[] { 0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x2d,0x4c,0x95,0x7e }, 8168)
            }));

            builder.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 82 });
            builder.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 7 });
            builder.SetBase58Bytes(Base58Type.SECRET_KEY, new[] { (byte)210 });
            builder.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x88, 0xB2, 0x1E });
            builder.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0xAD, 0xE4 });

            builder.SetConsensus(new Consensus()
            {
                BIP34Hash = new uint256("000000000000024b89b42a942fe0d9fea3bb44ab7bd1b19115dd6a759c0808b8"),
                CoinbaseMaturity = 100,
                ConsensusFactory = ZcoinConsensusFactory.Instance,
                MajorityEnforceBlockUpgrade = 750,
                MajorityRejectBlockOutdated = 950,
                MajorityWindow = 1000,
                MinerConfirmationWindow = 2016,
                MinimumChainWork = new uint256("0000000000000000000000000000000000000000000000000708f98bf623f02e"),
                PowAllowMinDifficultyBlocks = false,
                PowLimit = new Target(new uint256("00ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
                PowNoRetargeting = false,
                PowTargetSpacing = new TimeSpan(hours: 0, minutes: 10, seconds: 0),
                PowTargetTimespan = new TimeSpan(hours: 1, minutes: 0, seconds: 0),
                RuleChangeActivationThreshold = 1916,
                SubsidyHalvingInterval = 305000,
                SupportSegwit = false
            });

            builder.SetGenesis("4381deb85b1b2c9843c222944b616d997516dcbd6a964e1eaf0def0830695233");
            builder.SetMagic(0xf1fed9e3);
            builder.SetName("xzc-main");
            builder.SetPort(8168);
            builder.SetRPCPort(8888);

            return builder;
        }

        protected override NetworkBuilder CreateRegtest()
        {
            var builder = new NetworkBuilder();

            builder.AddAlias("xzc-regtest");
            builder.AddAlias("zcoin-regtest");
            builder.AddAlias("zcoin-reg");

            builder.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 65 });
            builder.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 178 });
            builder.SetBase58Bytes(Base58Type.SECRET_KEY, new[] { (byte)239 });
            builder.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF });
            builder.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 });

            builder.SetConsensus(new Consensus()
            {
                CoinbaseMaturity = 100,
                ConsensusFactory = ZcoinConsensusFactory.Instance,
                MajorityEnforceBlockUpgrade = 750,
                MajorityRejectBlockOutdated = 950,
                MajorityWindow = 1000,
                MinerConfirmationWindow = 144,
                MinimumChainWork = new uint256(0),
                PowAllowMinDifficultyBlocks = true,
                PowLimit = new Target(new uint256("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
                PowNoRetargeting = true,
                PowTargetSpacing = new TimeSpan(hours: 0, minutes: 0, seconds: 1),
                PowTargetTimespan = new TimeSpan(hours: 1 * 1000, minutes: 0, seconds: 0),
                RuleChangeActivationThreshold = 108,
                SubsidyHalvingInterval = 305000,
                SupportSegwit = false
            });

            builder.SetGenesis("a42b98f04cc2916e8adfb5d9db8a2227c4629bc205748ed2f33180b636ee885b");
            builder.SetMagic(0xdab5bffa);
            builder.SetName("xzc-reg");
            builder.SetPort(18444);
            builder.SetRPCPort(28888);

            return builder;
        }

        protected override NetworkBuilder CreateTestnet()
        {
            var builder = new NetworkBuilder();

            builder.AddAlias("xzc-testnet");
            builder.AddAlias("zcoin-testnet");
            builder.AddAlias("zcoin-test");

            builder.AddDNSSeeds(new[]
            {
                new DNSSeedData("MTP1", "mtp1.zcoin.io"),
                new DNSSeedData("MTP2", "mtp2.zcoin.io")
            });

            builder.AddSeeds(ToSeed(new[]
            {
                Tuple.Create(new byte[] { 0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0xff,0xff,0x34,0xaf,0xf4,0x16 }, 18168)
            }));

            builder.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 65 });
            builder.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 178 });
            builder.SetBase58Bytes(Base58Type.SECRET_KEY, new[] { (byte)185 });
            builder.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF });
            builder.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 });

            builder.SetConsensus(new Consensus()
            {
                BIP34Hash = new uint256("0000000023b3a96d3484e5abb3755c413e7d41500f8e2a5c3f0dd01299cd8ef8"),
                CoinbaseMaturity = 100,
                ConsensusFactory = ZcoinConsensusFactory.Instance,
                MajorityEnforceBlockUpgrade = 51,
                MajorityRejectBlockOutdated = 75,
                MajorityWindow = 100,
                MinerConfirmationWindow = 2016,
                MinimumChainWork = new uint256("0000000000000000000000000000000000000000000000000708f98bf623f02e"),
                PowAllowMinDifficultyBlocks = true,
                PowLimit = new Target(new uint256("00ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
                PowNoRetargeting = false,
                PowTargetSpacing = new TimeSpan(hours: 0, minutes: 5, seconds: 0),
                PowTargetTimespan = new TimeSpan(hours: 1, minutes: 0, seconds: 0),
                RuleChangeActivationThreshold = 1512,
                SubsidyHalvingInterval = 305000,
                SupportSegwit = false
            });

            builder.SetGenesis("1e3487fdb1a7d46dac3e8f3e58339c6eff54abf6aef353485f3ed64250a35e89");
            builder.SetMagic(0xeabefccf);
            builder.SetName("xzc-test");
            builder.SetPort(18168);
            builder.SetRPCPort(18888);

            return builder;
        }
    }
}
