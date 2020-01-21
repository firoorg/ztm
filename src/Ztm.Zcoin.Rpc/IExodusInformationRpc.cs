using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.Rpc
{
    public interface IExodusInformationRpc : IDisposable
    {
        Task<(PropertyAmount balance, PropertyAmount reserved)> GetBalanceAsync(
            BitcoinAddress address,
            Property property,
            CancellationToken cancellationToken);

        Task<PropertyGrantsInfo> GetGrantsAsync(Property property, CancellationToken cancellationToken);

        Task<byte[]> GetPayloadAsync(uint256 transaction, CancellationToken cancellationToken);

        Task<ExodusTransactionInformation> GetTransactionAsync(uint256 hash, CancellationToken cancellationToken);

        Task<IEnumerable<PropertyInfo>> ListPropertiesAsync(CancellationToken cancellationToken);
    }
}
