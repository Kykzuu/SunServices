using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SunServices.Functions;
using SunServices.Functions.ClansChannels;
using SunServices.Helpers;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TS3QueryLib.Net.Core;
using TS3QueryLib.Net.Core.Common.Responses;
using TS3QueryLib.Net.Core.Server.Commands;
using TS3QueryLib.Net.Core.Server.Entitities;
using TS3QueryLib.Net.Core.Server.Responses;

namespace SunServices
{
    public class ClansChannelsFuncs : IHostedService, IDisposable
    {
        private readonly ILogger<ClansChannelsFuncs> _logger;
        private readonly IQueryClient _client;
        private Timer _timer;

        public ClansChannelsFuncs(ILogger<ClansChannelsFuncs> logger, IQueryClient client, IConfiguration configuration)
        {
            _client = client;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ClansChannelsFuncs Background Service is starting.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(20));

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            Regex rg = new Regex(@"^.KK_.[0-9]*");
            IEnumerable<ChannelListEntry> Channels = new ChannelListCommand(includeAll: true, includeTopics: true).Execute(_client).Values;
            EntityListCommandResponse<ClientListEntry> Users = new ClientListCommand(includeGroupInfo: true, includeTimes: true).Execute(_client);

            IEnumerable<ChannelListEntry> ClansSpacerChannels = Channels.Where(x => x.Topic != null).Where(x => rg.IsMatch(x.Topic));
            List<ClansDataModel> allClansData = new List<ClansDataModel>();
            ClansSpacerChannels.ForEach(y => allClansData.Add(JsonConvert.DeserializeObject<ClansDataModel>(Base64Helper.Decode(new ChannelInfoCommand(y.ChannelId).Execute(_client).Description.Split("[size=1]").Last()))));
            foreach (ChannelListEntry clan in ClansSpacerChannels)
            {
                IEnumerable<ChannelListEntry> FuncChannels = Channels.Where(x => x.ParentChannelId == Channels.Where(x => x.ParentChannelId == clan.ChannelId).First().ChannelId);
                if(FuncChannels.Count() == 3)
                {
                    ClansDataModel clanData = JsonConvert.DeserializeObject<ClansDataModel>(Base64Helper.Decode(new ChannelInfoCommand(clan.ChannelId).Execute(_client).Description.Split("[size=1]").Last()));
                    foreach (ClientListEntry addChannelUser in Users.Values.Where(x => x.ChannelId == FuncChannels.Where(x => x.Topic == "ADD").First().ChannelId))
                    {
                        if(!addChannelUser.ServerGroups.Any(x => allClansData.Any(z => z.GroupID == x))){
                            new ServerGroupAddClientCommand(clanData.GroupID, addChannelUser.ClientDatabaseId).ExecuteAsync(_client);
                        }
                    }

                    foreach (ClientListEntry deleteChannelUser in Users.Values.Where(x => x.ChannelId == FuncChannels.Where(x => x.Topic == "DELETE").First().ChannelId))
                    {
                        if (deleteChannelUser.ClientDatabaseId != clanData.Owner.DatabaseID)
                        {
                            new ServerGroupDelClientCommand(clanData.GroupID, deleteChannelUser.ClientDatabaseId).ExecuteAsync(_client);
                        }
                    }

                    IEnumerable<ServerGroupClient> rankusers = new ServerGroupClientListCommand(clanData.GroupID, includeNicknamesAndUid: true).Execute(_client).Values;
                    string[] descriptions = "[center][size=15]Online [size=10]\n \nSPLIT [hr] \n[size=15]Offline [size=10]\n \nSPLIT".Split("SPLIT");
                    foreach (ServerGroupClient rankuser in rankusers.Where(x => Users.Values.All(z => z.ClientDatabaseId != x.DatabaseId)))
                    {
                        descriptions[2] = String.Concat(descriptions[2], $"[URL=client://0/{rankuser.UniqueId}]{rankuser.Nickname}[/URL]\n");
                    }

                    foreach (ServerGroupClient rankuser in rankusers.Where(x => Users.Values.Any(z => z.ClientDatabaseId == x.DatabaseId)))
                    {
                        descriptions[1] = String.Concat($"[URL=client://0/{rankuser.UniqueId}]{rankuser.Nickname}[/URL] \n", descriptions[1]);
                    }


                    string desc = String.Concat(descriptions[0], descriptions[1], descriptions[2]);
                    new ChannelEditCommand(FuncChannels.Where(x => x.Topic == "ONLINE").First().ChannelId, new ChannelModification
                    {
                        Name = $"Obecnie online: {Users.Values.Where(x => x.ServerGroups.Any(x => x == clanData.GroupID)).Count()}/{rankusers.Count()}",
                        Description = desc
                    }).ExecuteAsync(_client);
                }

            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ClansChannelsFuncs Background Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }



    }
}
