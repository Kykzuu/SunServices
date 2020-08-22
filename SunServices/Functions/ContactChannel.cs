using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SunServices.Functions.ClansChannels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TS3QueryLib.Net.Core;
using TS3QueryLib.Net.Core.Common.Responses;
using TS3QueryLib.Net.Core.Server.Commands;
using TS3QueryLib.Net.Core.Server.Entitities;
using TS3QueryLib.Net.Core.Server.Responses;

namespace SunServices.Functions
{
    public class ContactChannel : IHostedService, IDisposable
    {
        private readonly ILogger<ContactChannel> _logger;
        private readonly IQueryClient _client;
        private Timer _timer;
        private uint _contactchannelid;

        public ContactChannel(ILogger<ContactChannel> logger, IQueryClient client, IConfiguration configuration)
        {
            _client = client;
            _logger = logger;
            _contactchannelid = configuration.GetSection("ContactChannel:ContactChannelID").Get<uint>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ContactChannel Background Service is starting.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(10));

            return Task.CompletedTask;
        }

        private async void DoWork(object state)
        {
            EntityListCommandResponse<ClientListEntry> Users = new ClientListCommand().Execute(_client);
            IEnumerable<ClientListEntry> channelusers = Users.Values.Where(x => x.ChannelId == _contactchannelid);
            foreach(ClientListEntry client in channelusers)
            {
                await new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, client.ClientId, "Cześć! Wpisz [b]pomoc[/b] by uzyskać liste wszystkich komend").ExecuteAsync(_client);
                await new ClientKickCommand(client.ClientId, TS3QueryLib.Net.Core.Common.Entities.KickReason.Channel).ExecuteAsync(_client);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ContactChannel Background Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }



    }
}
