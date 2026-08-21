using DispatchPal.Api.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DispatchPal.Api.Infrastructure.Health
{
    public sealed class DatabaseHealthCheck(DispatchPalDbContext dbContext) : IHealthCheck
    {
        public async Task <HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

                return canConnect
                    ? HealthCheckResult.Healthy(
                        "PostgreSQL connection succeeded.")
                    : HealthCheckResult.Unhealthy(
                        "PostgreSQL connection failed.");
            }
            catch (Exception exception)
            {
                return HealthCheckResult.Unhealthy(
                    "PostgreSQL health check failed.",
                    exception);
            }
        }

    }
}
