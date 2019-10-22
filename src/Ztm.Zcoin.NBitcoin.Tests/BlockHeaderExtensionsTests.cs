using System;
using Xunit;
using NBitcoin;

namespace Ztm.Zcoin.NBitcoin.Tests
{
    public sealed class BlockHeaderExtensionsTests
    {
        readonly BlockHeader subject;

        public BlockHeaderExtensionsTests()
        {
            this.subject = ZcoinNetworks.Instance.Mainnet.Consensus.ConsensusFactory.CreateBlockHeader();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1414776286)]
        [InlineData(1544443199)]
        public void IsMtp_WithTimeBeforeMtpSwitchTime_ShouldReturnFalse(long blockTime)
        {
            this.subject.BlockTime = DateTimeOffset.FromUnixTimeSeconds(blockTime);

            Assert.False(this.subject.IsMtp());
        }

        [Theory]
        [InlineData(1544443200)]
        [InlineData(1544443201)]
        [InlineData(UInt32.MaxValue)]
        public void IsMtp_WithTimeAfterOrEqualMtpSwitchTime_ShouldReturnTrue(long blockTime)
        {
            subject.BlockTime = DateTimeOffset.FromUnixTimeSeconds(blockTime);

            Assert.True(this.subject.IsMtp());
        }

        [Fact]
        public void GetSetMtpHashData_ShouldReturnSameObject()
        {
            var hashData = new MTPHashData();
            subject.SetMtpHashData(hashData);

            Assert.Same(hashData, subject.GetMtpHashData());
        }

        [Fact]
        public void GetSetMtpHashValue_ShouldReturnSameObject()
        {
            var hashValue = new uint256();
            subject.SetMtpHashValue(hashValue);

            Assert.Same(hashValue, subject.GetMtpHashValue());
        }

        [Fact]
        public void GetMtpVersion_WithoutSetOtherValue_ShouldReturnDefault()
        {
            Assert.Equal(0x1000, subject.GetMtpVersion());
        }

        [Theory]
        [InlineData(Int32.MinValue)]
        [InlineData(0)]
        [InlineData(99)]
        [InlineData(Int32.MaxValue)]
        public void GetSetMtpVersion_ShouldReturnSameValue(int version)
        {
            subject.SetMtpVersion(version);

            Assert.Equal(version, subject.GetMtpVersion());
        }

        [Fact]
        public void GetSetReserved1_ShouldReturnSameObject()
        {
            var reserved1 = new uint256();
            subject.SetReserved1(reserved1);

            Assert.Same(reserved1, subject.GetReserved1());
        }

        [Fact]
        public void GetSetReserved2_ShouldReturnSameObject()
        {
            var reserved2 = new uint256();
            subject.SetReserved2(reserved2);

            Assert.Same(reserved2, subject.GetReserved2());
        }
    }
}
