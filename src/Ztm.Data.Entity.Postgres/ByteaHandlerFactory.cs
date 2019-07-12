using Npgsql;
using Npgsql.TypeHandling;

namespace Ztm.Data.Entity.Postgres
{
    public class ByteaHandlerFactory : NpgsqlTypeHandlerFactory<byte[]>
    {
        protected override NpgsqlTypeHandler<byte[]> Create(NpgsqlConnection conn)
            => new ByteaHandler ();
    }

}