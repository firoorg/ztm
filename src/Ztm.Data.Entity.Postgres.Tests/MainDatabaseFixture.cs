using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Ztm.Data.Entity.Postgres.Tests
{
    public sealed class MainDatabaseFixture : IDisposable
    {
        readonly DbConnection connection;
        readonly DbContextOptionsBuilder<Ztm.Data.Entity.Contexts.MainDatabase> optionsBuilder;

        public MainDatabaseFixture()
        {
            var connectionString = Environment.GetEnvironmentVariable("ZTM_MAIN_DATABASE");
            if (connectionString == null)
            {
                throw new Exception("No ZTM_MAIN_DATABASE environment variable is set.");
            }

            this.connection  = new NpgsqlConnection(connectionString);

            try
            {
                this.connection.Open();

                this.optionsBuilder = new DbContextOptionsBuilder<Ztm.Data.Entity.Contexts.MainDatabase>();
                this.optionsBuilder.UseNpgsql(this.connection);
                this.optionsBuilder.UseCustomTypeMappingSource();
            }
            catch
            {
                this.connection.Dispose();
            }
        }

        public async Task CleanUpAsync(CancellationToken cancellationToken)
        {
            // Truncate all tables in schema public except __EFMigrationsHistory
            var tables = this.ExecuteSql("SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_name <> '__EFMigrationsHistory';")
                             .Select(r => r[0] as string);

            var concatedTableNames = string.Join(", ", tables.Select(t => $"\"{t}\""));
            var query = $"TRUNCATE TABLE {concatedTableNames}";
            using (var command = this.connection.CreateCommand())
            {
                command.CommandText = query;
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        public MainDatabase CreateDbContext()
        {
            return new MainDatabase(optionsBuilder.Options);
        }

        public void Dispose()
        {
            this.connection.Dispose();
        }

        public IEnumerable<IDataRecord> ExecuteSql(string rawSql)
        {
            using (var command = this.connection.CreateCommand())
            {
                command.CommandText = rawSql;
                using (var result = command.ExecuteReader())
                {
                    while (result.Read())
                    {
                        yield return result;
                    }
                }
            }
        }
    }
}