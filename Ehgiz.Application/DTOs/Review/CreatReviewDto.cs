using System;
using System.Collections.Generic;
using System.Text;

namespace Ehgiz.Application.DTOs.Review
{
    public class CreateReviewDto
    {
        public int BookingId { get; set; }
        public int Rating { get; set; }           
        public string? Comment { get; set; }
    }
}
