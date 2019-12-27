using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NBitcoin;
using NBitcoin.RPC;
using Newtonsoft.Json.Serialization;
using Ztm.Configuration;
using Ztm.Data.Entity.Contexts;
using Ztm.Data.Entity.Postgres;
using Ztm.Hosting.AspNetCore;
using Ztm.WebApi.Binders;
using Ztm.WebApi.AddressPools;
using Ztm.WebApi.Callbacks;
using Ztm.WebApi.Watchers.TransactionConfirmation;
using Ztm.Zcoin.NBitcoin;
using Ztm.Zcoin.NBitcoin.Exodus;
using Ztm.Zcoin.Rpc;
using Ztm.Zcoin.Synchronization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;

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
            services.AddMvc(ConfigureMvc)
                    .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                    .AddJsonOptions(o =>
                    {
                        o.SerializerSettings.ContractResolver = new DefaultContractResolver()
                        {
                            NamingStrategy = new SnakeCaseNamingStrategy(),
                        };

                        var config = this.config.GetZcoinSection();
                        var network = ZcoinNetworks.Instance.GetNetwork(config.Network.Type);
                        o.SerializerSettings.Converters.Add(new BitcoinAddressJsonConverter(network));
                        o.SerializerSettings.Converters.Add(new UInt256JsonConverter());
                    });

            // Http Client Factory.
            services.AddHttpClient();

            // Address Pools.
            services.UseAddressPool();

            // Fundamentals Services.
            services.AddBackgroundServiceExceptionHandler();
            services.AddSingleton<Network>(CreateZcoinNetwork);

            services.AddSingleton<ZcoinConfiguration>(
                p => this.config.GetZcoinSection()
            );

            services.AddSingleton<JsonSerializer>(
                p =>
                {
                    var serializer = new JsonSerializer();

                    var network = p.GetRequiredService<Network>();
                    serializer.Converters.Add(new BitcoinAddressJsonConverter(network));
                    serializer.Converters.Add(new UInt256JsonConverter());

                    return serializer;
                }
            );

            // Database Services.
            services.AddSingleton<IMainDatabaseFactory, MainDatabaseFactory>();

            // Transaction Confirmation Watcher.
            services.AddSingleton<ICallbackExecuter, HttpCallbackExecuter>();
            services.AddSingleton<ICallbackRepository, EntityCallbackRepository>();
            services.AddSingleton<IRuleRepository, EntityRuleRepository>();
            services.AddSingleton<IWatchRepository, EntityWatchRepository>();
            services.AddSingleton<TransactionConfirmationWatcher>();
            services.AddSingleton<IBlockListener>(
                p => p.GetRequiredService<TransactionConfirmationWatcher>()
            );

            services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService, TransactionConfirmationWatcher>(
                p => p.GetRequiredService<TransactionConfirmationWatcher>()
            );

            services.AddSingleton<ITransactionConfirmationWatcher, TransactionConfirmationWatcher>(
                p => p.GetRequiredService<TransactionConfirmationWatcher>()
            );

            // Zcoin Interface Services.
            services.AddSingleton<IZcoinRpcClientFactory>(CreateZcoinRpcClientFactory);
            services.AddTransient<IBlocksRetriever, BlocksRetriever>();
            services.AddSingleton<IBlocksStorage, BlocksStorage>();

            // NBitcoin Services.
            services.AddNBitcoin();

            // Background Services.
            services.AddHostedService<BlocksSynchronizer>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseExceptionHandler("/error-development");
            }
            else
            {
                app.UseExceptionHandler("/error");
            }

            app.UseBackgroundServiceExceptionHandler("/background-service-error");
            app.UseMvc();
        }

        void ConfigureMvc(MvcOptions options)
        {
            // Custom Model Binders.
            options.ModelBinderProviders.Insert(0, new BitcoinAddressModelBinderProvider());
            options.ModelBinderProviders.Insert(0, new PropertyAmountModelBinderProvider());
            options.ModelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(BitcoinAddress)));
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
                RPCCredentialString.Parse($"{config.Rpc.UserName}:{config.Rpc.Password}"),
                provider.GetRequiredService<ITransactionEncoder>()
            );
        }
    }
}
