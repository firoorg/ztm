using System;
using Ztm.WebApi.AddressPools;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.WebApi.Watchers.TokenReceiving
{
    public sealed class Rule
    {
        public Rule(
            PropertyId property,
            ReceivingAddressReservation addressReservation,
            PropertyAmount targetAmount,
            int targetConfirmation,
            TimeSpan originalTimeout,
            TokenReceivingCallback callback)
            : this(
                property,
                addressReservation,
                targetAmount,
                targetConfirmation,
                originalTimeout,
                callback,
                Guid.NewGuid())
        {
        }

        public Rule(
            PropertyId property,
            ReceivingAddressReservation addressReservation,
            PropertyAmount targetAmount,
            int targetConfirmation,
            TimeSpan originalTimeout,
            TokenReceivingCallback callback,
            Guid id)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            if (addressReservation == null)
            {
                throw new ArgumentNullException(nameof(addressReservation));
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

            Property = property;
            AddressReservation = addressReservation;
            TargetAmount = targetAmount;
            TargetConfirmation = targetConfirmation;
            OriginalTimeout = originalTimeout;
            Callback = callback;
            Id = id;
        }

        public ReceivingAddressReservation AddressReservation { get; }

        public TokenReceivingCallback Callback { get; }

        public Guid Id { get; }

        public TimeSpan OriginalTimeout { get; }

        public PropertyId Property { get; }

        public PropertyAmount TargetAmount { get; }

        public int TargetConfirmation { get; }

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
