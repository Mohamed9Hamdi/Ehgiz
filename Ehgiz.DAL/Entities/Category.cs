namespace Ehgiz.DAL.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }

    public ICollection<Tool> Tools { get; set; } = [];
}
