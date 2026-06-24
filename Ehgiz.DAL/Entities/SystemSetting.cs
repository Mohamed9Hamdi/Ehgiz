using System.ComponentModel.DataAnnotations;

namespace Ehgiz.DAL.Entities;

public class SystemSetting
{
    [Key]
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
}
