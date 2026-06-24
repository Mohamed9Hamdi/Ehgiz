namespace Ehgiz.DAL.Entities;

public class ToolImage
{
    public int Id { get; set; }
    public int ToolId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;

    public Tool Tool { get; set; } = null!;
}
