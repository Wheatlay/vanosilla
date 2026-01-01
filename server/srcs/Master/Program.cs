using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PhoenixLib.Logging;
using PhoenixLib.ServiceBus.MQTT;
using WingsEmu.ClusterCommunicator;

namespace Master
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            PrintHeader();
            using IHost hostBuilder = CreateHostBuilder(args).Build();
            {
                using var stopService = new DockerGracefulStopService();
                Log.Info("Starting...");

                IMessagingService messagingService = hostBuilder.Services.GetRequiredService<IMessagingService>();
                await messagingService.StartAsync();

                await hostBuilder.StartAsync();
                IServiceProvider services = hostBuilder.Services;

                await hostBuilder.WaitForShutdownAsync(stopService.CancellationToken);
                await messagingService.DisposeAsync();
            }
        }

        private static void PrintHeader()
        {
            Console.Title = "WingsEmu - Master";
            const string text = @"
██╗    ██╗██╗███╗   ██╗ ██████╗ ███████╗███████╗███╗   ███╗██╗   ██╗                                       
██║    ██║██║████╗  ██║██╔════╝ ██╔════╝██╔════╝████╗ ████║██║   ██║                                       
██║ █╗ ██║██║██╔██╗ ██║██║  ███╗███████╗█████╗  ██╔████╔██║██║   ██║                                       
██║███╗██║██║██║╚██╗██║██║   ██║╚════██║██╔══╝  ██║╚██╔╝██║██║   ██║                                       
╚███╔███╔╝██║██║ ╚████║╚██████╔╝███████║███████╗██║ ╚═╝ ██║╚██████╔╝                                       
 ╚══╝╚══╝ ╚═╝╚═╝  ╚═══╝ ╚═════╝ ╚══════╝╚══════╝╚═╝     ╚═╝ ╚═════╝                                        
                                                                                                           
███╗   ███╗ █████╗ ███████╗████████╗███████╗██████╗       ███████╗███████╗██████╗ ██╗   ██╗███████╗██████╗ 
████╗ ████║██╔══██╗██╔════╝╚══██╔══╝██╔════╝██╔══██╗      ██╔════╝██╔════╝██╔══██╗██║   ██║██╔════╝██╔══██╗
██╔████╔██║███████║███████╗   ██║   █████╗  ██████╔╝█████╗███████╗█████╗  ██████╔╝██║   ██║█████╗  ██████╔╝
██║╚██╔╝██║██╔══██║╚════██║   ██║   ██╔══╝  ██╔══██╗╚════╝╚════██║██╔══╝  ██╔══██╗╚██╗ ██╔╝██╔══╝  ██╔══██╗
██║ ╚═╝ ██║██║  ██║███████║   ██║   ███████╗██║  ██║      ███████║███████╗██║  ██║ ╚████╔╝ ███████╗██║  ██║
╚═╝     ╚═╝╚═╝  ╚═╝╚══════╝   ╚═╝   ╚══════╝╚═╝  ╚═╝      ╚══════╝╚══════╝╚═╝  ╚═╝  ╚═══╝  ╚══════╝╚═╝  ╚═╝
";
            string separator = new('=', Console.WindowWidth);
            string logo = text.Split('\n').Select(s => string.Format("{0," + (Console.WindowWidth / 2 + s.Length / 2) + "}\n", s))
                .Aggregate("", (current, i) => current + i);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(separator + logo + $"Version: {Assembly.GetExecutingAssembly().GetName().Version}\n" + separator);
            Console.ForegroundColor = ConsoleColor.White;
        }


        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(s =>
                    {
                        s.ListenAnyIP(Convert.ToInt32(Environment.GetEnvironmentVariable("MASTER_PORT") ?? "20500"), options => options.Protocols = HttpProtocols.Http2);
                    });
                    webBuilder.UseStartup<Startup>();
                });
    }
}