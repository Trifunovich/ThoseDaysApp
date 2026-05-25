using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/user/{userId}")]
public class PredictionsController(ICycleService cycleService) : ControllerBase
{
    [HttpPost("predict")]
    public async Task<ActionResult<List<PredictionResponse>>> GeneratePredictions(Guid userId, [FromQuery] int cycles = 15)
    {
        var predictions = await cycleService.GeneratePredictionsAsync(userId, cycles);
        var responses = predictions.Select(p => new PredictionResponse
        {
            Id = p.Id,
            PredictedStart = p.PredictedStart,
            PredictedDuration = p.PredictedDuration,
            Confidence = p.Confidence
        }).ToList();

        return Ok(responses);
    }

    [HttpGet("stats")]
    public async Task<ActionResult<StatsResponse>> GetStats(Guid userId)
    {
        var (avgLength, avgInterval, totalPeriods) = await cycleService.GetStatsAsync(userId);

        var response = new StatsResponse
        {
            AverageCycleLength = avgLength,
            AverageInterval = avgInterval,
            TotalCycles = totalPeriods
        };

        return Ok(response);
    }

    [HttpPost("toggle")]
    public async Task<IActionResult> TogglePredictions(Guid userId)
    {
        return Ok(new { message = "Predictions toggled", enabled = true });
    }
}
