using System;
using System.Threading.Tasks;
using NBitcoin;

namespace Ztm.Zcoin.Synchronization
{
    public interface IAddressListener
    {
        Guid Id { get; }

        Task<bool> CreditConfirmAsync(BitcoinAddress address, Money amount, ConfirmationType type, int confirmation);

        Task<bool> DebitConfirmAsync(BitcoinAddress address, Money amount, ConfirmationType type, int confirmation);

        Task<AddressListeningType> StartListenAsync(BitcoinAddress address);
    }
}
