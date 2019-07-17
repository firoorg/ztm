using Microsoft.EntityFrameworkCore;
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

        protected override void ConfigureWebApiCallback(ModelBuilder modelBuilder)
        {
            base.ConfigureWebApiCallback(modelBuilder);

            modelBuilder.Entity<WebApiCallback>(b =>
            {
                b.Property(e => e.Url).HasConversion(Converters.UriToStringConverter);
            });
        }
    }
}
