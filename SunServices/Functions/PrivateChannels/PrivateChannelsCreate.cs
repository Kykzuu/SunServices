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
using TS3QueryLib.Net.Core.Common.Responses;
using TS3QueryLib.Net.Core.Server.Commands;
using TS3QueryLib.Net.Core.Server.Entitities;

namespace SunServices.Functions.PrivateChannels
{
    public class PrivateChannelsCreate : IHostedService, IDisposable
    {
        private readonly ILogger<PrivateChannelsCreate> _logger;
        private readonly IQueryClient _client;
        private Timer _timer;
        private uint _privatezonechannelid;
        private string _channellogourl;
        private int _subchannels;
        private string _createchannelmessage;
        private uint _createchannel;
        private uint _channeladmingroup;

        public PrivateChannelsCreate(ILogger<PrivateChannelsCreate> logger, IQueryClient client, IConfiguration configuration)
        {
            _client = client;
            _logger = logger;
            _privatezonechannelid = configuration.GetSection("PrivateChannels:ZoneChannelID").Get<uint>();
            _channellogourl = configuration.GetSection("PrivateChannels:LogoURL").Get<string>();
            _subchannels = configuration.GetSection("PrivateChannels:SubChannels").Get<int>();
            _createchannelmessage = configuration.GetSection("PrivateChannels:CreateChannelMessage").Get<string>();
            _createchannel = configuration.GetSection("PrivateChannels:AutoCreateChannelID").Get<uint>();
            _channeladmingroup = configuration.GetSection("PrivateChannels:ChannelAdminGroup").Get<uint>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("PrivateChannelsCreate Background Service is starting.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(15));

            return Task.CompletedTask;
        }

        private async void DoWork(object state)
        {
            EntityListCommandResponse<ClientListEntry> Users = new ClientListCommand(includeGroupInfo: true, includeUniqueId: true).Execute(_client);
            IEnumerable<ClientListEntry> privatechannelsusers = Users.Values.Where(x => x.ChannelId == _createchannel);
            if (privatechannelsusers.Count() == 1)
            {
                string channeltopic = Base64Helper.Encode(privatechannelsusers.First().ClientUniqueId + "|+" + DateTimeOffset.Now.AddDays(14).ToUnixTimeSeconds() + "|+" + DateTimeOffset.Now.ToUnixTimeSeconds());
                EntityListCommandResponse<ChannelListEntry> channels = new ChannelListCommand(includeAll: true, includeTopics: true).Execute(_client);

                if (!channels.Values.Any(x => new GetDataFromTopic().UniqueId(x.Topic) == privatechannelsusers.First().ClientUniqueId))
                {
                    new ChannelCreateCommand(
                    new ChannelModification
                    {
                        ParentChannelId = _privatezonechannelid,
                        IsPermanent = true,
                        Description = String.Format("[center][size=11] Kana³ Prywatny[/center] [size=10] \n W³aœciciel kana³u: [b][URL=client://0/{0}~{1}]{1}[/URL][/b] \n Stworzony dnia: [b]{2}[/b] \n Wa¿ny do: [b]{3}[/b][/size] \n [hr] \n [center][img]{4}[/img][/center]", privatechannelsusers.First().ClientUniqueId, privatechannelsusers.First().Nickname, DateTime.Now.ToString("dd.MM.yyyy HH:mm"), DateTime.Now.AddDays(14).ToString("dd.MM.yyyy HH:mm"), _channellogourl),
                        Topic = channeltopic,
                        Name = Truncate(String.Format("{0}. Kana³ prywatny {1}", channels.Values.Where(x => x.ParentChannelId == _privatezonechannelid).Count() + 1, privatechannelsusers.First().Nickname), 40),
                        CodecQuality = 10
                    }
                    ).Execute(_client);

                    channels = new ChannelListCommand(includeAll: true, includeTopics: true).Execute(_client);
                    ChannelListEntry createdchannel = channels.Values.Where(x => x.Topic == channeltopic).First();
                    await new SetClientChannelGroupCommand(_channeladmingroup, createdchannel.ChannelId, privatechannelsusers.First().ClientDatabaseId).ExecuteAsync(_client);
                    for (int i = 1; i < _subchannels + 1; i++)
                    {
                        new ChannelCreateCommand(
                            new ChannelModification
                            {
                                ParentChannelId = createdchannel.ChannelId,
                                IsPermanent = true,
                                Name = String.Format("{0}. podkana³", i),
                                CodecQuality = 10
                            }
                            ).Execute(_client);
                    }
                    await new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, privatechannelsusers.First().ClientId, _createchannelmessage).ExecuteAsync(_client);
                    await new ClientMoveCommand(privatechannelsusers.First().ClientId, createdchannel.ChannelId).ExecuteAsync(_client);
                    _logger.LogInformation("[PrivateChannels] Created private channel for {0} ({1}) {2}", privatechannelsusers.First().ClientUniqueId, privatechannelsusers.First().Nickname, privatechannelsusers.First().ClientIP);
                }
                else
                {
                    await new SendTextMessageCommand(TS3QueryLib.Net.Core.Common.CommandHandling.MessageTarget.Client, privatechannelsusers.First().ClientId, "Posiadasz ju¿ jeden kana³ prywatny!").ExecuteAsync(_client);
                    await new ClientMoveCommand(privatechannelsusers.First().ClientId, channels.Values.Where(x => new GetDataFromTopic().UniqueId(x.Topic) == privatechannelsusers.First().ClientUniqueId).First().ChannelId).ExecuteAsync(_client);
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("PrivateChannelsCreate Background Service is stopping.");

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
