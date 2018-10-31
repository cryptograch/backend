using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Taxi.Models.Drivers
{
    public class DriverCommentDto
    {
        public Guid Id { get; set; }

        public DateTime CreationTime { get; set; }

        public string Message { get; set; }

        public Guid DriverId { get; set; }

        public Guid CustomerId { get; set; }
    }
}
