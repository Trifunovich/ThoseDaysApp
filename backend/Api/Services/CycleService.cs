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
    Task<(double averageCycleLength, double averageInterval)> GetStatsAsync(Guid userId);
    Task<List<Prediction>> GeneratePredictionsAsync(Guid userId, int numCycles);
}

public class CycleService(AppDbContext context) : ICycleService
{
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
            StartDate = startDate,
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

        cycle.StartDate = startDate;
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

    public async Task<(double averageCycleLength, double averageInterval)> GetStatsAsync(Guid userId)
    {
        var cycles = await GetUserCyclesAsync(userId);

        if (cycles.Count == 0)
            return (0, 0);

        var cycleLengths = cycles.Select(c => c.DurationDays).ToList();
        var averageCycleLength = cycleLengths.Average();

        if (cycles.Count < 2)
            return (averageCycleLength, 28);

        var intervals = new List<int>();
        for (int i = 1; i < cycles.Count; i++)
        {
            var interval = (cycles[i].StartDate.Date - cycles[i - 1].StartDate.Date).Days;
            intervals.Add(interval);
        }

        var averageInterval = intervals.Count > 0 ? intervals.Average() : 28;

        return (averageCycleLength, averageInterval);
    }

    public async Task<List<Prediction>> GeneratePredictionsAsync(Guid userId, int numCycles)
    {
        var cycles = await GetUserCyclesAsync(userId);
        var predictions = new List<Prediction>();

        if (cycles.Count == 0)
            return predictions;

        var (_, averageInterval) = await GetStatsAsync(userId);
        if (averageInterval == 0)
            averageInterval = 28;

        var lastCycle = cycles.Last();
        var nextStartDate = lastCycle.StartDate.AddDays((int)Math.Round(averageInterval));

        for (int i = 0; i < numCycles; i++)
        {
            var prediction = new Prediction
            {
                UserId = userId,
                PredictedStart = nextStartDate,
                PredictedDuration = (int)Math.Round((double)lastCycle.DurationDays),
                Confidence = 0.85f
            };

            predictions.Add(prediction);
            nextStartDate = nextStartDate.AddDays((int)Math.Round((double)averageInterval));
        }

        var existingPredictions = await context.Predictions
            .Where(p => p.UserId == userId)
            .ToListAsync();

        context.Predictions.RemoveRange(existingPredictions);

        context.Predictions.AddRange(predictions);
        await context.SaveChangesAsync();

        return predictions;
    }
}
