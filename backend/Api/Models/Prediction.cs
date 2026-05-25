namespace Api.Models;

public class Prediction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public required DateTime PredictedStart { get; set; }
    public int PredictedDuration { get; set; }
    public float Confidence { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
