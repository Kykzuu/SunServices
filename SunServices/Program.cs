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
using SunServices.Functions.PrivateChannels;
using SunServices.Functions.ServerStatsToDB;
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


                    //Kanaly klanowe
                    if (configuration.GetSection("ClansChannels:Enabled").Get<bool>())
                    {
                        services.AddHostedService<ClansChannelsCreate>();
                        services.AddHostedService<ClansChannelsFuncs>();
                        services.AddHostedService<ClansChannelsTimeData>();
                    }

                    //Kanaly prywatne
                    if (configuration.GetSection("PrivateChannels:Enabled").Get<bool>())
                    {
                        services.AddHostedService<PrivateChannelsCreate>();
                        services.AddHostedService<PrivateChannelsDelete>();
                        services.AddHostedService<PrivateChannelsNumbering>();
                        services.AddHostedService<PrivateChannelsUpdate>();
                    }

                    //Liczenie czasu administracji
                    if (configuration.GetSection("AdminsTimeSpend:Enabled").Get<bool>())
                    {
                        services.AddHostedService<AdminsTimeSpend>();
                    }

                    //Banner
                    if (configuration.GetSection("Banner:Enabled").Get<bool>())
                    {
                        services.AddHostedService<Banner>();
                    }


                    //kanal do kontaktu z botem
                    if (configuration.GetSection("ContactChannel:Enabled").Get<bool>())
                    {
                        services.AddHostedService<ContactChannel>();
                    }

                    //Kanal automatycznej pomocy
                    if (configuration.GetSection("HelpChannel:Enabled").Get<bool>())
                    {
                        services.AddHostedService<HelpChannel>();
                    }

                    //Automatyczna rejestracja uzytkownikow
                    if (configuration.GetSection("RegisterUser:Enabled").Get<bool>())
                    {
                        services.AddHostedService<RegisterUser>();
                    }

                    //Aktualizacja nazwy serwera o liczbe online
                    if (configuration.GetSection("UpdateServerName:Enabled").Get<bool>())
                    {
                        services.AddHostedService<UpdateServerName>();
                    }

                    //Statystyki do bazy danych
                    if (configuration.GetSection("ServerStatsToDB:Enabled").Get<bool>())
                    {
                        services.AddHostedService<ServerStatsToDB>();
                    }
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
