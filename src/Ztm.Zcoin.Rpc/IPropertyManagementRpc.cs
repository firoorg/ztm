using System;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.Rpc
{
    public interface IPropertyManagementRpc : IDisposable
    {
        Task<Transaction> CreateManagedAsync(
            BitcoinAddress owner,
            Ecosystem ecosystem,
            PropertyType type,
            Property current,
            string category,
            string subcategory,
            string name,
            string url,
            string description,
            CancellationToken cancellationToken);

        Task<Transaction> GrantAsync(
            Property property,
            BitcoinAddress from,
            BitcoinAddress to,
            PropertyAmount amount,
            string note,
            CancellationToken cancellationToken);

        Task<Transaction> SendAsync(
            BitcoinAddress from,
            BitcoinAddress to,
            Property property,
            PropertyAmount amount,
            Money referenceAmount,
            CancellationToken cancellationToken);
    }
}
