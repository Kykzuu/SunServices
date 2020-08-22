using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SunServices.Functions;
using SunServices.Functions.ClansChannels;
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
using TS3QueryLib.Net.Core.Server.Responses;

namespace SunServices.Functions.ClansChannels
{
    public class ClansChannelsTimeData : IHostedService, IDisposable
    {
        private readonly ILogger<ClansChannelsTimeData> _logger;
        private readonly IQueryClient _client;
        private Timer _timer;

        public ClansChannelsTimeData(ILogger<ClansChannelsTimeData> logger, IQueryClient client, IConfiguration configuration)
        {
            _client = client;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ClansChannelsTimeData Background Service is starting.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(305));

            return Task.CompletedTask;
        }

        private async void DoWork(object state)
        {
            Regex rg = new Regex(@"^.KK_.[0-9]*");
            IEnumerable<ChannelListEntry> Channels = new ChannelListCommand(includeAll: true, includeTopics: true).Execute(_client).Values;
            EntityListCommandResponse<ClientListEntry> Users = new ClientListCommand(includeAll: true).Execute(_client);

            IEnumerable<ChannelListEntry> ClansSpacerChannels = Channels.Where(x => x.Topic != null).Where(x => rg.IsMatch(x.Topic));
            foreach (ChannelListEntry clan in ClansSpacerChannels)
            {
                ClansDataModel clanData = JsonConvert.DeserializeObject<ClansDataModel>(Base64Helper.Decode(new ChannelInfoCommand(clan.ChannelId).Execute(_client).Description.Split("[size=1]").Last()));
                IEnumerable<ClientListEntry> onlineUsers = Users.Values.Where(x => x.ServerGroups.Any(x => x == clanData.GroupID)).Where(x => DateTimeOffset.Now > x.ClientLastConnected.Value.AddMinutes(5) || DateTimeOffset.Now < x.ClientLastConnected.Value.AddHours(24));
                ActivityTimeDataModel activityTimeDataModel = clanData.ActivityTime.Where(x => x.Date.Month == DateTimeOffset.Now.Month).FirstOrDefault();
                if (activityTimeDataModel != null)
                {
                    clanData.ActivityTime.Where(x => x.Date.Month == DateTime.Now.Month).First().Time += onlineUsers.Count()*5;
                }
                else
                {
                    clanData.ActivityTime.Add(new ActivityTimeDataModel { Date = DateTimeOffset.Now, Time = onlineUsers.Count() * 5 });
                }
                string output = JsonConvert.SerializeObject(clanData);

                string[] descriptionbuilder = new ChannelInfoCommand(clan.ChannelId).Execute(_client).Description.Split("Spêdzony czas:");
                string[] descriptionbuilder2 = descriptionbuilder[1].Split("[/size]");
                descriptionbuilder2[0] = null;
                foreach (ActivityTimeDataModel activityTimeData in clanData.ActivityTime)
                {
                    descriptionbuilder2[0] = String.Concat($"\n    {activityTimeData.Date.ToString("MM.yyyy")} - [b]{activityTimeData.Time/60}h[/b]" + descriptionbuilder2[0]);
                }
                descriptionbuilder2[1] = $"[hr]\n[center][img]https://sunnight.pl/PrivateChannelsLogo.png[/img][/center][size=1]\n{Base64Helper.Encode(output)}";
                string enddesc = String.Concat(descriptionbuilder[0] + "Spêdzony czas:" +descriptionbuilder2[0] + "[/size]" + descriptionbuilder2[1]);
                await new ChannelEditCommand(clan.ChannelId, new ChannelModification
                {
                    Description = enddesc
                }).ExecuteAsync(_client);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ClansChannelsTimeData Background Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }



    }
}
