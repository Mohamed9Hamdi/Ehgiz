using System;
using System.Collections.Generic;
using System.Text;

namespace Ehgiz.Application.DTOs.Tools
{
    public class ToolFilterDto
    {
        public int? CategoryId { get; set; }
        public string? Location { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool? IsAvailable { get; set; }
        public string? SearchTerm { get; set; }   // بيبحث في Name + Description

        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

}
