using System;
using System.Threading.Tasks;
using NBitcoin;

namespace Ztm.Zcoin.Synchronization
{
    public interface IAddressListener
    {
        Guid Id { get; }

        Task<bool> CreditConfirmedAsync(BitcoinAddress address, Money amount, int confirmation);

        Task<bool> CreditUnconfirmedAsync(BitcoinAddress address, Money amount, int confirmation);

        Task<bool> DebitConfirmedAsync(BitcoinAddress address, Money amount, int confirmation);

        Task<bool> DebitUnconfirmedAsync(BitcoinAddress address, Money amount, int confirmation);

        Task<AddressListeningType> StartListenAsync(BitcoinAddress address);
    }
}
