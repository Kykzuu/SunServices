using System;
using System.Collections.Generic;
using System.Text;

namespace SunServices.Functions.ServerStatsToDB
{
    interface IServerStatsToDB
    {

        void PlayersOnline(int playersOnline, DateTimeOffset dateTime, string connString);

        void QueryClientsOnline(int queryClientsOnline, DateTimeOffset dateTime, string connString);

        void PrivateChannels(int privateChannels, DateTimeOffset dateTime, string connString);

        void PrivateChannelsUsers(int privateChannelsUsers, DateTimeOffset dateTime, string connString);

        void ClansChannels(int clansChannels, DateTimeOffset dateTime, string connString);

        void ClansChannelsUsers(int clansChannelsUsers, DateTimeOffset dateTime, string connString);

        void AdminsOnline(int adminsOnline, DateTimeOffset dateTime, string connString);

        void AveragePing(float averagePing, DateTimeOffset dateTime, string connString);

        void AveragePacketLoss(float averagePacketLoss, DateTimeOffset dateTime, string connString);

        void BandwidthSent(float bandwidthSent, DateTimeOffset dateTime, string connString);

        void BandwidthReceived (float bandwidthReceived, DateTimeOffset dateTime, string connString);

        void Channels(int channels, DateTimeOffset dateTime, string connString);
    }
}
