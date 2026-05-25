namespace Api.DTOs;

public class CreateCycleRequest
{
    public required DateTime StartDate { get; set; }
    public int DurationDays { get; set; }
}

public class UpdateCycleRequest
{
    public DateTime StartDate { get; set; }
    public int DurationDays { get; set; }
}

public class CycleResponse
{
    public Guid Id { get; set; }
    public DateTime StartDate { get; set; }
    public int DurationDays { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Corrected { get; set; }
}

public class PredictionResponse
{
    public Guid Id { get; set; }
    public DateTime PredictedStart { get; set; }
    public int PredictedDuration { get; set; }
    public float Confidence { get; set; }
}

public class StatsResponse
{
    public double AverageCycleLength { get; set; }
    public double AverageInterval { get; set; }
    public int TotalCycles { get; set; }
}

public class PredictionRequest
{
    public int Cycles { get; set; } = 15;
}
