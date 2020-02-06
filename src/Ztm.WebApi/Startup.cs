using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
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
using Ztm.WebApi.Callbacks;
using Ztm.WebApi.Controllers;
using Ztm.WebApi.Converters;
using Ztm.WebApi.Watchers.TokenReceiving;
using Ztm.WebApi.Watchers.TransactionConfirmation;
using Ztm.Zcoin.NBitcoin;
using Ztm.Zcoin.NBitcoin.Exodus;
using Ztm.Zcoin.Rpc;
using Ztm.Zcoin.Synchronization;
using PropertyAmountConverter=Ztm.WebApi.Converters.PropertyAmountConverter;

namespace Ztm.WebApi
{
    public sealed class Startup
    {
        readonly BitcoinAddressConverter bitcoinAddressConverter;
        readonly MoneyConverter moneyConverter;
        readonly Network network;
        readonly PropertyAmountConverter propertyAmountConverter;
        readonly UInt256Converter uint256Converter;

        public Startup(IConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            this.network = ZcoinNetworks.Instance.GetNetwork(config.GetZcoinSection().Network.Type);
            this.bitcoinAddressConverter = new BitcoinAddressConverter(this.network);
            this.moneyConverter = new MoneyConverter();
            this.propertyAmountConverter = new PropertyAmountConverter(config.GetZcoinSection().Property.Type);
            this.uint256Converter = new UInt256Converter();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (env == null)
            {
                throw new ArgumentNullException(nameof(env));
            }

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

        public void ConfigureServices(IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // NBitcoin Services.
            services.AddNBitcoin();

            // ASP.NET Related Services.
            services
                .AddMvc(ConfigureMvc)
                .AddJsonOptions(ConfigureJson)
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddHttpClient();

            // Application Fundamentals Services.
            services.AddBackgroundServiceExceptionHandler();
            services.AddSingleton<JsonSerializer>(CreateJsonSerializer);
            services.AddSingleton<ITimerScheduler, ThreadPoolScheduler>();
            services.AddSingleton<Network>(this.network);
            services.AddSingleton<ZcoinConfiguration>(p => p.GetRequiredService<IConfiguration>().GetZcoinSection());
            services.AddSingleton<PropertyId>(p => p.GetRequiredService<ZcoinConfiguration>().Property.Id);

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

        static JsonSerializer CreateJsonSerializer(IServiceProvider provider)
        {
            var options = provider.GetRequiredService<IOptions<MvcJsonOptions>>().Value;

            return JsonSerializer.Create(options.SerializerSettings);
        }

        static IRpcFactory CreateRpcFactory(IServiceProvider provider)
        {
            var config = provider.GetRequiredService<IConfiguration>().GetZcoinSection();

            return new RpcFactory(
                provider.GetRequiredService<Network>(),
                config.Rpc.Address,
                RPCCredentialString.Parse($"{config.Rpc.UserName}:{config.Rpc.Password}"),
                provider.GetRequiredService<ITransactionEncoder>());
        }

        void ConfigureMvc(MvcOptions options)
        {
            // Custom Model Binders.
            options.ModelBinderProviders.Insert(
                0,
                new ModelBinderProvider<BitcoinAddress>(this.bitcoinAddressConverter));

            options.ModelBinderProviders.Insert(
                0,
                new ModelBinderProvider<Money>(this.moneyConverter));

            options.ModelBinderProviders.Insert(
                0,
                new ModelBinderProvider<PropertyAmount>(this.propertyAmountConverter));

            options.ModelBinderProviders.Insert(
                0,
                new ModelBinderProvider<uint256>(this.uint256Converter));

            // FIXME: remove this when upgrade to .NET Core version >= 2.2
            options.ModelMetadataDetailsProviders.Add(
                new SuppressChildValidationMetadataProvider(typeof(BitcoinAddress)));
        }

        void ConfigureJson(MvcJsonOptions options)
        {
            options.SerializerSettings.ContractResolver = new DefaultContractResolver()
            {
                NamingStrategy = new SnakeCaseNamingStrategy(),
            };

            options.SerializerSettings.Converters.Add(this.bitcoinAddressConverter);
            options.SerializerSettings.Converters.Add(this.moneyConverter);
            options.SerializerSettings.Converters.Add(this.propertyAmountConverter);
            options.SerializerSettings.Converters.Add(this.uint256Converter);
        }
    }
}
