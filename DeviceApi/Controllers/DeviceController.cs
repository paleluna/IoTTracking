using DeviceApi.Repositories;
using DeviceApi.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace DeviceApi.Controllers;

[ApiController]
[Route("[controller]")]
public class DevicesController : ControllerBase
{
    private readonly DeviceRepository _repo;
    public DevicesController(DeviceRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> Get() => Ok(await _repo.GetDevicesAsync());

    [HttpGet("{id}/data")]
    public async Task<IActionResult> GetData(int id, [FromQuery] string? month)
    {
        if (!string.IsNullOrWhiteSpace(month))
        {
            if (DateTime.TryParseExact(month, "yyyy-MM", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var monthStart))
            {
                return Ok(await _repo.GetDeviceDataForMonthAsync(id, monthStart));
            }
            return BadRequest("Invalid month format. Use YYYY-MM.");
        }

        return Ok(await _repo.GetDeviceDataAsync(id));
    }

    // ───────────────────── analytics ─────────────────────

    [HttpGet("{id}/aggregates")]
    public async Task<IActionResult> GetAggregates(int id, [FromQuery] string period = "hour")
        => Ok(await _repo.GetAggregatesAsync(id, period));

    [HttpGet("{id}/trend")]
    public async Task<IActionResult> GetTrend(
        int id,
        [FromQuery] string metric = "temperature",
        [FromQuery] string bucket = "5 minutes",
        [FromQuery] string period = "day")
        => Ok(await _repo.GetTrendAsync(id, metric, bucket, period));

    [HttpGet("{id}/anomalies")]
    public async Task<IActionResult> GetAnomalies(
        int id,
        [FromQuery] string metric = "temperature",
        [FromQuery] string period = "hour",
        [FromQuery] double threshold = 2.0)
        => Ok(await _repo.GetAnomaliesAsync(id, metric, period, threshold));
} 