using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TS3QueryLib.Net.Core;
using TS3QueryLib.Net.Core.Server.Commands;
using TS3QueryLib.Net.Core.Server.Entitities;

namespace SunServices
{
    public class PrivateChannelsNumbering : IHostedService, IDisposable
    {
        private readonly ILogger<PrivateChannelsNumbering> _logger;
        private readonly IQueryClient _client;
        private Timer _timer;
        private uint _privatezonechannelid;

        public PrivateChannelsNumbering(ILogger<PrivateChannelsNumbering> logger, IQueryClient client, IConfiguration configuration)
        {
            _client = client;
            _logger = logger;
            _privatezonechannelid = configuration.GetSection("PrivateChannels:ZoneChannelID").Get<uint>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("PrivateChannelsNumbering Background Service is starting.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(600));

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            IEnumerable<ChannelListEntry> Channels = new ChannelListCommand(includeAll: true, includeTopics: true).Execute(_client).Values.Where(x => x.ParentChannelId == _privatezonechannelid);

            int i = 1;
            foreach (ChannelListEntry channel in Channels)
            {
                Regex rg = new Regex(@"^[0-9]*\d(?:\. .+)");
                MatchCollection matched = rg.Matches(channel.Name);
                if (matched.Count == 1)
                {
                    string[] name = channel.Name.Split(". ");
                    if (name.First() != i.ToString())
                    {
                        new ChannelEditCommand(channel.ChannelId, new ChannelModification { Name = i + ". " + name.Last() }).ExecuteAsync(_client);
                        _logger.LogInformation("[PrivateChannels] Fixed numbering for channel {0} ({1})", channel.ChannelId, channel.Name);
                    }
                }
                else
                {
                    new ChannelEditCommand(channel.ChannelId, new ChannelModification { Name = i + ". Naruszenie numeracji" }).ExecuteAsync(_client);
                    _logger.LogInformation("[PrivateChannels] Fixed numbering for channel {0} ({1})", channel.ChannelId, channel.Name);
                }
                i++;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("PrivateChannelsNumbering Background Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }



    }
}
