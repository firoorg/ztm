using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Ztm.Data.Entity.Postgres.Tests
{
    public sealed class MainDatabaseFactoryTests
    {
        readonly MainDatabaseFactory subject;

        public MainDatabaseFactoryTests()
        {
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(new Dictionary<string, string>
            {
                {"Database:Main:ConnectionString", "Host=127.0.0.1;Database=ztm;Username=ztm;Password=1234"},
            });

            this.subject = new MainDatabaseFactory(builder.Build());
        }

        [Fact]
        public void Construct_WithNullConfig_ShouldThrow()
        {
            Action act;

            act = () => new MainDatabaseFactory(null);
            act.Should().Throw<ArgumentNullException>()
               .And.ParamName.Should().Be("config");
        }

        [Fact]
        public void CreateDbContext_ShouldSuccess()
        {
            // Act.
            var context = this.subject.CreateDbContext();

            // Assert.
            context.Should().NotBeNull();
            context.Database.ProviderName.Should().Be("Npgsql.EntityFrameworkCore.PostgreSQL");
        }
    }
}