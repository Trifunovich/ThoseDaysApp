namespace Api.Models;

public class Cycle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public required DateTime StartDate { get; set; }
    public int DurationDays { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool Corrected { get; set; } = false;

    public User? User { get; set; }
}
