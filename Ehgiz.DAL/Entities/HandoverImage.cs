namespace Ehgiz.DAL.Entities;

public class HandoverImage
{
    public int Id { get; set; }
    public int HandoverId { get; set; }
    public string ImageUrl { get; set; } = null!;
    public string PublicId { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public DateTime UploadedAt { get; set; }

    public Handover Handover { get; set; } = null!;
}
