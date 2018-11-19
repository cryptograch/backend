using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Taxi.Models.Drivers
{
    public class DriverCommentCreationDto
    {
        [Required]
        public string Message { get; set; }
        [Required]
        public Guid DriverId { get; set; }
    }
}
