using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SteamScrapper.Infrastructure.Database
{
    public class SteamContextFactory : IDesignTimeDbContextFactory<SteamContext>
    {
        public SteamContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            var sqlConnectionString = configuration["ConnectionStrings:Sql"];

            if (string.IsNullOrWhiteSpace(sqlConnectionString))
            {
                throw new InvalidOperationException("No SQL connection string could be found.");
            }

            var options = new DbContextOptionsBuilder<SteamContext>()
                .UseSqlServer(sqlConnectionString)
                .Options;

            return new SteamContext(options);
        }
    }
}