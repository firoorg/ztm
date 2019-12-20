using Microsoft.EntityFrameworkCore.Storage;
using NBitcoin;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal;
using Ztm.Data.Entity.Postgres.Mapping;

namespace Ztm.Data.Entity.Postgres
{
    class TypeMappingSource : NpgsqlTypeMappingSource
    {
        public TypeMappingSource(
            TypeMappingSourceDependencies dependencies,
            RelationalTypeMappingSourceDependencies relationalDependencies,
            INpgsqlOptions npgsqlOptions = null) :
            base(dependencies, relationalDependencies, npgsqlOptions)
        {
        }

        protected override RelationalTypeMapping FindMapping(in RelationalTypeMappingInfo mappingInfo)
        {
            var mapping = base.FindMapping(mappingInfo);

            if (mapping != null)
            {
                return mapping;
            }

            if (mappingInfo.ClrType == typeof(uint256))
            {
                return new UInt256TypeMapping();
            }

            return null;
        }
    }
}