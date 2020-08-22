using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SunServices.Functions.PrivateChannels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TS3QueryLib.Net.Core;
using TS3QueryLib.Net.Core.Server.Commands;
using TS3QueryLib.Net.Core.Server.Entitities;

namespace SunServices.Functions.PrivateChannels
{
    public class PrivateChannelsDelete : IHostedService, IDisposable
    {
        private readonly ILogger<PrivateChannelsDelete> _logger;
        private readonly IQueryClient _client;
        private Timer _timer;
        private uint _privatezonechannelid;

        public PrivateChannelsDelete(ILogger<PrivateChannelsDelete> logger, IQueryClient client, IConfiguration configuration)
        {
            _client = client;
            _logger = logger;
            _privatezonechannelid = configuration.GetSection("PrivateChannels:ZoneChannelID").Get<uint>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("PrivateChannelsDelete Background Service is starting.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(32400));

            return Task.CompletedTask;
        }

        private async void DoWork(object state)
        {
            IEnumerable<ChannelListEntry> AllChannels = new ChannelListCommand(includeAll: true, includeTopics: true).Execute(_client).Values; ;
            IEnumerable<ChannelListEntry> Channels = AllChannels.Where(x => x.ParentChannelId == _privatezonechannelid);
            foreach (ChannelListEntry channel in Channels)
            {
                DateTimeOffset time = new GetDataFromTopic().Time(channel.Topic);
                if (DateTimeOffset.Now > time && time.Second != 0)
                {
                    await new ChannelDeleteCommand(channel.ChannelId).ExecuteAsync(_client);
                    _logger.LogInformation("[PrivateChannels] Deleted channel {0} ({1})", channel.ChannelId, channel.Name);
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("PrivateChannelsDelete Background Service is stopping.");

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
