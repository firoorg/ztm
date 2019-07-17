using System;
using System.Threading.Tasks;
using NBitcoin;
using Npgsql;
using Npgsql.BackendMessages;
using Npgsql.TypeHandling;

namespace Ztm.Data.Entity.Postgres
{
    class ByteaHandler : Npgsql.TypeHandlers.ByteaHandler, INpgsqlTypeHandler<uint256>
    {
        async ValueTask<uint256> INpgsqlTypeHandler<uint256>.Read(NpgsqlReadBuffer buf, int len, bool async, FieldDescription fieldDescription)
        {
            try
            {
                var bytes = new byte[len];
                await buf.ReadBytes(bytes, 0, len, async);
                return new uint256(bytes, false);
            }
            catch (Exception e)
            {
                throw new NpgsqlSafeReadException(e);
            }
        }

        public async Task Write(uint256 value, NpgsqlWriteBuffer buf, NpgsqlLengthCache lengthCache, NpgsqlParameter parameter, bool async)
        {
            await buf.WriteBytesRaw(value.ToBytes(false), async);
        }

        int INpgsqlTypeHandler<uint256>.ValidateAndGetLength(uint256 value, ref NpgsqlLengthCache lengthCache, NpgsqlParameter parameter)
            => 32;
    }
}