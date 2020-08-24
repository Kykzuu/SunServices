using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SunServices.Functions.ClansChannels;
using SunServices.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TS3QueryLib.Net.Core;
using TS3QueryLib.Net.Core.Server.Commands;
using TS3QueryLib.Net.Core.Server.Entitities;
using TS3QueryLib.Net.Core.Server.Responses;

namespace SunServices.Functions.ServerStatsToDB
{
    public class ServerStatsToDB : IHostedService, IDisposable
    {
        private readonly ILogger<ServerStatsToDB> _logger;
        private readonly IQueryClient _client;
        private Timer _timer;
        private string _connString;
        private string _provider;
        private List<uint> _adminsgroups;
        private uint _privatezonechannelid;

        public ServerStatsToDB(ILogger<ServerStatsToDB> logger, IQueryClient client, IConfiguration configuration)
        {
            _client = client;
            _logger = logger;
            _connString = configuration.GetSection("ServerStatsToDB:ConnString").Get<string>();
            _provider = configuration.GetSection("ServerStatsToDB:Provider").Get<string>();
            _adminsgroups = configuration.GetSection("ServerStatsToDB:AdminGroups").Get<List<uint>>();
            _privatezonechannelid = configuration.GetSection("PrivateChannels:ZoneChannelID").Get<uint>();

        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ServerStatsToDB Background Service is starting.");
            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(300));

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            IReadOnlyList<ClientListEntry> users = new ClientListCommand(includeGroupInfo: true).Execute(_client).Values;
            ServerInfoCommandResponse serverInfo = new ServerInfoCommand().Execute(_client);
            IEnumerable<ChannelListEntry> serverChannels = new ChannelListCommand(includeAll: true, includeTopics: true).Execute(_client).Values;

            int adminsOnline = users.Count(s => s.ServerGroups.Any(x => _adminsgroups.Any(z => z == x)));
            int playersOnline = users.Count(s => s.ClientType == 0);
            int queryClientsOnline = users.Count(s => s.ClientType == 1);
            int channels = serverInfo.ChannelsOnline;
            float averagePing = Convert.ToSingle(serverInfo.TotalPing);
            float averagePacketLoss = Convert.ToSingle(serverInfo.TotalPacketLossTotal * 100);
            int privateChannels = serverChannels.Count(x => x.ParentChannelId == _privatezonechannelid);
            float BandwidthReceived = serverInfo.BandWidthReceivedLastMinuteTotal;
            float BandwidthSent = serverInfo.BandWidthSentLastMinuteTotal;
            int privateChannelsUsers = 0;
            foreach (ChannelListEntry channel in serverChannels.Where(x => x.ParentChannelId == _privatezonechannelid))
            {
                privateChannelsUsers += channel.TotalClients;
                foreach (ChannelListEntry subChannel in serverChannels.Where(x => x.ParentChannelId == channel.ChannelId))
                {
                    privateChannelsUsers += subChannel.TotalClients;
                }
            }

            Regex rg = new Regex(@"^.KK_.[0-9]*");
            IEnumerable<ChannelListEntry> ClansSpacerChannels = serverChannels.Where(x => x.Topic != null).Where(x => rg.IsMatch(x.Topic));
            int ClansChannels = ClansSpacerChannels.Count();
            int ClansChannelsUsers = 0;
            foreach (ChannelListEntry clan in ClansSpacerChannels)
            {
                ClansDataModel clanData = JsonConvert.DeserializeObject<ClansDataModel>(Base64Helper.Decode(new ChannelInfoCommand(clan.ChannelId).Execute(_client).Description.Split("[size=1]").Last()));
                ClansChannelsUsers += users.Count(x => x.ServerGroups.Any(x => x == clanData.GroupID));
            }

            DateTimeOffset dateTimeOffset = DateTimeOffset.Now;
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

            IServerStatsToDB statsToDB = null;

            if (_provider.ToLower().Contains("mysql"))
            {
                statsToDB = new MySQL();
            }

            statsToDB.PlayersOnline(playersOnline, dateTimeOffset, _connString);
            statsToDB.QueryClientsOnline(queryClientsOnline, dateTimeOffset, _connString);
            statsToDB.PrivateChannels(privateChannels, dateTimeOffset, _connString);
            statsToDB.PrivateChannelsUsers(privateChannelsUsers, dateTimeOffset, _connString);
            statsToDB.ClansChannels(ClansChannels, dateTimeOffset, _connString);
            statsToDB.ClansChannelsUsers(ClansChannelsUsers, dateTimeOffset, _connString);
            statsToDB.AdminsOnline(adminsOnline, dateTimeOffset, _connString);
            statsToDB.AveragePing(averagePing, dateTimeOffset, _connString);
            statsToDB.AveragePacketLoss(averagePacketLoss, dateTimeOffset, _connString);
            statsToDB.BandwidthSent(BandwidthSent, dateTimeOffset, _connString);
            statsToDB.BandwidthReceived(BandwidthReceived, dateTimeOffset, _connString);
            statsToDB.Channels(channels, dateTimeOffset, _connString);

        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ServerStatsToDB Background Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }



    }
}
