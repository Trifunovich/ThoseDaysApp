using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public interface ICycleService
{
    Task<List<Cycle>> GetUserCyclesAsync(Guid userId);
    Task<Cycle> AddCycleAsync(Guid userId, DateTime startDate, int durationDays);
    Task<Cycle?> UpdateCycleAsync(Guid userId, Guid cycleId, DateTime startDate, int durationDays);
    Task<bool> DeleteCycleAsync(Guid userId, Guid cycleId);
    Task<(double averageCycleLength, double averageInterval, int totalPeriods)> GetStatsAsync(Guid userId);
    Task<List<Prediction>> GeneratePredictionsAsync(Guid userId, int numCycles);
}

public class CycleService(AppDbContext context) : ICycleService
{
    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    public async Task<List<Cycle>> GetUserCyclesAsync(Guid userId)
    {
        return await context.Cycles
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.StartDate)
            .ToListAsync();
    }

    public async Task<Cycle> AddCycleAsync(Guid userId, DateTime startDate, int durationDays)
    {
        var cycle = new Cycle
        {
            UserId = userId,
            StartDate = ToUtc(startDate),
            DurationDays = durationDays
        };

        context.Cycles.Add(cycle);
        await context.SaveChangesAsync();

        await GeneratePredictionsAsync(userId, 15);

        return cycle;
    }

    public async Task<Cycle?> UpdateCycleAsync(Guid userId, Guid cycleId, DateTime startDate, int durationDays)
    {
        var cycle = await context.Cycles.FirstOrDefaultAsync(c => c.Id == cycleId && c.UserId == userId);
        if (cycle == null)
            return null;

        cycle.StartDate = ToUtc(startDate);
        cycle.DurationDays = durationDays;
        cycle.Corrected = true;

        await context.SaveChangesAsync();

        var oldPredictions = await context.Predictions
            .Where(p => p.UserId == userId)
            .ToListAsync();

        context.Predictions.RemoveRange(oldPredictions);
        await context.SaveChangesAsync();

        await GeneratePredictionsAsync(userId, 15);

        return cycle;
    }

    public async Task<bool> DeleteCycleAsync(Guid userId, Guid cycleId)
    {
        var cycle = await context.Cycles.FirstOrDefaultAsync(c => c.Id == cycleId && c.UserId == userId);
        if (cycle == null)
            return false;

        context.Cycles.Remove(cycle);
        await context.SaveChangesAsync();

        var oldPredictions = await context.Predictions
            .Where(p => p.UserId == userId)
            .ToListAsync();

        context.Predictions.RemoveRange(oldPredictions);
        await context.SaveChangesAsync();

        await GeneratePredictionsAsync(userId, 15);

        return true;
    }

    public async Task<(double averageCycleLength, double averageInterval, int totalPeriods)> GetStatsAsync(Guid userId)
    {
        var periods = await GetPeriodsAsync(userId);

        if (periods.Count == 0)
            return (0, 0, 0);

        var averageCycleLength = periods.Average(p => p.Length);

        if (periods.Count < 2)
            return (averageCycleLength, 28, periods.Count);

        var intervals = new List<int>();
        for (int i = 1; i < periods.Count; i++)
        {
            intervals.Add((periods[i].Start - periods[i - 1].Start).Days);
        }

        return (averageCycleLength, intervals.Average(), periods.Count);
    }

    public async Task<List<Prediction>> GeneratePredictionsAsync(Guid userId, int numCycles)
    {
        var periods = await GetPeriodsAsync(userId);
        var predictions = new List<Prediction>();

        var existingPredictions = await context.Predictions
            .Where(p => p.UserId == userId)
            .ToListAsync();
        context.Predictions.RemoveRange(existingPredictions);

        if (periods.Count == 0)
        {
            await context.SaveChangesAsync();
            return predictions;
        }

        var (avgLength, avgInterval, _) = await GetStatsAsync(userId);
        if (avgInterval <= 0) avgInterval = 28;

        var predictedDuration = avgLength > 0 ? (int)Math.Round(avgLength) : 5;
        if (predictedDuration < 1) predictedDuration = 1;

        var lastPeriod = periods.Last();
        var nextStartDate = lastPeriod.Start.AddDays((int)Math.Round(avgInterval));

        for (int i = 0; i < numCycles; i++)
        {
            predictions.Add(new Prediction
            {
                UserId = userId,
                PredictedStart = nextStartDate,
                PredictedDuration = predictedDuration,
                Confidence = 0.85f
            });
            nextStartDate = nextStartDate.AddDays((int)Math.Round(avgInterval));
        }

        context.Predictions.AddRange(predictions);
        await context.SaveChangesAsync();

        return predictions;
    }

    private async Task<List<(DateTime Start, int Length)>> GetPeriodsAsync(Guid userId)
    {
        var cycles = await GetUserCyclesAsync(userId);

        var days = new SortedSet<DateTime>();
        foreach (var c in cycles)
        {
            for (int i = 0; i < c.DurationDays; i++)
                days.Add(c.StartDate.Date.AddDays(i));
        }

        var periods = new List<(DateTime Start, int Length)>();
        DateTime? runStart = null;
        DateTime? prev = null;
        int length = 0;

        foreach (var day in days)
        {
            if (runStart is null)
            {
                runStart = day;
                length = 1;
            }
            else if (day == prev!.Value.AddDays(1))
            {
                length++;
            }
            else
            {
                periods.Add((runStart.Value, length));
                runStart = day;
                length = 1;
            }
            prev = day;
        }

        if (runStart is not null)
            periods.Add((runStart.Value, length));

        return periods;
    }
}
