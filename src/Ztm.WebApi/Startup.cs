using System;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NBitcoin.RPC;
using Ztm.Configuration;
using Ztm.Data.Entity.Contexts;
using Ztm.Data.Entity.Postgres;
using Ztm.WebApi.Binders;
using Ztm.Zcoin.Rpc;
using Ztm.Zcoin.Synchronization;

namespace Ztm.WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // ASP.NET Services.
            services.AddMvc(ConfigureMvc).SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            // Database Services.
            services.AddSingleton<IMainDatabaseFactory, MainDatabaseFactory>();

            // Transaction Confirmation Watcher.
            services.AddHttpClient<ICallbackExecuter, HttpCallbackExecuter>();
            services.AddTransient<ICallbackRepository, SqlCallbackRepository>();
            services.AddTransient<
                ITransactionConfirmationWatchRepository<TransactionConfirmationCallbackResult>,
                SqlTransactionConfirmationWatchRepository<TransactionConfirmationCallbackResult>>();
            services.AddHostedService<TransactionConfirmationWatcher>();
            services.AddSingleton<IBlockListener, TransactionConfirmationWatcher>();

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
            options.ModelBinderProviders.Insert(0, new PropertyAmountModelBinderProvider());
        }

        IZcoinRpcClientFactory CreateZcoinRpcClientFactory(IServiceProvider provider)
        {
            var config = Configuration.GetZcoinSection();

            return new ZcoinRpcClientFactory(
                config.Rpc.Address,
                config.Network.Type,
                RPCCredentialString.Parse($"{config.Rpc.UserName}:{config.Rpc.Password}")
            );
        }
    }
}
