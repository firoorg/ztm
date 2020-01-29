using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Ztm.Data.Entity.Contexts;

namespace Ztm.Data.Entity.Testing
{
    public sealed class TestMainDatabaseFactory : IMainDatabaseFactory, IDisposable
    {
        readonly ILoggerFactory logger;
        readonly SqliteConnection connection;

        public TestMainDatabaseFactory()
        {
            try
            {
                var logger = new DebugLoggerProvider();

                try
                {
                    this.logger = new LoggerFactory(new[] { logger });
                }
                catch
                {
                    logger.Dispose();
                    throw;
                }

                this.connection = new SqliteConnection("DataSource=:memory:");
                this.connection.Open();
            }
            catch
            {
                this.connection?.Dispose();
                this.logger?.Dispose();
                throw;
            }
        }

        public MainDatabase CreateDbContext()
        {
            var builder = new DbContextOptionsBuilder<MainDatabase>();

            builder.EnableDetailedErrors();
            builder.EnableSensitiveDataLogging();
            builder.UseSqlite(this.connection);
            builder.UseLoggerFactory(this.logger);

            var context = new TestMainDatabase(builder.Options);

            try
            {
                context.Database.EnsureCreated();
            }
            catch
            {
                context.Dispose();
                throw;
            }

            return context;
        }

        public void Dispose()
        {
            this.connection.Dispose();
            this.logger.Dispose();
        }
    }
}
