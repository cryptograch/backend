using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Taxi.RedisEntities;

namespace Taxi.Models.Chat
{
    public class ChannelDto
    {
        public string Id { get; set; }

        public List<ChatUserDto> Members { get; set; }

        public int NumUnread { get; set; }
   
        public DateTime LastUpdate { get; set; }
    }
}
