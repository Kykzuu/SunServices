using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SunServices.Functions;
using SunServices.Functions.ClansChannels;
using SunServices.Helpers;
using TS3QueryLib.Net.Core;
using TS3QueryLib.Net.Core.Server.Commands;
using TS3QueryLib.Net.Core.Server.Entitities;
using TS3QueryLib.Net.Core.Server.Notification;

namespace SunServices
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IConfiguration Configuration = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
               .AddEnvironmentVariables()
               .AddCommandLine(args)
               .Build();
            try
            {
                NotificationHub notifications = new NotificationHub();
                notifications.ClientMessage.ReceivedFromClient += Commands.ClientMessage_ReceivedFromClient;
                IQueryClient client = new QueryClient(notificationHub: notifications, keepAliveInterval: TimeSpan.FromSeconds(30), host: Configuration.GetSection("ServerQuery")["Ip"], port: ushort.Parse(Configuration.GetSection("ServerQuery")["QueryPort"]));
                Connect(client);
                
                _ = !new LoginCommand(Configuration.GetSection("ServerQuery")["Username"], Configuration.GetSection("ServerQuery")["Password"]).Execute(client).IsErroneous;
                _ = !new UseCommand(ushort.Parse(Configuration.GetSection("ServerQuery")["ServerPort"])).Execute(client).IsErroneous;
                _ = new ClientUpdateCommand(new TS3QueryLib.Net.Core.Server.Entitities.ClientModification { Nickname = "SunService" }).Execute(client);
                _ = new ServerNotifyRegisterCommand(ServerNotifyRegisterEvent.TextPrivate).Execute(client);
                CreateHostBuilder(args, client, Configuration).Build().Run();
            }
            catch (Exception err)
            {
                Console.WriteLine("Error: " + err.Message);
                using (FileStream fs = new FileStream("Logs/crash_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm") + ".txt", FileMode.CreateNew))
                {
                    using (BinaryWriter w = new BinaryWriter(fs, System.Text.Encoding.GetEncoding("ISO-8859-1")))
                    {
                        w.Write(err.Message);
                        w.Write(err.ToString());
                    }
                }
                Environment.Exit(-1);
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args, IQueryClient client, IConfiguration configuration) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton(sp => client);
                    services.AddHostedService<UpdateServerName>();
                    services.AddHostedService<RegisterUser>();
                    services.AddHostedService<HelpChannel>();
                    //Kanaly prywatne
                    services.AddHostedService<PrivateChannelsCreate>();
                    services.AddHostedService<PrivateChannelsUpdate>();
                    services.AddHostedService<PrivateChannelsNumbering>();
                    services.AddHostedService<PrivateChannelsDelete>();
                    //
                    //kanaly klanowe
                    services.AddHostedService<ClansChannelsCreate>();
                    services.AddHostedService<ClansChannelsFuncs>();
                    services.AddHostedService<ClansChannelsTimeData>();
                    services.AddHostedService<Banner>();
                    services.AddHostedService<AdminsTimeSpend>();
                    services.AddHostedService<ApiData>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.AddFile("Logs/{Date}.log");
                    logging.AddConsole();
                });

        private static void Connect(IQueryClient client)
        {
            QueryClient.ConnectResponse connectResponse = client.Connect();
        }
    }
}
