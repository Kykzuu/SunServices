using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SunServices.Functions;
using SunServices.Functions.ClansChannels;
using SunServices.Functions.PrivateChannels;
using SunServices.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TS3QueryLib.Net.Core;
using TS3QueryLib.Net.Core.Common.Responses;
using TS3QueryLib.Net.Core.Server.Commands;
using TS3QueryLib.Net.Core.Server.Entitities;

namespace SunServices
{
    public class ClansChannelsCreate : IHostedService, IDisposable
    {
        private readonly ILogger<ClansChannelsCreate> _logger;
        private readonly IQueryClient _client;
        private Timer _timer;

        public ClansChannelsCreate(ILogger<ClansChannelsCreate> logger, IQueryClient client, IConfiguration configuration)
        {
            _client = client;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ClansChannelsCreate Background Service is starting.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(15));

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            EntityListCommandResponse<ClientListEntry> Users = new ClientListCommand(includeGroupInfo: true, includeUniqueId: true).Execute(_client);
            IEnumerable<ClientListEntry> clanschannelusers = Users.Values.Where(x => x.ChannelId == 22394);
            if (clanschannelusers.Count() == 1)
            {
                Regex rg = new Regex(@"^\[spacer_.KK_.[0-9]*\]\.\.\.");
                IEnumerable<ChannelListEntry> ClansSpacerChannels = new ChannelListCommand(includeAll: true, includeTopics: true).Execute(_client).Values.Where(x => rg.Match(x.Name).Success);
                new ServerGroupCopyCommand(26503, $"Klan #{ClansSpacerChannels.Count()+1}", TS3QueryLib.Net.Core.Common.Entities.GroupDatabaseType.Regular).Execute(_client);
                uint ServerGroupCreatedId = new ServerGroupListCommand().Execute(_client).Values.Where(x => x.Name == $"Klan #{ClansSpacerChannels.Count() + 1}").First().Id;
                ClansDataModel clanDataModel = new ClansDataModel()
                {
                    Owner =
                   new OwnerDataModel()
                   {
                       DatabaseID = clanschannelusers.First().ClientDatabaseId,
                       UniqueID = clanschannelusers.First().ClientUniqueId,
                   },
                    CreatedDate = DateTimeOffset.Now,
                    GroupID = ServerGroupCreatedId,
                    ActivityTime = new List<ActivityTimeDataModel>
                {
                        new ActivityTimeDataModel()
                        {
                            Date = DateTimeOffset.Now,
                            Time = 0
                        }
                }

                };

                new ServerGroupAddClientCommand(ServerGroupCreatedId, clanschannelusers.First().ClientDatabaseId).ExecuteAsync(_client);
                string output = JsonConvert.SerializeObject(clanDataModel);
                string datetime = ".KK_"+DateTimeOffset.Now.ToUnixTimeSeconds().ToString();

                uint NewChannelOrder;
                if(ClansSpacerChannels.Count() == 0)
                {
                    NewChannelOrder = 72313;
                }
                else
                {
                    NewChannelOrder = ClansSpacerChannels.Last().ChannelId;
                }
                EntityListCommandResponse<ChannelListEntry> channels = new ChannelListCommand(includeAll: true, includeTopics: true).Execute(_client);
                    new ChannelCreateCommand(
                    new ChannelModification
                    {
                        ChannelOrder = NewChannelOrder,
                        Topic = datetime,
                        IsPermanent = true,
                        Description = $"[center][size=11] Kanał Klanowy[/center] [size=10] \n Lider: [b][URL=client://0/{clanDataModel.Owner.UniqueID}~{clanschannelusers.First().Nickname}]{clanschannelusers.First().Nickname}[/URL][/b]\n Stworzony dnia: [b]{clanDataModel.CreatedDate.ToString("dd.MM.yyyy HH:mm")}[/b]\n Spędzony czas:\n    {clanDataModel.ActivityTime.First().Date.ToString("MM.yyyy")} - [b]0h[/b] [/size] \n [hr] [center][img]https://sunnight.pl/PrivateChannelsLogo.png[/img][/center][size=1] {Base64Helper.Encode(output)}",
                        Name = Truncate(String.Format("[cspacer]Kanał klanowy {0}", ClansSpacerChannels.Count() + 1), 40),
                        MaxClients = 0
                    }
                    ).Execute(_client);

                    channels = new ChannelListCommand(includeAll: true, includeTopics: true).Execute(_client);
                    ChannelListEntry createdchannel = channels.Values.Where(x => x.Topic == datetime).First();
                    new SetClientChannelGroupCommand(35, createdchannel.ChannelId, clanschannelusers.First().ClientDatabaseId).ExecuteAsync(_client);

                new ChannelCreateCommand(
                    new ChannelModification
                    {
                        ParentChannelId = createdchannel.ChannelId,
                        IsPermanent = true,
                        Name = $"Klan {ClansSpacerChannels.Count() + 1}",
                        MaxClients = 0
                    }
                    ).Execute(_client);
                channels = new ChannelListCommand(includeAll: true, includeTopics: true).Execute(_client);
                uint FuncChannelId = channels.Values.Where(x => x.ParentChannelId == createdchannel.ChannelId).First().ChannelId;
                new ChannelCreateCommand(
                        new ChannelModification
                        {
                            ParentChannelId = FuncChannelId,
                            IsPermanent = true,
                            Topic = "ADD",
                            Name = "Nadaj rangę",
                            MaxClients = 0
                        }
                        ).Execute(_client);

                    new ChannelCreateCommand(
                        new ChannelModification
                        {
                            ParentChannelId = FuncChannelId,
                            IsPermanent = true,
                            Topic = "DELETE",
                            Name = "Zabierz rangę",
                            MaxClients = 0
                        }
                        ).Execute(_client);

                    new ChannelCreateCommand(
                        new ChannelModification
                        {
                            ParentChannelId = FuncChannelId,
                            IsPermanent = true,
                            Topic = "ONLINE",
                            Name = "Obecnie online: 0/0",
                            MaxClients = 0
                        }
                        ).Execute(_client);

                for (int i = 2; i < 9 + 2; i++)
                {
                    new ChannelCreateCommand(
                        new ChannelModification
                        {
                            ParentChannelId = createdchannel.ChannelId,
                            IsPermanent = true,
                            Name = String.Format("#{0}", i),
                            CodecQuality = 10
                        }
                        ).Execute(_client);
                }

                new ChannelCreateCommand(
                    new ChannelModification
                    {
                        ChannelOrder = createdchannel.ChannelId,
                        IsPermanent = true,
                        Name = $"[spacer_{datetime}]...",
                        MaxClients = 0
                    }
                    ).Execute(_client);

                new ChannelAddPermCommand(createdchannel.ChannelId, new NamedPermissionLight { Name = "i_channel_needed_modify_power", Value = 100 }).ExecuteAsync(_client);
                    new ClientMoveCommand(clanschannelusers.First().ClientId, createdchannel.ChannelId).ExecuteAsync(_client);
                    _logger.LogInformation("[ClansChannelsCreate] Created clan channel for {0} ({1}) {2}", clanschannelusers.First().ClientUniqueId, clanschannelusers.First().Nickname, clanschannelusers.First().ClientIP);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ClansChannelsCreate Background Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

    }
}
