using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using SteamScrapper.BundleScanner.BackgroundServices;
using SteamScrapper.BundleScanner.Commands.ScanBundleBatch;
using SteamScrapper.BundleScanner.Options;
using SteamScrapper.BundleScanner.Services;
using SteamScrapper.Common.Hosting;
using SteamScrapper.Common.Providers;
using SteamScrapper.Domain.Factories;
using SteamScrapper.Domain.Repositories;
using SteamScrapper.Domain.Services.Abstractions;
using SteamScrapper.Infrastructure.Database.Context;
using SteamScrapper.Infrastructure.Database.Repositories;
using SteamScrapper.Infrastructure.Options;
using SteamScrapper.Infrastructure.Redis;
using SteamScrapper.Infrastructure.Services;

namespace SteamScrapper.BundleScanner
{
    public static class Program
    {
        private const int SqlConnectionPoolSize = 32;

        public static async Task Main(string[] args)
        {
            try
            {
                var hostBuilder = CreateHostBuilder(args);

                await hostBuilder.Build().RunAsync();
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host
                .CreateDefaultBuilder(args)
                .ConfigureLogging((context, builder) =>
                {
                    var config = context.Configuration;
                    var loggerConfig = new LoggerConfiguration();
                    var useElasticsearch = string.Equals("Elasticsearch", config["Serilog:Use"], StringComparison.OrdinalIgnoreCase);

                    if (useElasticsearch)
                    {
                        loggerConfig = loggerConfig
                            .WriteTo
                            .Elasticsearch(config["Serilog:Elasticsearch:Address"], "steam-scrapper-bundle-scanner-{0:yyyy.MM}");
                    }
                    else
                    {
                        loggerConfig = loggerConfig
                            .WriteTo
                            .Console();
                    }

                    Log.Logger = loggerConfig.CreateLogger();

                    builder.ClearProviders();
                    builder.AddSerilog();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<SqlServerOptions>(hostContext.Configuration.GetSection(SqlServerOptions.SectionName));
                    services.Configure<RedisOptions>(hostContext.Configuration.GetSection(RedisOptions.SectionName));
                    services.Configure<ScanBundleBatchOptions>(hostContext.Configuration.GetSection(ScanBundleBatchOptions.SectionName));

                    services.AddPooledDbContextFactory<SteamContext>(
                        (services, opts) => opts.UseSqlServer(services.GetRequiredService<IOptions<SqlServerOptions>>().Value.ConnectionString), SqlConnectionPoolSize);

                    services.AddSingleton<BundleRepository>();
                    services.AddSingleton<IBundleQueryRepository>(services => services.GetRequiredService<BundleRepository>());
                    services.AddSingleton<IBundleWriteRepository>(services => services.GetRequiredService<BundleRepository>());

                    services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
                    services.AddSingleton<ISteamPageFactory, SteamPageFactory>();
                    services.AddSingleton<ISteamService, SteamService>();
                    services.AddSingleton<IBundleScanningService, BundleScanningService>();

                    services.AddSingleton<IScanBundleBatchCommandHandler, ScanBundleBatchCommandHandler>();

                    services.AddSingleton<IRedisConnectionWrapper, RedisConnectionWrapper>();

                    services
                        .AddHealthChecks()
                        .AddCheck<SteamContextHealthCheck>("SQL Server", HealthStatus.Unhealthy, new[] { "SQL Server", "Database" })
                        .AddCheck<RedisHealthCheck>("Redis", HealthStatus.Unhealthy, new[] { "Redis" });

                    services.AddHostedService<ScanBundlesBackgroundService>();
                    services.AddHostedService<HealthCheckBackgroundService>();
                });
        }
    }
}