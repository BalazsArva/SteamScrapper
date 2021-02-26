using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using SteamScrapper.Domain.Services.Abstractions;

namespace SteamScrapper.Infrastructure.Services
{
    public class SteamContentRegistrationService : ISteamContentRegistrationService
    {
        private readonly SqlConnection sqlConnection;

        public SteamContentRegistrationService(SqlConnection sqlConnection)
        {
            // TODO: Swap to a data access abstraction
            this.sqlConnection = sqlConnection ?? throw new ArgumentNullException(nameof(sqlConnection));
        }

        public async Task<int> RegisterUnknownAppsAsync(IEnumerable<int> appIds)
        {
            if (appIds is null)
            {
                throw new ArgumentNullException(nameof(appIds));
            }

            if (sqlConnection.State == ConnectionState.Closed || sqlConnection.State == ConnectionState.Broken)
            {
                await sqlConnection.OpenAsync();
            }

            using var sqlCommand = sqlConnection.CreateCommand();

            var commandTexts = appIds
                .Distinct()
                .Select(appId => IncludeInsertUnknownApp(sqlCommand, appId))
                .ToList();

            if (commandTexts.Count == 0)
            {
                return 0;
            }

            var completeCommandText = string.Join('\n', commandTexts);

            sqlCommand.CommandText = completeCommandText;

            return Math.Max(0, await sqlCommand.ExecuteNonQueryAsync());
        }

        public async Task<int> RegisterUnknownSubsAsync(IEnumerable<int> subIds)
        {
            if (subIds is null)
            {
                throw new ArgumentNullException(nameof(subIds));
            }

            if (sqlConnection.State == ConnectionState.Closed || sqlConnection.State == ConnectionState.Broken)
            {
                await sqlConnection.OpenAsync();
            }

            using var sqlCommand = sqlConnection.CreateCommand();

            var commandTexts = subIds
                .Distinct()
                .Select(subId => IncludeInsertUnknownSub(sqlCommand, subId))
                .ToList();

            if (commandTexts.Count == 0)
            {
                return 0;
            }

            var completeCommandText = string.Join('\n', commandTexts);

            sqlCommand.CommandText = completeCommandText;

            return Math.Max(0, await sqlCommand.ExecuteNonQueryAsync());
        }

        public async Task<int> RegisterUnknownBundlesAsync(IEnumerable<int> bundleIds)
        {
            if (bundleIds is null)
            {
                throw new ArgumentNullException(nameof(bundleIds));
            }

            if (sqlConnection.State == ConnectionState.Closed || sqlConnection.State == ConnectionState.Broken)
            {
                await sqlConnection.OpenAsync();
            }

            using var sqlCommand = sqlConnection.CreateCommand();

            var commandTexts = bundleIds
                .Distinct()
                .Select(bundleId => IncludeInsertUnknownBundle(sqlCommand, bundleId))
                .ToList();

            if (commandTexts.Count == 0)
            {
                return 0;
            }

            var completeCommandText = string.Join('\n', commandTexts);

            sqlCommand.CommandText = completeCommandText;

            return Math.Max(0, await sqlCommand.ExecuteNonQueryAsync());
        }

        private static string IncludeInsertUnknownApp(SqlCommand command, int appId)
        {
            var parameterName = $"app_{appId}";

            command.Parameters.AddWithValue(parameterName, appId);

            return $"IF NOT EXISTS (SELECT 1 FROM [dbo].[Apps] AS [A] WHERE [A].[Id] = @{parameterName}) INSERT INTO [dbo].[Apps] ([Id], [Title], [UtcDateTimeRecorded]) VALUES (@{parameterName}, N'Unknown App', SYSUTCDATETIME())";
        }

        private static string IncludeInsertUnknownSub(SqlCommand command, int subId)
        {
            var parameterName = $"sub_{subId}";

            command.Parameters.AddWithValue(parameterName, subId);

            return $"IF NOT EXISTS (SELECT 1 FROM [dbo].[Subs] AS [S] WHERE [S].[Id] = @{parameterName}) INSERT INTO [dbo].[Subs] ([Id], [Title], [UtcDateTimeRecorded]) VALUES (@{parameterName}, N'Unknown sub', SYSUTCDATETIME())";
        }

        private static string IncludeInsertUnknownBundle(SqlCommand command, int bundleId)
        {
            var parameterName = $"bundle_{bundleId}";

            command.Parameters.AddWithValue(parameterName, bundleId);

            return $"IF NOT EXISTS (SELECT 1 FROM [dbo].[Bundles] AS [B] WHERE [B].[Id] = @{parameterName}) INSERT INTO [dbo].[Bundles] ([Id], [Title], [UtcDateTimeRecorded]) VALUES (@{parameterName}, N'Unknown bundle', SYSUTCDATETIME())";
        }
    }
}