using System;
using NBitcoin;

namespace Ztm.Zcoin.NBitcoin
{
    public class ZcoinBlock : Block
    {
        readonly ZcoinConsensusFactory consensusFactory;

        #pragma warning disable CS0618
        public ZcoinBlock(ZcoinConsensusFactory consensusFactory, BlockHeader header) : base(header)
        {
            if (consensusFactory == null)
            {
                throw new ArgumentNullException(nameof(consensusFactory));
            }

            this.consensusFactory = consensusFactory;
        }
        #pragma warning restore CS0618

        public new BlockHeader Header
        {
            get { return base.Header; }
        }

        public static new ZcoinBlock CreateBlock(Network network)
        {
            return (ZcoinBlock)CreateBlock(network.Consensus.ConsensusFactory);
        }

        public static new ZcoinBlock Parse(string hex, Network network)
        {
            return (ZcoinBlock)Parse(hex, network.Consensus.ConsensusFactory);
        }

        public new ZcoinBlock CreateNextBlockWithCoinbase(BitcoinAddress address, int height)
        {
            return (ZcoinBlock)base.CreateNextBlockWithCoinbase(address, height);
        }

        public new ZcoinBlock CreateNextBlockWithCoinbase(BitcoinAddress address, int height, DateTimeOffset now)
        {
            return (ZcoinBlock)base.CreateNextBlockWithCoinbase(address, height, now);
        }

        public override ConsensusFactory GetConsensusFactory()
        {
            return this.consensusFactory;
        }
    }
}
