using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SunServices.Functions.ServerStatsToDB
{
    public class MySQL : IServerStatsToDB
    {

        private string _schema = "CREATE TABLE IF NOT EXISTS `PlayersOnline` ( `dateTime` INT NOT NULL , `playersOnline` INT NOT NULL ) ENGINE = InnoDB; " +
            "CREATE TABLE IF NOT EXISTS `QueryClientsOnline` ( `dateTime` INT NOT NULL , `queryClientsOnline` INT NOT NULL ) ENGINE = InnoDB; " +
            "CREATE TABLE IF NOT EXISTS `PrivateChannels` ( `dateTime` INT NOT NULL , `privateChannels` INT NOT NULL ) ENGINE = InnoDB; " +
            "CREATE TABLE IF NOT EXISTS `PrivateChannelsUsers` ( `dateTime` INT NOT NULL , `privateChannelsUsers` INT NOT NULL ) ENGINE = InnoDB; " +
            "CREATE TABLE IF NOT EXISTS `ClansChannels` ( `dateTime` INT NOT NULL , `clansChannels` INT NOT NULL ) ENGINE = InnoDB;" +
            "CREATE TABLE IF NOT EXISTS `ClansChannelsUsers` ( `dateTime` INT NOT NULL , `clansChannelsUsers` INT NOT NULL ) ENGINE = InnoDB; " +
            "CREATE TABLE IF NOT EXISTS `AdminsOnline` ( `dateTime` INT NOT NULL , `adminsOnline` INT NOT NULL ) ENGINE = InnoDB;" +
            "CREATE TABLE IF NOT EXISTS `Channels` ( `dateTime` INT NOT NULL , `channels` INT NOT NULL ) ENGINE = InnoDB;" +
            "CREATE TABLE IF NOT EXISTS `AveragePing` ( `dateTime` INT NOT NULL , `averagePing` FLOAT NOT NULL ) ENGINE = InnoDB;" +
            "CREATE TABLE IF NOT EXISTS `AveragePacketLoss` ( `dateTime` INT NOT NULL , `averagePacketLoss` FLOAT NOT NULL ) ENGINE = InnoDB;" +
            "CREATE TABLE IF NOT EXISTS `BandwidthSent` ( `dateTime` INT NOT NULL , `bandwidthSent` FLOAT NOT NULL ) ENGINE = InnoDB;" +
            "CREATE TABLE IF NOT EXISTS `BandwidthReceived` ( `dateTime` INT NOT NULL , `bandwidthReceived` FLOAT NOT NULL ) ENGINE = InnoDB;";

        private void CreateTablesIfNotExists(string schema, string connString)
        {
            using var conn = new MySqlConnection(connString);
            conn.Open();
            using var cmd = new MySqlCommand(schema);
            cmd.Connection = conn;
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        private async Task ExecuteNonQuery(string connString, MySqlCommand cmd)
        {
            CreateTablesIfNotExists(_schema, connString);
            using var conn = new MySqlConnection(connString);
            conn.Open();
            cmd.Connection = conn;
            cmd.ExecuteNonQuery();
            await conn.CloseAsync();
        }

        public async void AdminsOnline(int adminsOnline, DateTimeOffset dateTime, string connString)
        {
            await ExecuteNonQuery(connString, new MySqlCommand($"INSERT INTO `AdminsOnline` (`dateTime`, `adminsOnline`) VALUES " +
                $"('{dateTime.ToUnixTimeSeconds()}', '{adminsOnline}');"));
        }

        public async void AveragePacketLoss(float averagePacketLoss, DateTimeOffset dateTime, string connString)
        {
            await ExecuteNonQuery(connString, new MySqlCommand($"INSERT INTO `AveragePacketLoss` (`dateTime`, `averagePacketLoss`) VALUES " +
                $"('{dateTime.ToUnixTimeSeconds()}', '{averagePacketLoss}');"));
        }

        public async void AveragePing(float averagePing, DateTimeOffset dateTime, string connString)
        {
            await ExecuteNonQuery(connString, new MySqlCommand($"INSERT INTO `AveragePing` (`dateTime`, `averagePing`) VALUES " +
                $"('{dateTime.ToUnixTimeSeconds()}', '{averagePing}');"));
        }

        public async void BandwidthReceived(float bandwidthReceived, DateTimeOffset dateTime, string connString)
        {
            await ExecuteNonQuery(connString, new MySqlCommand($"INSERT INTO `BandwidthReceived` (`dateTime`, `bandwidthReceived`) VALUES " +
                $"('{dateTime.ToUnixTimeSeconds()}', '{bandwidthReceived}');"));
        }

        public async void BandwidthSent(float bandwidthSent, DateTimeOffset dateTime, string connString)
        {
            await ExecuteNonQuery(connString, new MySqlCommand($"INSERT INTO `BandwidthSent` (`dateTime`, `bandwidthSent`) VALUES " +
                $"('{dateTime.ToUnixTimeSeconds()}', '{bandwidthSent}');"));
        }

        public async void Channels(int channels, DateTimeOffset dateTime, string connString)
        {
            await ExecuteNonQuery(connString, new MySqlCommand($"INSERT INTO `Channels` (`dateTime`, `channels`) VALUES " +
                $"('{dateTime.ToUnixTimeSeconds()}', '{channels}');"));
        }

        public async void ClansChannels(int clansChannels, DateTimeOffset dateTime, string connString)
        {
            await ExecuteNonQuery(connString, new MySqlCommand($"INSERT INTO `ClansChannels` (`dateTime`, `clansChannels`) VALUES " +
                $"('{dateTime.ToUnixTimeSeconds()}', '{clansChannels}');"));
        }

        public async void ClansChannelsUsers(int clansChannelsUsers, DateTimeOffset dateTime, string connString)
        {
            await ExecuteNonQuery(connString, new MySqlCommand($"INSERT INTO `ClansChannelsUsers` (`dateTime`, `clansChannelsUsers`) VALUES " +
                $"('{dateTime.ToUnixTimeSeconds()}', '{clansChannelsUsers}');"));
        }

        public async void PlayersOnline(int playersOnline, DateTimeOffset dateTime, string connString)
        {
            await ExecuteNonQuery(connString, new MySqlCommand($"INSERT INTO `PlayersOnline` (`dateTime`, `playersOnline`) VALUES " +
                $"('{dateTime.ToUnixTimeSeconds()}', '{playersOnline}');"));
        }

        public async void PrivateChannels(int privateChannels, DateTimeOffset dateTime, string connString)
        {
            await ExecuteNonQuery(connString, new MySqlCommand($"INSERT INTO `PrivateChannels` (`dateTime`, `privateChannels`) VALUES " +
                $"('{dateTime.ToUnixTimeSeconds()}', '{privateChannels}');"));
        }

        public async void PrivateChannelsUsers(int privateChannelsUsers, DateTimeOffset dateTime, string connString)
        {
            await ExecuteNonQuery(connString, new MySqlCommand($"INSERT INTO `PrivateChannelsUsers` (`dateTime`, `privateChannelsUsers`) VALUES " +
                $"('{dateTime.ToUnixTimeSeconds()}', '{privateChannelsUsers}');"));
        }

        public async void QueryClientsOnline(int queryClientsOnline, DateTimeOffset dateTime, string connString)
        {
            await ExecuteNonQuery(connString, new MySqlCommand($"INSERT INTO `QueryClientsOnline` (`dateTime`, `queryClientsOnline`) VALUES " +
                $"('{dateTime.ToUnixTimeSeconds()}', '{queryClientsOnline}');"));
        }
    }
}
