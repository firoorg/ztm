using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage;
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

        protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
            => new UInt256TypeMapping(parameters);

        protected override string GenerateNonNullSqlLiteral(object value)
            => $"bytea '\\x{(uint256)value}'";

        public override Expression GenerateCodeLiteral(object value)
            => Expression.Call(
                typeof(uint256).GetMethod("Parse", new[] {typeof(string)}),
                Expression.Constant(((uint256)value).ToString())
            );
    }
}