using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SunServices.Functions;
using SunServices.Functions.ClansChannels;
using SunServices.Helpers;
using System;
using System.Buffers.Text;
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
    public class AdminsTimeSpend : IHostedService, IDisposable
    {
        private readonly ILogger<AdminsTimeSpend> _logger;
        private readonly IQueryClient _client;
        private Timer _timer;
        private List<uint> _adminsgroups;

        public AdminsTimeSpend(ILogger<AdminsTimeSpend> logger, IQueryClient client, IConfiguration configuration)
        {
            _client = client;
            _logger = logger;
            _adminsgroups = configuration.GetSection("AdminsTimeSpend:AdminGroups").Get<List<uint>>();
        }

        public class AdminsTimeModel
        {
            public uint AdminDatabaseId { get; set; }
            public string Nickname { get; set; }
            public List<SpendTimeDataModel> Time { get; set; }
        }

        public class SpendTimeDataModel
        {
            public DateTimeOffset Date { get; set; }
            public long Time { get; set; }
        }



        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("AdminsTimeSpend Background Service is starting.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(300));

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            List<EntityListCommandResponse<ServerGroupClient>> Admins = new List<EntityListCommandResponse<ServerGroupClient>>();
            _adminsgroups.ForEach(x => Admins.Add(new ServerGroupClientListCommand(x, includeNicknamesAndUid: true).Execute(_client)));



            List<AdminsTimeModel> loadData = new List<AdminsTimeModel>();
            try
            {
                loadData = FileDataHelper.Read<List<AdminsTimeModel>>("AdminsTimeSpend");
            }
            catch (Exception)
            {
                List<AdminsTimeModel> newdata = new List<AdminsTimeModel>();
                Admins.ForEach(x => x.Values.ForEach(z => newdata.Add(new AdminsTimeModel { AdminDatabaseId = z.DatabaseId, Nickname = z.Nickname, Time = new List<SpendTimeDataModel> { new SpendTimeDataModel { Date = DateTimeOffset.Now, Time = 0 } } })));
                FileDataHelper.Write(newdata, "AdminsTimeSpend");
                loadData = FileDataHelper.Read<List<AdminsTimeModel>>("AdminsTimeSpend");
            }

            //jest na serwerze i nie ma w pliku
            foreach (EntityListCommandResponse<ServerGroupClient> entityListCommandResponse in Admins)
            {
                foreach (ServerGroupClient serverGroupClient in entityListCommandResponse.Values)
                {
                    if (!loadData.Any(x => x.AdminDatabaseId == serverGroupClient.DatabaseId))
                    {
                        loadData.Add(
                            new AdminsTimeModel
                            {
                                AdminDatabaseId = serverGroupClient.DatabaseId,
                                Nickname = serverGroupClient.Nickname,
                                Time = new List<SpendTimeDataModel>
                                {
                                    new SpendTimeDataModel { Date = DateTimeOffset.Now, Time = 0 }
                                }
                            });
                        FileDataHelper.Write(loadData, "AdminsTimeSpend");
                    }
                }
            }
            foreach (AdminsTimeModel adminsTimeModel in loadData.ToList())
            {
                //jest w pliku i na serwerze
                if (Admins.Any(x => x.Values.Any(z => z.DatabaseId == adminsTimeModel.AdminDatabaseId)))
                {
                    EntityListCommandResponse<ClientListEntry> allUsers = new ClientListCommand().Execute(_client);

                    if (loadData.Where(x => x.AdminDatabaseId == adminsTimeModel.AdminDatabaseId).First().Time.Last().Date.Day != DateTimeOffset.Now.Day)
                    {
                        loadData.Where(x => x.AdminDatabaseId == adminsTimeModel.AdminDatabaseId).First().Time.Add(new SpendTimeDataModel
                        {
                            Time = 0,
                            Date = DateTimeOffset.Now
                        });
                    }

                    if (allUsers.Values.Any(x => x.ClientDatabaseId == adminsTimeModel.AdminDatabaseId))
                    {
                        loadData.Where(x => x.AdminDatabaseId == adminsTimeModel.AdminDatabaseId).First().Time.Last().Time += 300;

                    }
                }
                else //jest w pliku nie ma na serwerze
                {
                    loadData.Remove(adminsTimeModel);
                }
                FileDataHelper.Write(loadData, "AdminsTimeSpend");
            }
        }



        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("AdminsTimeSpend Background Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }



    }
}
