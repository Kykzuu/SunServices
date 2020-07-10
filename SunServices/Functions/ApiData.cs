using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SunServices.Functions.ClansChannels;
using SunServices.Helpers;
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
    public class ApiData : IHostedService, IDisposable
    {
        private readonly ILogger<ApiData> _logger;
        private readonly IQueryClient _client;
        private Timer _timer;

        public ApiData(ILogger<ApiData> logger, IQueryClient client, IConfiguration configuration)
        {
            _client = client;
            _logger = logger;
        }

        private class SimpleUsersOnline
        {
            public int Online { get; set; }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ApiData Background Service is starting.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(30));

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            EntityListCommandResponse<ClientListEntry> Users = new ClientListCommand(includeAll: true).Execute(_client);
            FileDataHelper.Write(new SimpleUsersOnline { Online = Users.Values.Count() }, "ApiSimpleUsersOnline");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ApiData Background Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }



    }
}
