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
    public class HelpChannel : IHostedService, IDisposable
    {
        private readonly ILogger<HelpChannel> _logger;
        private readonly IQueryClient _client;
        private Timer _timer;
        private uint _helpchannelid;
        private List<uint> _adminsgroups;
        private string _adminmessage;
        private string _usermessage;
        private string _userofflineadmin;
        private string _afksymbol;

        public HelpChannel(ILogger<HelpChannel> logger, IQueryClient client, IConfiguration configuration)
        {
            _client = client;
            _logger = logger;
            _helpchannelid = configuration.GetSection("HelpChannel:HelpChannelID").Get<uint>();
            _adminsgroups = configuration.GetSection("HelpChannel:AdminGroups").Get<List<uint>>();
            _adminmessage = configuration.GetSection("HelpChannel:AdminMessage").Get<string>();
            _usermessage = configuration.GetSection("HelpChannel:UserMessage").Get<string>();
            _userofflineadmin = configuration.GetSection("HelpChannel:AdminsOfflineMessage").Get<string>();
            _afksymbol = configuration.GetSection("HelpChannel:AFKSymbol").Get<string>();

        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("HelpChannel Background Service is starting.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(10));

            return Task.CompletedTask;
        }

        private async void DoWork(object state)
        {
            EntityListCommandResponse<ClientListEntry> Users = new ClientListCommand(includeGroupInfo: true).Execute(_client);
            IEnumerable<ClientListEntry> helpchannelusers = Users.Values.Where(x => x.ChannelId == _helpchannelid);
            IEnumerable<ClientListEntry> adminsonline = Users.Values.Where(s => s.ServerGroups.Any(x => _adminsgroups.Any(z => z == x)));
            if (!helpchannelusers.Any(s => s.ServerGroups.Any(x => _adminsgroups.Any(z => z == x))) && helpchannelusers.Count() > 0)
            {
                if (adminsonline.Where(x => !x.Nickname.Contains(_afksymbol)).Count() > 0)
                {
                    foreach (ClientListEntry admin in adminsonline)
                    {
                        if (!admin.Nickname.Contains(_afksymbol))
                        {
                            await new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, admin.ClientId, _adminmessage).ExecuteAsync(_client);
                        }
                    }
                    _logger.LogInformation("[HelpChannel] Sended notification to admins {0} for {1}", String.Join(",", adminsonline.Select(x => x.Nickname)), helpchannelusers.First().Nickname);
                    foreach (ClientListEntry client in helpchannelusers)
                    {
                        await new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, client.ClientId, _usermessage).ExecuteAsync(_client);
                    }
                    _logger.LogInformation("[HelpChannel] Sended notification to user {0}", helpchannelusers.First().Nickname);
                }
                else
                {
                    foreach (ClientListEntry client in helpchannelusers)
                    {
                        await new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, client.ClientId, _userofflineadmin).ExecuteAsync(_client);
                    }
                    _logger.LogInformation("[HelpChannel] Sended notification to user {0}", helpchannelusers.First().Nickname);
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("HelpChannel Background Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }



    }
}
