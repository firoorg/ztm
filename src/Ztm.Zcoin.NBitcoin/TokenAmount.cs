using System;

namespace Ztm.Zcoin.NBitcoin
{
    public struct TokenAmount
    {
        readonly long value;
        readonly TokenType type;

        private TokenAmount(long value, TokenType type)
        {
            this.value = value;
            this.type = type;
        }

        public bool IsValid => this.value > 0 && Enum.IsDefined(typeof(TokenType), this.type);

        public TokenType Type => IsValid ? this.type : throw new InvalidOperationException("The amount is not valid.");

        public static TokenAmount Divisible(decimal value)
        {
            if (value <= 0 || value > 92233720368.54775807m || (value % 0.00000001m) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "The value is not valid.");
            }

            value *= 100000000;

            return new TokenAmount((long)value, TokenType.Divisible);
        }

        public static TokenAmount Indivisible(long value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "The value is not valid.");
            }

            return new TokenAmount(value, TokenType.Indivisible);
        }

        public static TokenAmount Parse(string s)
        {
            var amount = decimal.Parse(s);

            if (amount < 0)
            {
                throw new FormatException("The string is not valid.");
            }
            else if (amount == 0)
            {
                return default(TokenAmount);
            }

            var type = (s.IndexOf('.') != -1) ? TokenType.Divisible : TokenType.Indivisible;

            if (type == TokenType.Divisible)
            {
                if (amount > 92233720368.54775807m || (amount % 0.00000001m) != 0)
                {
                    throw new FormatException("The string is not valid.");
                }

                amount *= 100000000;
            }
            else if (amount > long.MaxValue)
            {
                throw new FormatException("The string is not valid.");
            }

            return new TokenAmount((long)amount, type);
        }

        public override string ToString()
        {
            if (!IsValid)
            {
                return "";
            }

            switch (this.type)
            {
                case TokenType.Divisible:
                    return ((decimal)this.value / 100000000).ToString("0.00000000");
                case TokenType.Indivisible:
                    return this.value.ToString();
                default:
                    throw new InvalidOperationException($"Token type {this.type} is not valid.");
            }
        }
    }
}
