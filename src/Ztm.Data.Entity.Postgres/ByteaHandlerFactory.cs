using System;
using Npgsql;
using Npgsql.TypeHandling;

namespace Ztm.Data.Entity.Postgres
{
    class ByteaHandlerFactory : NpgsqlTypeHandlerFactory<byte[]>
    {
        protected override NpgsqlTypeHandler<byte[]> Create(NpgsqlConnection conn)
            => new ByteaHandler();
    }

}