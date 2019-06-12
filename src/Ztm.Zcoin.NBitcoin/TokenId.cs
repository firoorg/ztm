using System;

namespace Ztm.Zcoin.NBitcoin
{
    public struct TokenId
    {
        readonly uint value;

        public TokenId(long value)
        {
            if (value <= 0 || value > uint.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "The value is not valid.");
            }

            this.value = (uint)value;
        }

        public bool IsValid => this.value != 0;

        public uint Value => IsValid ? this.value : throw new InvalidOperationException("The identifier is not valid.");

        public static implicit operator TokenId(long value)
        {
            return new TokenId(value);
        }
    }
}
