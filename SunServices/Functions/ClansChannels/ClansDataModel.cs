using System;
using System.Collections.Generic;
using System.Text;

namespace SunServices.Functions.ClansChannels
{
    public class ClansDataModel
    {
        public OwnerDataModel Owner { get; set; }
        public uint GroupID { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public List<ActivityTimeDataModel> ActivityTime { get; set; }
    }

    public class OwnerDataModel
    {
        public uint DatabaseID { get; set; }
        public string UniqueID { get; set; }
    }

    public class ActivityTimeDataModel
    {
        public DateTimeOffset Date { get; set; }
        public long Time { get; set; }
    }
}
