using System;
using System.IO;
using System.Net;
using System.Text;
using Ztm.Zcoin.NBitcoin.Exodus;

namespace Ztm.Zcoin.NBitcoin.Tests.Exodus
{
    static class RawTransaction
    {
        public static MemoryStream Create(int type, int version)
        {
            var data = new MemoryStream();

            try
            {
                using (var writer = new BinaryWriter(data, Encoding.UTF8, true))
                {
                    writer.Write(IPAddress.HostToNetworkOrder((short)version));
                    writer.Write(IPAddress.HostToNetworkOrder((short)type));
                }
            }
            catch
            {
                data.Dispose();
                throw;
            }

            return data;
        }

        public static void WritePropertyId(Stream output, PropertyId id)
        {
            using (var writer = new BinaryWriter(output, Encoding.UTF8, true))
            {
                writer.Write(IPAddress.HostToNetworkOrder((int)id.Value));
            }
        }

        public static void WritePropertyId(Stream output, long id)
        {
            using (var writer = new BinaryWriter(output, Encoding.UTF8, true))
            {
                writer.Write(IPAddress.HostToNetworkOrder((int)Convert.ToUInt32(id)));
            }
        }

        public static void WritePropertyAmount(Stream output, PropertyAmount amount)
        {
            using (var writer = new BinaryWriter(output, Encoding.UTF8, true))
            {
                writer.Write(IPAddress.HostToNetworkOrder(amount.Indivisible));
            }
        }
    }
}
