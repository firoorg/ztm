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
                ConsensusFactory = new ZcoinConsensusFactory(mtpSwitchTime: DateTimeOffset.FromUnixTimeSeconds(1544443200)),
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

            builder.SetGenesis("0200000000000000000000000000000000000000000000000000000000000000000000008327a4aae5254fd54eafc4b74b3b1e6b718539acabfdaec97013065da72a5d36dec55354f0ff0f1e382c02000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff5a04f0ff0f1e01044c4c54696d657320323031342f31302f3331204d61696e65204a756467652053617973204e75727365204d75737420466f6c6c6f772045626f6c612051756172616e74696e6520666f72204e6f7704823f0000ffffffff0100000000000000000000000000");
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
                ConsensusFactory = new ZcoinConsensusFactory(mtpSwitchTime: DateTimeOffset.MaxValue),
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

            builder.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000b8b7f5efce3af62564cb2b1035c711d9add9f59b38721e316ba6c70bd661b325dec55354ffff7f201ba4ae180101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff5a04f0ff0f1e01044c4c54696d657320323031342f31302f3331204d61696e65204a756467652053617973204e75727365204d75737420466f6c6c6f772045626f6c612051756172616e74696e6520666f72204e6f770408000000ffffffff0100000000000000000000000000");
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
                ConsensusFactory = new ZcoinConsensusFactory(mtpSwitchTime: DateTimeOffset.FromUnixTimeSeconds(1539172800)),
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

            builder.SetGenesis("020000000000000000000000000000000000000000000000000000000000000000000000b8b7f5efce3af62564cb2b1035c711d9add9f59b38721e316ba6c70bd661b325dec55354f0ff0f1eed6436000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff5a04f0ff0f1e01044c4c54696d657320323031342f31302f3331204d61696e65204a756467652053617973204e75727365204d75737420466f6c6c6f772045626f6c612051756172616e74696e6520666f72204e6f770408000000ffffffff0100000000000000000000000000");
            builder.SetMagic(0xeabefccf);
            builder.SetName("xzc-test");
            builder.SetPort(18168);
            builder.SetRPCPort(18888);

            return builder;
        }
    }
}
