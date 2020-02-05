using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NBitcoin;
using NBitcoin.RPC;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Ztm.Configuration;
using Ztm.Data.Entity.Contexts;
using Ztm.Data.Entity.Postgres;
using Ztm.Hosting.AspNetCore;
using Ztm.Threading;
using Ztm.Threading.TimerSchedulers;
using Ztm.WebApi.AddressPools;
using Ztm.WebApi.Binders;
using Ztm.WebApi.Callbacks;
using Ztm.WebApi.Controllers;
using Ztm.WebApi.Watchers.TokenReceiving;
using Ztm.WebApi.Watchers.TransactionConfirmation;
using Ztm.Zcoin.NBitcoin;
using Ztm.Zcoin.NBitcoin.Exodus;
using Ztm.Zcoin.NBitcoin.Json;
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
            // ASP.NET Related Services.
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
                        o.SerializerSettings.Converters.Add(new BitcoinAddressConverter(network));
                        o.SerializerSettings.Converters.Add(new UInt256Converter());
                    });

            services.AddSingleton<JsonSerializer>(
                p =>
                {
                    var serializer = new JsonSerializer();

                    var network = p.GetRequiredService<Network>();
                    serializer.Converters.Add(new BitcoinAddressConverter(network));
                    serializer.Converters.Add(new UInt256Converter());

                    return serializer;
                });

            services.AddHttpClient();

            // Application Fundamentals Services.
            services.AddSingleton<ITimerScheduler, ThreadPoolScheduler>();
            services.AddBackgroundServiceExceptionHandler();
            services.AddSingleton<Network>(CreateZcoinNetwork);
            services.AddSingleton<ZcoinConfiguration>(p => p.GetRequiredService<IConfiguration>().GetZcoinSection());
            services.AddSingleton<PropertyId>(p => p.GetRequiredService<ZcoinConfiguration>().Property.Id);

            // NBitcoin Services.
            services.AddNBitcoin();

            // Database Services.
            services.AddSingleton<IMainDatabaseFactory, MainDatabaseFactory>();

            // Zcoin Interface Services.
            services.AddSingleton<IRpcFactory>(CreateRpcFactory);
            services.AddTransient<IBlocksRetriever, BlocksRetriever>();
            services.AddSingleton<IBlocksStorage, BlocksStorage>();

            // Callback Services.
            services.AddSingleton<ICallbackExecuter, HttpCallbackExecuter>();
            services.AddSingleton<ICallbackRepository, EntityCallbackRepository>();

            // Address Pools.
            services.UseAddressPool();

            // Watchers.
            services.AddTokenReceivingWatcher();
            services.AddTransactionConfirmationWatcher();

            // Background Services.
            services.AddHostedService<BlocksSynchronizer>();

            // Helper Controller.
            services.AddSingleton<ControllerHelper>();
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

            // FIXME: remove this when upgrade to .NET Core version >= 2.2
            options.ModelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(BitcoinAddress)));
        }

        Network CreateZcoinNetwork(IServiceProvider provider)
        {
            var config = this.config.GetZcoinSection();

            return ZcoinNetworks.Instance.GetNetwork(config.Network.Type);
        }

        IRpcFactory CreateRpcFactory(IServiceProvider provider)
        {
            var config = this.config.GetZcoinSection();

            return new RpcFactory(
                provider.GetRequiredService<Network>(),
                config.Rpc.Address,
                RPCCredentialString.Parse($"{config.Rpc.UserName}:{config.Rpc.Password}"),
                provider.GetRequiredService<ITransactionEncoder>());
        }
    }
}
