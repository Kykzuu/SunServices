using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TS3QueryLib.Net.Core;
using TS3QueryLib.Net.Core.Common.Responses;
using TS3QueryLib.Net.Core.Server.Commands;
using TS3QueryLib.Net.Core.Server.Entitities;

namespace SunServices.Functions
{
    public class RegisterUser : IHostedService, IDisposable
    {
        private readonly ILogger<RegisterUser> _logger;
        private readonly IQueryClient _client;
        private Timer _timer;
        private uint _registergroupid;
        private int _timetoregister; //seconds
        private string _registermessage;

        public RegisterUser(ILogger<RegisterUser> logger, IQueryClient client, IConfiguration configuration)
        {
            _client = client;
            _logger = logger;
            _registergroupid = configuration.GetSection("RegisterUser:RegisterGroupID").Get<uint>();
            _timetoregister = configuration.GetSection("RegisterUser:TimeToRegister").Get<int>();
            _registermessage = configuration.GetSection("RegisterUser:RegisterMessage").Get<string>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("RegisterUser Background Service is starting.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(60));

            return Task.CompletedTask;
        }

        private async void DoWork(object state)
        {
            EntityListCommandResponse<ClientListEntry> Users = new ClientListCommand(includeAll: true).Execute(_client);
            IEnumerable<ClientListEntry> newusers = Users.Values.Where(x => x.ClientType == 0).Where(x => !x.ServerGroups.Any(x => x == _registergroupid)).Where(x => x.ClientCreated < DateTime.Now.AddSeconds(-_timetoregister));
            foreach (ClientListEntry client in newusers)
            {
                await new ServerGroupAddClientCommand(_registergroupid, client.ClientDatabaseId).ExecuteAsync(_client);
                await new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, client.ClientId, _registermessage).ExecuteAsync(_client);
                _logger.LogInformation("[RegisterUser] Registered user {0} ({1}) {2}", client.ClientUniqueId, client.Nickname, client.ClientIP);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("RegisterUser Background Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }



    }
}
