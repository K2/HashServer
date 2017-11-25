using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Globalization;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace HashServer
{
    public class Program
    {
        public static AppOptions Settings;
        public static ILogger log { get; set; }

        public IConfigurationRoot Configuration { get; set; }


        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            // Register the IConfiguration instance which MyOptions binds against.
            services.Configure<AppOptions>(Configuration);
        }

        public static Task Main(string[] args)
        {
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Console.WriteLine("Unobserved exception: {0}", e.Exception);
            };

            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json",  optional: false, reloadOnChange: true)
                .Build();
            Settings = configuration.GetSection("App").Get<AppOptions>();

            Settings.ToString();

            var basePort = Settings.Host.BasePort;
            
            var host = new WebHostBuilder()
            .ConfigureLogging((_, factory) =>
            {
                factory.ClearProviders();
                factory.SetMinimumLevel(Enum.Parse<LogLevel>(Settings.Host.LogLevel));
                factory.AddConsole();
            })
            .UseKestrel(options =>
            {
                // Run callbacks on the transport thread
                options.ApplicationSchedulingMode = SchedulingMode.ThreadPool;
                options.Listen(IPAddress.Any, basePort, listenOptions =>
                {
                    // Uncomment the following to enable Nagle's algorithm for this endpoint.
                    listenOptions.NoDelay = false;
                    listenOptions.KestrelServerOptions.Limits.MaxConcurrentConnections = Settings.Host.MaxConcurrentConnections;
                    listenOptions.KestrelServerOptions.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(1);
                    listenOptions.KestrelServerOptions.AllowSynchronousIO = true;
                    listenOptions.UseConnectionLogging();
                });

                options.Listen(IPAddress.Loopback, basePort + 1, listenOptions =>
                {
                    listenOptions.UseHttps(Settings.Host.CertificateFile, Settings.Host.CertificatePassword);
                    listenOptions.UseConnectionLogging();
                });
            })
            .UseLibuv(options =>
            {
#if DEBUG
                options.ThreadCount = 1;
#else
                options.ThreadCount = Settings.Host.ThreadCount;
#endif
            })
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseUrls(Settings.Internal.gRoot, Settings.InternalSSL.gRoot)
            .UseStartup<Startup>()
            .Build();
            return host.RunAsync();
        }
    }
}
