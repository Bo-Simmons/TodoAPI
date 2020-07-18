using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.Http;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TodoAPI.Controllers
{
    [Route("health-controller")]
    [ApiController]
    [Produces("application/json")]
    public class HealthController : ControllerBase
    {
        // GET: api/<HealthController>
        [HttpGet]
        public Health Get()
        {
            HealthStatus status = HealthStatus.Healthy;
            Health h = new Health("OverallHealth", "Healthy", null, null);
            h.SubCategories = new List<Health>();
            h.SubCategories.Add(MemoryCheck());

            // this only applies in our current, single-metric version
            if (h.SubCategories[0].Status == "Degraded")
                h.Status = "Degraded";

            return h;
        }

        private enum HealthStatus
        {
            Healthy,
            Degraded,
            Unhealthy
        }

        public struct Health
        {
            public string Name { get; set; }
            public string Status { get; set; }
            public List<Health> SubCategories { get; set; }
            public Dictionary<string, string> Details { get; set; }
            public Health(string n, string s, List<Health> sub, Dictionary<string, string> d)
            {
                Name = n;
                Status = s;
                SubCategories = sub;
                Details = d;
            }
        }

        private Health MemoryCheck()
        {
            Health h = new Health("Memory", "Healthy", null, null);
            MemoryMetrics mm = getWindowsMemMetrics();

            double percentUsed = 100 * mm.Used / mm.Total;
            if (percentUsed > 80)
                h.Status = "Degraded";

            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("Total", mm.Total.ToString());
            data.Add("Used", mm.Used.ToString());
            data.Add("Free", mm.Free.ToString());

            h.Details = data;

            return h;
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
