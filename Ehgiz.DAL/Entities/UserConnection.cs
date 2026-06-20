namespace Ehgiz.DAL.Entities;

public class UserConnection
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string ConnectionId { get; set; } = null!;
    public DateTime ConnectedAt { get; set; }
    public bool IsOnline { get; set; }
    public ApplicationUser User { get; set; } = null!;
}