using System;

namespace SteamScrapper.Common.HealthCheck
{
    public record HealthCheckResult(bool IsHealthy, string ReporterName, string Reason = null, Exception Exception = null);
}