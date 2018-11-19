using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Taxi.RedisEntities
{
    public class UserMessage
    {
        public DateTime PublicationTime { get; set; }
        public string Message { get; set; }
        public string UserId { get; set; }
    }

    public class UserMessageForChannel : UserMessage
    {
        public string Channel { get; set; }
    }
}
