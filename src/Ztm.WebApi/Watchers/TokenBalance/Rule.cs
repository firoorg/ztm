using System;
using NBitcoin;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.WebApi.Watchers.TokenBalance
{
    public sealed class Rule
    {
        public Rule(
            PropertyId property,
            BitcoinAddress address,
            PropertyAmount targetAmount,
            int targetConfirmation,
            TimeSpan originalTimeout,
            string timeoutStatus,
            Guid callback)
            : this(
                property,
                address,
                targetAmount,
                targetConfirmation,
                originalTimeout,
                timeoutStatus,
                callback,
                Guid.NewGuid())
        {
        }

        public Rule(
            PropertyId property,
            BitcoinAddress address,
            PropertyAmount targetAmount,
            int targetConfirmation,
            TimeSpan originalTimeout,
            string timeoutStatus,
            Guid callback,
            Guid id)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (targetAmount <= PropertyAmount.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(targetAmount),
                    targetAmount,
                    "The value is not a valid target amount.");
            }

            if (targetConfirmation < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(targetConfirmation),
                    targetConfirmation,
                    "The value is not a valid target confirmation.");
            }

            if (originalTimeout < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(originalTimeout),
                    originalTimeout,
                    "The value is not a valid timeout.");
            }

            if (timeoutStatus == null)
            {
                throw new ArgumentNullException(nameof(timeoutStatus));
            }

            Property = property;
            Address = address;
            TargetAmount = targetAmount;
            TargetConfirmation = targetConfirmation;
            OriginalTimeout = originalTimeout;
            TimeoutStatus = timeoutStatus;
            Callback = callback;
            Id = id;
        }

        public BitcoinAddress Address { get; }

        public Guid Callback { get; set; }

        public Guid Id { get; }

        public TimeSpan OriginalTimeout { get; }

        public PropertyId Property { get; }

        public PropertyAmount TargetAmount { get; }

        public int TargetConfirmation { get; }

        public string TimeoutStatus { get; }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
            {
                return false;
            }

            return ((Rule)obj).Id == Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
