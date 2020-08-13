using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SunServices.Functions.PrivateChannels;
using SunServices.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TS3QueryLib.Net.Core;
using TS3QueryLib.Net.Core.Common.Responses;
using TS3QueryLib.Net.Core.Server.Commands;
using TS3QueryLib.Net.Core.Server.Entitities;
using TS3QueryLib.Net.Core.Server.Responses;

namespace SunServices.Functions
{
    public class Commands
    {
        public static void ClientMessage_ReceivedFromClient(object sender, TS3QueryLib.Net.Core.Server.Notification.EventArgs.MessageReceivedEventArgs e)
        {
            uint botid = new WhoAmICommand().Execute((IQueryClient)sender).ClientId;
            if (botid != e.InvokerClientId)
            {
                IConfiguration configuration = new ConfigurationBuilder()
                   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                   .AddEnvironmentVariables()
                   .Build();


                List<uint> admingroups = configuration.GetSection("Commands:AdminGroups").Get<List<uint>>();
                List<uint> ownergroups = configuration.GetSection("Commands:OwnerGroups").Get<List<uint>>();
                try
                {
                    ClientInfoCommandResponse client = new ClientInfoCommand(e.InvokerClientId).Execute((IQueryClient)sender);


                    //Komendy graczowe
                    if (e.Message.ToLower().Split(' ')[0].Equals("pomoc") ||
                       e.Message.ToLower().Split(' ')[0].Equals("grouplist") ||
                       e.Message.ToLower().Split(' ')[0].Equals("groupadd") ||
                       e.Message.ToLower().Split(' ')[0].Equals("groupdel"))
                    {
                        typeof(Commands).GetMethod(e.Message.ToLower().Split(' ')[0]).Invoke(null, new[] { e.Message.Split(' '), sender, e });
                        return;
                    }
                    //

                    //Komendy administracyjne
                    if (e.Message.ToLower().Split(' ')[0].Equals("extendchannelexpiration") ||
                       e.Message.ToLower().Split(' ')[0].Equals("myadmintime"))
                    {
                        if (client.ServerGroups.Any(x => admingroups.Any(z => z == x)) || client.ServerGroups.Any(x => ownergroups.Any(z => z == x)))
                        {
                            typeof(Commands).GetMethod(e.Message.ToLower().Split(' ')[0]).Invoke(null, new[] { e.Message.Split(' '), sender, e });
                            return;
                        }
                        else
                        {
                            new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, "Nie masz do tego uprawnień").ExecuteAsync((IQueryClient)sender);
                        }
                    }
                    //

                    //Komendy ownerowskie
                    if (client.ServerGroups.Any(x => ownergroups.Any(z => z == x)))
                    {
                        typeof(Commands).GetMethod(e.Message.ToLower().Split(' ')[0]).Invoke(null, new[] { e.Message.Split(' '), sender, e });
                        return;
                    }
                    else
                    {
                        new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, "Nie ma takiej komendy lub nie masz do tego uprawnień").ExecuteAsync((IQueryClient)sender);
                    }
                    //

                }
                catch (Exception)
                {
                    new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, "Nieznana komenda. Wpisz [b]pomoc[/b] po więcej informacji").ExecuteAsync((IQueryClient)sender);
                }
            }
        }

        #region pomoc
        public static void pomoc(string[] args, IQueryClient sender, TS3QueryLib.Net.Core.Server.Notification.EventArgs.MessageReceivedEventArgs e)
        {
            string tekst = "[b]Dostępne komendy:[/b] \n" +
                "grouplist [b]--[/b] wyświetla wszystkie możliwe do nadania grupy wraz z ID \n" +
                "groupadd [i]{group_id}[/i] [b]--[/b] dodaje Cie do grupy serwerowej \n" +
                "groupdel [i]{group_id}[/i] [b]--[/b] usuwa Cie z grupy serwerowej";

            if (args.Length >= 2)
            {
                if (args[1].Equals("owner"))
                {
                    tekst = "[b]Dostępne komendy:[/b] \n" +
                    "pwall [i]{message}[/i] [b]--[/b] wysyła wiadomość do podanej wszystkich użytkowników \n" +
                    "AllAdminsTime [b]--[/b] pokazuje spedzony przez administratorów czas \n" +
                    "ProtectChannel [i]{channelid}[/i] [b]--[/b] chroni dany kanał przed usunięciem \n" +
                    "UnprotectChannel [i]{channelid}[/i] [b]--[/b] usuwa ochrone z kanału \n" +
                    "ShowProtectedChannels [b]--[/b] pokazuje wszystkie kanały obecnie podlegające ochronie \n" +
                    "AdminTime [i]{nickname} {dd/mm/rrrr}[/i] [b]--[/b] pokazuje spędzony przez administratora czas w danym dniu";
                }

                if (args[1].Equals("admin"))
                {
                    tekst = "[b]Dostępne komendy:[/b] \n" +
                    "ExtendChannelExpiration [i]{channelid} {days}[/i] [b]--[/b] przedłuża ważność kanału o podaną liczbe dni \n" +
                    "MyAdminTime [b]--[/b] pokazuje Twój czas online";
                }
            }

            new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, tekst).ExecuteAsync(sender);

        }
        #endregion

        #region grouplist
        public static void grouplist(string[] args, IQueryClient sender, TS3QueryLib.Net.Core.Server.Notification.EventArgs.MessageReceivedEventArgs e)
        {
            IConfiguration configuration = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
               .AddEnvironmentVariables()
               .Build();


            string[][][] groups = configuration.GetSection("Commands:AvailableGroups").Get<string[][][]>();
            EntityListCommandResponse<ServerGroup> serverGroups = new ServerGroupListCommand().Execute(sender);
            foreach(string[][] grupa in groups)
            {
                string text = $"[b]{grupa[0][1]}[/b] (Możesz mieć max: {grupa[0][0]})";

                foreach(string ranga in grupa[1])
                {
                    text = String.Concat(text + $"\n{serverGroups.Values.Where(x => x.Id == uint.Parse(ranga)).First().Name} - {ranga}");
                }
                new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, text).ExecuteAsync(sender);
            }
        }



        #endregion

        #region groupadd
        public static void groupadd(string[] args, IQueryClient sender, TS3QueryLib.Net.Core.Server.Notification.EventArgs.MessageReceivedEventArgs e)
        {
            IConfiguration configuration = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
               .AddEnvironmentVariables()
               .Build();

            if (args.Length == 2)
            {
                string[][][] groups = configuration.GetSection("Commands:AvailableGroups").Get<string[][][]>();
                ClientInfoCommandResponse client = new ClientInfoCommand(e.InvokerClientId).Execute(sender);
                if(groups.Any(x => x[1].Any(z => z == args[1]))) //czy grupa jest w configu
                { 
                    foreach(string [][] group in groups)
                    {
                        int i = 0;
                        foreach (uint clientGroup in client.ServerGroups)
                        {
                            i = i + group[1].Count(x => x == clientGroup.ToString());
                        }
                        if(group[1].Any(x => x == args[1]))
                        {
                            if(i < int.Parse(group[0][0])){
                                new ServerGroupAddClientCommand(uint.Parse(args[1]), client.DatabaseId).ExecuteAsync(sender);
                                new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, "Sukces").ExecuteAsync(sender);
                                return;
                            }
                            else
                            {
                                new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, $"Możesz mieć maksymalnie {group[0][0]} {group[0][1]}").ExecuteAsync(sender);
                                return;
                            }
                        }
                    }
                    
                }
                else
                {
                    new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, "Ta grupa nie istnieje. Wpisz [b]grouplist[/b] by zobaczyć listę dostępnych grup").ExecuteAsync(sender);
                }
            }
            else
            {
                new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, "Poprawne użycie: groupadd [i]{group_id}[/i]").ExecuteAsync(sender);
            }
        }



        #endregion

        #region groupdel
        public static void groupdel(string[] args, IQueryClient sender, TS3QueryLib.Net.Core.Server.Notification.EventArgs.MessageReceivedEventArgs e)
        {
            IConfiguration configuration = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
               .AddEnvironmentVariables()
               .Build();

            if (args.Length == 2)
            {
                string[][][] groups = configuration.GetSection("Commands:AvailableGroups").Get<string[][][]>();

                if (groups.Any(x => x[1].Any(z => z == args[1])))
                {
                    new ServerGroupDelClientCommand(uint.Parse(args[1]), new ClientGetDbIdFromUIdCommand(e.InvokerUniqueId).Execute(sender).Values.First()).Execute(sender);
                    new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, "Sukces").ExecuteAsync(sender);
                }
                else
                {
                    new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, "Ta grupa nie istnieje. Wpisz [b]grouplist[/b] by zobaczyć listę dostępnych grup").ExecuteAsync(sender);
                }
            }
            else
            {
                new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, "Poprawne użycie: groupadd [i]{group_id}[/i]").ExecuteAsync(sender);
            }
        }



        #endregion

        #region protectchannel
        public static void protectchannel(string[] args, IQueryClient sender, TS3QueryLib.Net.Core.Server.Notification.EventArgs.MessageReceivedEventArgs e)
        {
            if (args.Length == 2)
            {
                try
                {
                    IEnumerable<ChannelListEntry> AllChannels = new ChannelListCommand(includeAll: true, includeTopics: true).Execute(sender).Values;
                    ChannelListEntry channel = AllChannels.Where(x => x.ChannelId == uint.Parse(args[1])).First();
                    string UniqueId = new GetDataFromTopic().UniqueId(channel.Topic);
                    string protectedchanneltopic = Base64Helper.Encode(UniqueId + "|+" + 0 + "|+" + new GetDataFromTopic().CreatedDate(channel.Topic).ToUnixTimeSeconds());
                    string desc = new ChannelInfoCommand(channel.ChannelId).Execute(sender).Description;
                    string[] description = desc.Split("Ważny do:");
                    string channellogourl = desc.Split("[img]").Last();
                    new ChannelEditCommand(channel.ChannelId,
                        new ChannelModification
                        {
                            Topic = protectedchanneltopic,
                            Description = description.First() + String.Format("Ważny do: [b]{0}[/b][/size] \n [hr] \n [center][img]{1}", "bezterminowo", channellogourl)
                        }).ExecuteAsync(sender);
                    new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, "Sukces").ExecuteAsync(sender);

                }
                catch (Exception)
                {
                    new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, "Poprawne użycie: ProtectChannel [i]{channelid}[/i]").ExecuteAsync(sender);
                }
            }
            else
            {
                new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, "Poprawne użycie: ProtectChannel [i]{channelid}[/i]").ExecuteAsync(sender);
            }

        }
        #endregion

        #region unprotectchannel
        public static void unprotectchannel(string[] args, IQueryClient sender, TS3QueryLib.Net.Core.Server.Notification.EventArgs.MessageReceivedEventArgs e)
        {
            if (args.Length == 2)
            {
                try
                {
                    IEnumerable<ChannelListEntry> AllChannels = new ChannelListCommand(includeAll: true, includeTopics: true).Execute(sender).Values;
                    ChannelListEntry channel = AllChannels.Where(x => x.ChannelId == uint.Parse(args[1])).First();
                    string UniqueId = new GetDataFromTopic().UniqueId(channel.Topic);
                    string protectedchanneltopic = Base64Helper.Encode(UniqueId + "|+" + DateTimeOffset.Now.AddDays(3).ToUnixTimeSeconds() + "|+" + new GetDataFromTopic().CreatedDate(channel.Topic).ToUnixTimeSeconds());
                    new ChannelEditCommand(channel.ChannelId,
                        new ChannelModification
                        {
                            Topic = protectedchanneltopic
                        }).ExecuteAsync(sender);
                    new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, "Sukces").ExecuteAsync(sender);

                }
                catch (Exception)
                {
                    new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, "Poprawne użycie: ProtectChannel [i]{channelid}[/i]").ExecuteAsync(sender);
                }
            }
            else
            {
                new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, "Poprawne użycie: ProtectChannel [i]{channelid}[/i]").ExecuteAsync(sender);
            }

        }
        #endregion

        #region showprotectedchannels
        public static void showprotectedchannels(string[] args, IQueryClient sender, TS3QueryLib.Net.Core.Server.Notification.EventArgs.MessageReceivedEventArgs e)
        {
            try
            {
                IEnumerable<ChannelListEntry> AllChannels = new ChannelListCommand(includeAll: true, includeTopics: true).Execute(sender).Values;
                IEnumerable<ChannelListEntry> Channels = AllChannels.Where(x => new GetDataFromTopic().Time(x.Topic).ToUnixTimeSeconds() == 0);
                foreach (ChannelListEntry channelListEntry in Channels)
                {
                    new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, $"{channelListEntry.Name} ( {channelListEntry.ChannelId} )").Execute(sender);
                }
                new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, "Sukces").ExecuteAsync(sender);

            }
            catch (Exception)
            {
                new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, "Poprawne użycie: ProtectChannel [i]{channelid}[/i]").ExecuteAsync(sender);
            }

        }
        #endregion

        #region extendchannelexpiration
        public static void extendchannelexpiration(string[] args, IQueryClient sender, TS3QueryLib.Net.Core.Server.Notification.EventArgs.MessageReceivedEventArgs e)
        {
            if (args.Length == 3)
            {
                try
                {
                    IEnumerable<ChannelListEntry> AllChannels = new ChannelListCommand(includeAll: true, includeTopics: true).Execute(sender).Values;
                    ChannelListEntry channel = AllChannels.Where(x => x.ChannelId == uint.Parse(args[1])).First();
                    string UniqueId = new GetDataFromTopic().UniqueId(channel.Topic);
                    DateTimeOffset time = new GetDataFromTopic().Time(channel.Topic);
                    DateTimeOffset newtime = time.AddDays(int.Parse(args[2]));
                    string topic = Base64Helper.Encode(UniqueId + "|+" + newtime.ToUnixTimeSeconds() + "|+" + new GetDataFromTopic().CreatedDate(channel.Topic).ToUnixTimeSeconds());
                    string desc = new ChannelInfoCommand(channel.ChannelId).Execute(sender).Description;
                    string[] description = desc.Split("Ważny do:");
                    string channellogourl = desc.Split("[img]").Last();
                    new ChannelEditCommand(channel.ChannelId,
                        new ChannelModification
                        {
                            Topic = topic,
                            Description = description.First() + String.Format("Ważny do: [b]{0}[/b][/size] \n [hr] \n [center][img]{1}", newtime.ToString("dd.MM.yyyy HH:mm"), channellogourl)
                        }).ExecuteAsync(sender);
                    new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, "Sukces").ExecuteAsync(sender);

                }
                catch (Exception)
                {
                    new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, "Poprawne użycie: ExtendChannelExpiration [i]{channelid} {days}[/i]").ExecuteAsync(sender);
                }
            }
            else
            {
                new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, "Poprawne użycie: ExtendChannelExpiration [i]{channelid} {days}[/i]").ExecuteAsync(sender);
            }

        }
        #endregion

        #region pwall
        public static void pwall(string[] args, IQueryClient sender, TS3QueryLib.Net.Core.Server.Notification.EventArgs.MessageReceivedEventArgs e)
        {
            if (args.Length >= 2)
            {
                try
                {
                        EntityListCommandResponse<ClientListEntry> Users = new ClientListCommand().Execute(sender);
                        args[0] = null;
                        foreach (ClientListEntry client in Users.Values)
                        {
                            new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, client.ClientId, string.Join(" ", args)).ExecuteAsync(sender);
                        }

                    new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, "Sukces").ExecuteAsync(sender);

                }
                catch (Exception)
                {
                    new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, "Poprawne użycie: pwall [i]{message}[/i]").ExecuteAsync(sender);
                }
            }
            else
            {
                new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, "Poprawne użycie: pwall [i]{message}[/i]").ExecuteAsync(sender);
            }

        }
        #endregion

        #region alladminstime
        public static void alladminstime(string[] args, IQueryClient sender, TS3QueryLib.Net.Core.Server.Notification.EventArgs.MessageReceivedEventArgs e)
        {
            new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, "[b]Spędzony przez administracje czas:[/b]").ExecuteAsync(sender);
            List<AdminsTimeSpend.AdminsTimeModel> loadData = FileDataHelper.Read<List<AdminsTimeSpend.AdminsTimeModel>>("AdminsTimeSpend");
            foreach (AdminsTimeSpend.AdminsTimeModel admin in loadData)
            {
                string info = $"[b]{admin.Nickname} ( {admin.AdminDatabaseId} )[/b] \n" +
                    $"Dziś: {admin.Time.Last().Time / 3600} godzin ({admin.Time.Last().Time / 60} minut) \n" +
                    $"Ostatnie 7 dni: {admin.Time.Skip(Math.Max(0, admin.Time.Count() - 7)).Sum(x => x.Time) / 3600} godzin \n" +
                    $"Ostatnie 30 dni: {admin.Time.Skip(Math.Max(0, admin.Time.Count() - 30)).Sum(x => x.Time) / 3600} godzin \n" +
                    $"Łącznie: {admin.Time.Sum(x => x.Time) / 3600} godzin \n";
                new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, info).ExecuteAsync(sender);
            }

        }
        #endregion

        #region admintime
        public static void admintime(string[] args, IQueryClient sender, TS3QueryLib.Net.Core.Server.Notification.EventArgs.MessageReceivedEventArgs e)
        {
            if(args.Length >= 3)
            {
                List<AdminsTimeSpend.AdminsTimeModel> loadData = FileDataHelper.Read<List<AdminsTimeSpend.AdminsTimeModel>>("AdminsTimeSpend");
                if (loadData.Any(x => x.Nickname.ToLower().Contains(args[1].ToLower())))
                {
                    AdminsTimeSpend.AdminsTimeModel admin = loadData.Where(x => x.Nickname.ToLower().Contains(args[1].ToLower())).First();
                    try
                    {
                        DateTimeOffset dateTime = DateTimeOffset.Parse(args[2], CultureInfo.CreateSpecificCulture("pl-PL"));
                        long time = admin.Time.Where(x => x.Date.Day == dateTime.Day && x.Date.Month == dateTime.Month && x.Date.Year == dateTime.Year).First().Time;
                        string info = $"[b]{admin.Nickname} ( {admin.AdminDatabaseId} )[/b] \n" +
                            $"Spędzony czas w tym dniu: 0 godzin (0 minut)";
                        if (time != 0)
                        {
                            info = $"[b]{admin.Nickname} ( {admin.AdminDatabaseId} )[/b] \n" +
                            $"Spędzony czas w tym dniu: {time / 3600} godzin ({time / 60} minut)";
                        }
                        new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, info).ExecuteAsync(sender);

                    }
                    catch
                    {
                        new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, "Podana data jest nieprawidłowa").ExecuteAsync(sender);
                    }
                }
                else
                {
                    new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, "Taki użytkownik nie istnieje lub nie jest rejestrowany").ExecuteAsync(sender);
                }
            }
            else
            {
                new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, "Poprawne użycie: AdminTime [i]{nickname} {dd/mm/rrrr}[/i]").ExecuteAsync(sender);
            }

        }
        #endregion

        #region myadmintime
        public static void myadmintime(string[] args, IQueryClient sender, TS3QueryLib.Net.Core.Server.Notification.EventArgs.MessageReceivedEventArgs e)
        {
            List<AdminsTimeSpend.AdminsTimeModel> loadData = FileDataHelper.Read<List<AdminsTimeSpend.AdminsTimeModel>>("AdminsTimeSpend");
            uint databaseid = new ClientGetDbIdFromUIdCommand(e.InvokerUniqueId).Execute(sender).Values.First();
            AdminsTimeSpend.AdminsTimeModel admin = loadData.Where(x => x.AdminDatabaseId == databaseid).FirstOrDefault();
            if(admin != null)
            {
                new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, "[b]Spędzony przez Ciebie czas:[/b]").ExecuteAsync(sender);
                string info = $"[b]{admin.Nickname} ( {admin.AdminDatabaseId} )[/b] \n" +
                    $"Dziś: {admin.Time.Last().Time / 3600} godzin ({admin.Time.Last().Time / 60} minut) \n" +
                    $"Ostatnie 7 dni: {admin.Time.Skip(Math.Max(0, admin.Time.Count() - 7)).Sum(x => x.Time) / 3600} godzin \n" +
                    $"Ostatnie 30 dni: {admin.Time.Skip(Math.Max(0, admin.Time.Count() - 30)).Sum(x => x.Time) / 3600} godzin \n" +
                    $"Łącznie: {admin.Time.Sum(x => x.Time) / 3600} godzin \n";
                new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, info).ExecuteAsync(sender);
            }
            else
            {
                new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, e.InvokerClientId, "Nie jesteś administratorem lub Twój czas nie jest liczony").ExecuteAsync(sender);
            }
           

        }
        #endregion
    }
}
