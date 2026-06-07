namespace Ehgiz.DAL.Entities;

public class Conversation
{
    public int Id { get; set; }
    public int User1Id { get; set; }
    public int User2Id { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User1 { get; set; } = null!;
    public User User2 { get; set; } = null!;
    public ICollection<Message> Messages { get; set; } = [];
}
