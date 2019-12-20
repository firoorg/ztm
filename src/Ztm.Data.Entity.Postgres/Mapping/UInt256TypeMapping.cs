using System.Linq.Expressions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NBitcoin;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal.Mapping;
using NpgsqlTypes;

namespace Ztm.Data.Entity.Postgres.Mapping
{
    public class UInt256TypeMapping : NpgsqlTypeMapping
    {
        public UInt256TypeMapping() : base("bytea", typeof(uint256), NpgsqlDbType.Bytea)
        {
        }

        protected UInt256TypeMapping(RelationalTypeMappingParameters parameters)
            : base(parameters, NpgsqlDbType.Bytea)
        {
        }

        public override RelationalTypeMapping Clone(string storeType, int? size)
            => new UInt256TypeMapping(Parameters.WithStoreTypeAndSize(storeType, size));

        public override CoreTypeMapping Clone(ValueConverter converter)
            => new UInt256TypeMapping(Parameters.WithComposedConverter(converter));

        protected override string GenerateNonNullSqlLiteral(object value)
            => @"'\x" + (uint256)value + @"'";
    }
}