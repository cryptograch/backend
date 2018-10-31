using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Taxi.Entities
{
    public class DriverComment
    {
        public Guid Id { get; set; }

        public DateTime CreationTime { get; set; }

        public string Message { get; set; }

        public Guid DriverId { get; set; }

        public Driver Driver { get; set; }

        public Guid CustomerId { get; set; }
    }
}
