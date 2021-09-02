using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
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
using SteamScrapper.SubScanner.BackgroundServices;
using SteamScrapper.SubScanner.Commands.ScanSubBatch;
using SteamScrapper.SubScanner.Options;
using SteamScrapper.SubScanner.Services;

namespace SteamScrapper.SubScanner
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
                    var appInstanceId = Guid.NewGuid().ToString("n");

                    var config = context.Configuration;
                    var loggerConfig = new LoggerConfiguration();
                    var useElasticsearch = string.Equals("Elasticsearch", config["Serilog:Use"], StringComparison.OrdinalIgnoreCase);

                    if (useElasticsearch)
                    {
                        loggerConfig = loggerConfig
                            .WriteTo
                            .Elasticsearch(config["Serilog:Elasticsearch:Address"], "steam-scrapper-sub-scanner-{0:yyyy.MM}");
                    }
                    else
                    {
                        loggerConfig = loggerConfig
                            .WriteTo
                            .Console();
                    }

                    Log.Logger = loggerConfig
                        .Enrich.WithProperty("AppInstanceId", appInstanceId)
                        .CreateLogger();

                    builder.ClearProviders();
                    builder.AddSerilog();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<SqlServerOptions>(hostContext.Configuration.GetSection(SqlServerOptions.SectionName));
                    services.Configure<RedisOptions>(hostContext.Configuration.GetSection(RedisOptions.SectionName));
                    services.Configure<ScanSubBatchOptions>(hostContext.Configuration.GetSection(ScanSubBatchOptions.SectionName));

                    services.AddPooledDbContextFactory<SteamContext>(
                        (services, opts) => opts.UseSqlServer(services.GetRequiredService<IOptions<SqlServerOptions>>().Value.ConnectionString), SqlConnectionPoolSize);

                    services.AddSingleton<SubRepository>();
                    services.AddSingleton<ISubQueryRepository>(services => services.GetRequiredService<SubRepository>());
                    services.AddSingleton<ISubWriteRepository>(services => services.GetRequiredService<SubRepository>());

                    services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
                    services.AddSingleton<ISteamPageFactory, SteamPageFactory>();
                    services.AddSingleton<ISteamService, SteamService>();
                    services.AddSingleton<ISubScanningService, SubScanningService>();

                    services.AddSingleton<IScanSubBatchCommandHandler, ScanSubBatchCommandHandler>();

                    services.AddSingleton<IRedisConnectionWrapper, RedisConnectionWrapper>();

                    services
                        .AddHealthChecks()
                        .AddCheck<SteamContextHealthCheck>("SQL Server", HealthStatus.Unhealthy, new[] { "SQL Server", "Database" })
                        .AddCheck<RedisHealthCheck>("Redis", HealthStatus.Unhealthy, new[] { "Redis" });

                    services.AddHostedService<ScanSubsBackgroundService>();
                    services.AddHostedService<HealthCheckBackgroundService>();
                });
        }
    }
}