using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TS3QueryLib.Net.Core;
using TS3QueryLib.Net.Core.Common.Responses;
using TS3QueryLib.Net.Core.Server.Commands;
using TS3QueryLib.Net.Core.Server.Entitities;

namespace SunServices
{
    public class Banner : IHostedService, IDisposable
    {
        private readonly ILogger<Banner> _logger;
        private readonly IQueryClient _client;
        private Timer _timer;
        private List<Single> _onlineTextLocationList;
        private List<Single> _pingTextLocationList;
        private List<Single> _onlineLocationList;
        private List<Single> _pingLocationList;
        private bool _enabled;

        public Banner(ILogger<Banner> logger, IQueryClient client, IConfiguration configuration)
        {
            _client = client;
            _logger = logger;
            _onlineTextLocationList = configuration.GetSection("Banner:onlineTextLocation").Get<List<Single>>();
            _pingTextLocationList = configuration.GetSection("Banner:pingTextLocation").Get<List<Single>>();
            _onlineLocationList = configuration.GetSection("Banner:onlineLocation").Get<List<Single>>();
            _pingLocationList = configuration.GetSection("Banner:pingLocation").Get<List<Single>>();
            _enabled = configuration.GetSection("Banner:enabled").Get<bool>();

        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_enabled)
            {
                _logger.LogInformation("Banner Background Service is starting.");
                _timer = new Timer(DoWork, null, TimeSpan.Zero,
                    TimeSpan.FromSeconds(60));

                return Task.CompletedTask;
            }
            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            PointF onlineTextLocation = new PointF(_onlineTextLocationList.First(), _onlineTextLocationList.Last());
            PointF pingTextLocation = new PointF(_pingTextLocationList.First(), _pingTextLocationList.Last());

            PointF onlineLocation = new PointF(_onlineLocationList.First(), _onlineLocationList.Last());
            PointF pingLocation = new PointF(_pingLocationList.First(), _pingLocationList.Last());

            int online = new ClientListCommand().Execute(_client).Values.Where(x => x.ClientType == 0).Count();
            int ping = Convert.ToInt32(new ServerInfoCommand().Execute(_client).TotalPing);

            string imageFilePath = @"Banner/source.jpg";
            Bitmap bitmap = (Bitmap)Image.FromFile(imageFilePath);

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                using (Font arialFont = new Font("Arial", 30))
                {
                    graphics.DrawString("ONLINE", arialFont, Brushes.White, onlineTextLocation);
                    graphics.DrawString("PING", arialFont, Brushes.White, pingTextLocation);

                    if (online < 10)
                    {
                        graphics.DrawString(online.ToString(), arialFont, Brushes.White, new PointF(onlineLocation.X, onlineLocation.Y));
                    }

                    if (online >= 10 && online <= 100)
                    {
                        graphics.DrawString(online.ToString(), arialFont, Brushes.White, new PointF(onlineLocation.X - 10f, onlineLocation.Y));
                    }

                    if (online > 100)
                    {
                        graphics.DrawString(online.ToString(), arialFont, Brushes.White, new PointF(onlineLocation.X - 20f, onlineLocation.Y));
                    }

                    if (ping < 10)
                    {
                        graphics.DrawString(ping.ToString(), arialFont, Brushes.White, new PointF(pingLocation.X, pingLocation.Y));
                    }

                    if (ping >= 10 && ping <= 100)
                    {
                        graphics.DrawString(ping.ToString(), arialFont, Brushes.White, new PointF(pingLocation.X - 10f, pingLocation.Y));
                    }

                    if (ping > 100)
                    {
                        graphics.DrawString(ping.ToString(), arialFont, Brushes.White, new PointF(pingLocation.X - 20f, pingLocation.Y));
                    }
                }
            }
            bool exists = System.IO.Directory.Exists("Banner");
            if (!exists)
                System.IO.Directory.CreateDirectory("Banner");
            bitmap.Save("Banner/result.jpg");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Banner Background Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }



    }
}
