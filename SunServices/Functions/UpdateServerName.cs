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

namespace SunServices
{
    public class UpdateServerName : IHostedService, IDisposable
    {
        private readonly ILogger<UpdateServerName> _logger;
        private readonly IQueryClient _client;
        private Timer _timer;
        private string _servername;

        public UpdateServerName(ILogger<UpdateServerName> logger, IQueryClient client, IConfiguration configuration)
        {
            _client = client;
            _logger = logger;
            _servername = configuration.GetSection("UpdateServerName:ServerName").Get<string>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("UpdateServerName Background Service is starting.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(30));

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            EntityListCommandResponse<ClientListEntry> Users = new ClientListCommand().Execute(_client);
            ServerInfoCommandResponse ServerInfo = new ServerInfoCommand().Execute(_client);
            new ServerEditCommand(new VirtualServerModification { Name = String.Format("{0} [{1}/{2}]", _servername, Users.Values.Where(x => x.ClientType == 0).Count(), ServerInfo.MaximumClientsAllowed) }).ExecuteAsync(_client);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("UpdateServerName Background Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }



    }
}
