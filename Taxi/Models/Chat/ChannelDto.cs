using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Taxi.Models.Chat
{
    public class ChannelDto
    {
        public string Id { get; set; }

        public List<ChatUserDto> Members { get; set; }
    }
}
