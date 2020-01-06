using System.Threading.Tasks;
using NBitcoin;
using Npgsql;
using Npgsql.BackendMessages;
using Npgsql.TypeHandling;

namespace Ztm.Data.Entity.Postgres
{
    class ByteaHandler : Npgsql.TypeHandlers.ByteaHandler, INpgsqlTypeHandler<uint256>
    {
        async ValueTask<uint256> INpgsqlTypeHandler<uint256>.Read(NpgsqlReadBuffer buf, int len, bool async,
            FieldDescription fieldDescription)
        {
            var bytes = await base.Read(buf, len, async, fieldDescription);
            return new uint256(bytes, false);
        }

        async Task INpgsqlTypeHandler<uint256>.Write(uint256 value, NpgsqlWriteBuffer buf,
            NpgsqlLengthCache lengthCache, NpgsqlParameter parameter, bool async)
            => await Write(value.ToBytes(false), buf, lengthCache, parameter, async);

        int INpgsqlTypeHandler<uint256>.ValidateAndGetLength(uint256 value, ref NpgsqlLengthCache lengthCache,
            NpgsqlParameter parameter)
            => 32;
    }
}