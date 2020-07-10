using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SunServices.Functions;
using SunServices.Functions.PrivateChannels;
using SunServices.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TS3QueryLib.Net.Core;
using TS3QueryLib.Net.Core.Server.Commands;
using TS3QueryLib.Net.Core.Server.Entitities;

namespace SunServices
{
    public class PrivateChannelsUpdate : IHostedService, IDisposable
    {
        private readonly ILogger<PrivateChannelsUpdate> _logger;
        private readonly IQueryClient _client;
        private Timer _timer;
        private uint _privatezonechannelid;
        private string _channellogourl;

        public PrivateChannelsUpdate(ILogger<PrivateChannelsUpdate> logger, IQueryClient client, IConfiguration configuration)
        {
            _client = client;
            _logger = logger;
            _privatezonechannelid = configuration.GetSection("PrivateChannels:ZoneChannelID").Get<uint>();
            _channellogourl = configuration.GetSection("PrivateChannels:LogoURL").Get<string>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("PrivateChannelsUpdate Background Service is starting.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(60));

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            IEnumerable<ChannelListEntry> AllChannels = new ChannelListCommand(includeAll: true, includeTopics: true).Execute(_client).Values; ;
            IEnumerable<ChannelListEntry> Channels = AllChannels.Where(x => x.ParentChannelId == _privatezonechannelid);


            foreach (ChannelListEntry channel in Channels)
            {
                if (channel.TotalClients > 0 || AllChannels.Where(x => x.ParentChannelId == channel.ChannelId).Any(x => x.TotalClients > 0))
                {
                    DateTimeOffset time = new GetDataFromTopic().Time(channel.Topic);
                    string UniqueId = new GetDataFromTopic().UniqueId(channel.Topic);
                    string[] description = new ChannelInfoCommand(channel.ChannelId).Execute(_client).Description.Split("Wa¿ny do:");
                    if (DateTimeOffset.Now > time.AddDays(-13) && time.Second != 0)
                    {
                        string channeltopic = Base64Helper.Encode(UniqueId + "|+" + DateTimeOffset.Now.AddDays(14).ToUnixTimeSeconds());
                        new ChannelEditCommand(channel.ChannelId,
                            new ChannelModification
                            {
                                Topic = channeltopic,
                                Description = description.First() + String.Format("Wa¿ny do: [b]{0}[/b][/size] \n [hr] \n [center][img]{1}[/img][/center]", DateTime.Now.AddDays(14).ToString("dd.MM.yyyy HH:mm"), _channellogourl)
                            }).ExecuteAsync(_client);
                        _logger.LogInformation("[PrivateChannels] Updated expiration time for {0} ({1})", channel.ChannelId, channel.Name);
                    }

                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("PrivateChannelsUpdate Background Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }



    }
}
