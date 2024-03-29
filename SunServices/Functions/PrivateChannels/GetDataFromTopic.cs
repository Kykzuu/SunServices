﻿using SunServices.Helpers;
using System;
using System.Linq;

namespace SunServices.Functions.PrivateChannels
{
    public class GetDataFromTopic
    {

        public string UniqueId(string topic)
        {
            if(topic != null)
            {
                string encode = Base64Helper.Decode(topic);
                string[] UniqueId = encode.Split("|+");
                return UniqueId.First();
            }
            return "";
        }

        public DateTimeOffset Time(string topic)
        {
            if(topic != null)
            {
                string encode = Base64Helper.Decode(topic);
                string[] UniqueId = encode.Split("|+");
                try
                {
                    return DateTimeOffset.FromUnixTimeSeconds(long.Parse(UniqueId[1]));
                }
                catch (Exception)
                {
                    return DateTime.Now;
                }
            }
            return DateTime.Now;
        }

        public DateTimeOffset CreatedDate(string topic)
        {
            if (topic != null)
            {
                string encode = Base64Helper.Decode(topic);
                string[] UniqueId = encode.Split("|+");
                try
                {
                    return DateTimeOffset.FromUnixTimeSeconds(long.Parse(UniqueId[2]));
                }
                catch (Exception)
                {
                    return DateTime.Now;
                }
            }
            return DateTime.Now;
        }
    }
}
