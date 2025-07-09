using DataIngestionApi.Models;
using DataIngestionApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataIngestionApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataController : ControllerBase
{
    private readonly KafkaProducerService _producer;
    private readonly ILogger<DataController> _logger;

    public DataController(KafkaProducerService producer, ILogger<DataController> logger)
    {
        _producer = producer;
        _logger = logger;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Post([FromBody] SensorDataDto dto, CancellationToken ct)
    {
        if (dto.Timestamp == default)
        {
            return BadRequest("Timestamp is required");
        }

        try
        {
            await _producer.ProduceAsync(dto, ct);
            _logger.LogInformation("Produced sensor data for device {DeviceId}", dto.DeviceId);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to produce data to Kafka");
            return StatusCode(500, "Failed to queue data");
        }
    }
} 