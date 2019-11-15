using NBitcoin;
using Ztm.Zcoin.NBitcoin;

namespace Ztm.Testing
{
    public static class TestAddress
    {
        public static readonly BitcoinAddress Mainnet1 = BitcoinAddress.Create(
            "a8ULhhDgfdSiXJhSZVdhb8EuDc6R3ogsaM",
            ZcoinNetworks.Instance.Mainnet
        );

        public static readonly BitcoinAddress Testnet1 = BitcoinAddress.Create(
            "TEDC38GBncNgtd2pVXeDhLeUGwJmXsiJBA",
            ZcoinNetworks.Instance.Testnet
        );

        public static readonly BitcoinAddress Testnet2 = BitcoinAddress.Create(
            "TG3Pnw5xPZQS8JXMVa3F9WjUFfUqXKsqAz",
            ZcoinNetworks.Instance.Testnet
        );

        public static readonly BitcoinAddress Regtest1 = BitcoinAddress.Create(
            "TEDC38GBncNgtd2pVXeDhLeUGwJmXsiJBA",
            ZcoinNetworks.Instance.Regtest
        );

        public static readonly BitcoinAddress Regtest2 = BitcoinAddress.Create(
            "TG3Pnw5xPZQS8JXMVa3F9WjUFfUqXKsqAz",
            ZcoinNetworks.Instance.Regtest
        );
    }
}
