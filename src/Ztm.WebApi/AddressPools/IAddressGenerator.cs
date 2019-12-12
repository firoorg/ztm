using System.Threading;
using System.Threading.Tasks;
using NBitcoin;

namespace Ztm.WebApi.AddressPools
{
    public interface IAddressGenerator
    {
        Task<BitcoinAddress> GenerateAsync(CancellationToken cancellationToken);
    }
}