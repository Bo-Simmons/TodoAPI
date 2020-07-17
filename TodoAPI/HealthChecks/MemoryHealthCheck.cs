using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TodoAPI.HealthChecks
{
    public class MemoryHealthCheck : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            MemoryMetrics metrics = getWindowsMemMetrics();
            double percentUsed = 100 * metrics.Used / metrics.Total;

            HealthStatus status = HealthStatus.Healthy;
            if (percentUsed > 50)
                status = HealthStatus.Degraded;

            Dictionary<string, object> data = new Dictionary<string, object>();
            data.Add("Total", metrics.Total);
            data.Add("Used", metrics.Used);
            data.Add("Free", metrics.Free);

            HealthCheckResult result = new HealthCheckResult(status, null, null, data);
            return await Task.FromResult(result);
        }


        private struct MemoryMetrics
        {
            public double Total { get; set; }
            public double Used { get; set; }
            public double Free { get; set; }
        }

        private MemoryMetrics getWindowsMemMetrics()
        {
            string output = "";
            System.Diagnostics.ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = "wmic";
            info.Arguments = "OS get FreePhysicalMemory,TotalVisibleMemorySize /Value";
            info.RedirectStandardOutput = true;

            using (System.Diagnostics.Process process = Process.Start(info)) { output = process.StandardOutput.ReadToEnd(); }

            string[] lines = output.Trim().Split("\n");
            string[] freeMemoryParts = lines[0].Split("=", StringSplitOptions.RemoveEmptyEntries);
            string[] totalMemoryParts = lines[1].Split("=", StringSplitOptions.RemoveEmptyEntries);

            MemoryMetrics metrics = new MemoryMetrics();
            metrics.Total = Math.Round(double.Parse(totalMemoryParts[1]));
            metrics.Free = Math.Round(double.Parse(freeMemoryParts[1]));
            metrics.Used = metrics.Total - metrics.Free;

            return metrics;
        }
    }
}
