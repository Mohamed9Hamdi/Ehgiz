using System.ComponentModel.DataAnnotations;

namespace Ehgiz.Application.DTOs.Admin;

public class UpdatePlatformFeeRequest
{
    [Range(0, 100)]
    public decimal FeePercent { get; set; }
}
