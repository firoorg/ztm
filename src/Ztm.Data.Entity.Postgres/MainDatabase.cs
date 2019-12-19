using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ztm.Data.Entity.Contexts.Main;
using Npgsql;
using Npgsql.TypeMapping;
using NpgsqlTypes;
using NBitcoin;
using System;

namespace Ztm.Data.Entity.Postgres
{
    public class MainDatabase : Ztm.Data.Entity.Contexts.MainDatabase
    {
        static MainDatabase()
        {
            NpgsqlConnection.GlobalTypeMapper.AddMapping(new NpgsqlTypeMappingBuilder
            {
                PgTypeName = "bytea",
                NpgsqlDbType = NpgsqlDbType.Bytea,
                ClrTypes = new[] { typeof(uint256), typeof(byte[]), typeof(ArraySegment<byte>) },
                TypeHandlerFactory = new ByteaHandlerFactory()
            }.Build());
        }

        public MainDatabase(DbContextOptions<Ztm.Data.Entity.Contexts.MainDatabase> options) : base(options)
        {
        }

        protected override void ConfigureWebApiCallback(EntityTypeBuilder<WebApiCallback> builder)
        {
            base.ConfigureWebApiCallback(builder);

            builder.Property(e => e.Url).HasConversion(Converters.UriToStringConverter);
        }

        protected override void ConfigureWebApiCallbackHistory(EntityTypeBuilder<WebApiCallbackHistory> builder)
        {
            base.ConfigureWebApiCallbackHistory(builder);

            builder.Property(e => e.Data).HasColumnType("jsonb");
        }

        protected override void ConfigureTransactionConfirmationWatcherRule(
            EntityTypeBuilder<TransactionConfirmationWatcherRule> builder)
        {
            base.ConfigureTransactionConfirmationWatcherRule(builder);

            builder.Property(e => e.SuccessData).HasColumnType("jsonb");
            builder.Property(e => e.TimeoutData).HasColumnType("jsonb");
        }
    }
}
