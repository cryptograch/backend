using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Taxi.RedisEntities
{
    public class UnreadMessages
    {
        public string ChannelId { get; set; }

        public DateTime LastUpDateTime { get; set; }

        public int NumberOfUnread { get; set; }
    }
}
