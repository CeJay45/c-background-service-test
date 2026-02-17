using System;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;


namespace WorkerService1
{

    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Run(() =>
            {
                var watcher = new ManagementEventWatcher();
                var query = new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2");
                watcher.EventArrived += (sender, e) =>
                {
                    var driveName = e.NewEvent["DriveName"]?.ToString();
                    if (!string.IsNullOrEmpty(driveName))
                    {
                        Console.WriteLine($"New drive detected: {driveName}");
                        try
                        {
                            //use Win Defender cli
                            var process = new System.Diagnostics.Process
                            {
                                StartInfo = new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = "powershell",
                                    Arguments = $"-Command \"Start-MpScan -ScanPath '{driveName}'\"",
                                    RedirectStandardOutput = true,
                                    UseShellExecute = false,
                                    CreateNoWindow = true
                                }
                            };
                            process.Start();
                            process.WaitForExit();

                            Console.WriteLine($"Scan completed for drive: {driveName}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error scanning drive {driveName}: {ex.Message}");
                        }
                    }
                };
                watcher.Query = query;
                watcher.Start();

                stoppingToken.Register(() => watcher.Stop());
            }, stoppingToken);
        }

        private void RunDefenderScan(string driveName)
        {

        }
    }
}
