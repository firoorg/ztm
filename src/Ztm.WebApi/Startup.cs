using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NBitcoin;
using NBitcoin.RPC;
using Ztm.Configuration;
using Ztm.Data.Entity.Contexts;
using Ztm.Data.Entity.Postgres;
using Ztm.WebApi.Binders;
using Ztm.Zcoin.NBitcoin;
using Ztm.Zcoin.Rpc;
using Ztm.Zcoin.Synchronization;

namespace Ztm.WebApi
{
    public sealed class Startup
    {
        readonly IConfiguration config;

        public Startup(IConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            this.config = config;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // ASP.NET Services.
            services.AddMvc(ConfigureMvc).SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            // Fundamentals Services.
            services.AddSingleton<Network>(CreateZcoinNetwork);

            // Database Services.
            services.AddSingleton<IMainDatabaseFactory, MainDatabaseFactory>();

            // Zcoin Interface Services.
            services.AddSingleton<IZcoinRpcClientFactory>(CreateZcoinRpcClientFactory);
            services.AddTransient<IBlocksRetriever, BlocksRetriever>();
            services.AddSingleton<IBlocksStorage, BlocksStorage>();

            // Background Services.
            services.AddHostedService<BlocksSynchronizer>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
                app.UseHttpsRedirection();
            }

            app.UseMvc();
        }

        void ConfigureMvc(MvcOptions options)
        {
            // Custom Model Binders.
            options.ModelBinderProviders.Insert(0, new BitcoinAddressModelBinderProvider());
            options.ModelBinderProviders.Insert(0, new PropertyAmountModelBinderProvider());
        }

        Network CreateZcoinNetwork(IServiceProvider provider)
        {
            var config = this.config.GetZcoinSection();

            return ZcoinNetworks.Instance.GetNetwork(config.Network.Type);
        }

        IZcoinRpcClientFactory CreateZcoinRpcClientFactory(IServiceProvider provider)
        {
            var config = this.config.GetZcoinSection();

            return new ZcoinRpcClientFactory(
                config.Rpc.Address,
                config.Network.Type,
                RPCCredentialString.Parse($"{config.Rpc.UserName}:{config.Rpc.Password}")
            );
        }
    }
}
