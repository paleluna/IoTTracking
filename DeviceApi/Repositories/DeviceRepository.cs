using Dapper;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DeviceApi.Models;
using Microsoft.Extensions.Logging;

namespace DeviceApi.Repositories;

public class DeviceRepository
{
    private readonly string _connStr;
    private readonly ILogger<DeviceRepository> _logger;

    public DeviceRepository(string connStr, ILogger<DeviceRepository> logger)
    {
        _connStr = connStr ?? throw new ArgumentNullException(nameof(connStr));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<DeviceDto>> GetDevicesAsync()
    {
        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.QueryAsync<DeviceDto>(@"
            SELECT DISTINCT
                device_id as Id,
                CASE 
                    WHEN device_id = 999 THEN 'NCEI-Device' 
                    ELSE CONCAT('Device ', device_id) 
                END as Name
            FROM sensor_data
            ORDER BY device_id;");
    }

    public async Task<IEnumerable<SensorDataDto>> GetDeviceDataAsync(int deviceId, int limit = 100)
    {
        if (deviceId <= 0) throw new ArgumentException("Invalid deviceId", nameof(deviceId));
        if (limit <= 0) throw new ArgumentException("Invalid limit", nameof(limit));

        await using var conn = new NpgsqlConnection(_connStr);
        try
        {
            return await conn.QueryAsync<SensorDataDto>(@"
                WITH latest_data AS (
                    SELECT 
                        timestamp::timestamptz AT TIME ZONE 'UTC' as timestamp,
                        temperature
                    FROM sensor_data 
                    WHERE device_id = @id
                    ORDER BY timestamp DESC 
                    LIMIT @lim
                )
                SELECT * FROM latest_data
                ORDER BY timestamp ASC;", // Reverse for chart
                new { id = deviceId, lim = limit });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device data for device {DeviceId}", deviceId);
            throw;
        }
    }

    public async Task<IEnumerable<SensorDataDto>> GetDeviceDataForMonthAsync(int deviceId, DateTime monthStart)
    {
        if (deviceId <= 0) throw new ArgumentException("Invalid deviceId", nameof(deviceId));
        
        var next = monthStart.AddMonths(1);
        await using var conn = new NpgsqlConnection(_connStr);
        try
        {
            return await conn.QueryAsync<SensorDataDto>(@"
                SELECT 
                    timestamp::timestamptz AT TIME ZONE 'UTC' as timestamp,
                    temperature
                FROM sensor_data
                WHERE device_id = @id
                    AND timestamp >= @start::timestamptz
                    AND timestamp < @end::timestamptz
                ORDER BY timestamp ASC;",
                new { id = deviceId, start = monthStart, end = next });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error getting month data for device {DeviceId}, month {Month}", 
                deviceId, monthStart.ToString("yyyy-MM"));
            throw;
        }
    }

    // ──────────────────────── Analytics ────────────────────────

    public async Task<AggregatesDto> GetAggregatesAsync(int deviceId, string period)
    {
        var interval = MapPeriodToInterval(period);
        await using var conn = new NpgsqlConnection(_connStr);
        
        try
        {
            return await conn.QuerySingleAsync<AggregatesDto>(@"
                SELECT 
                    COALESCE(MIN(temperature), 0) AS min,
                    COALESCE(MAX(temperature), 0) AS max,
                    COALESCE(AVG(temperature), 0) AS avg
                FROM sensor_data
                WHERE device_id = @id 
                    AND timestamp >= NOW() - INTERVAL @interval;",
                new { id = deviceId, interval });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error getting aggregates for device {DeviceId}, period {Period}", 
                deviceId, period);
            throw;
        }
    }

    public async Task<IEnumerable<TrendPointDto>> GetTrendAsync(
        int deviceId, string metric, string bucket, string period)
    {
        var column = metric?.ToLower() == "humidity" ? "humidity" : "temperature";
        var interval = MapPeriodToInterval(period);
        var safeBucket = ValidateBucket(bucket);

        await using var conn = new NpgsqlConnection(_connStr);
        try
        {
            return await conn.QueryAsync<TrendPointDto>(@"
                SELECT 
                    time_bucket(@bucket, timestamp) AS Bucket,
                    AVG(@column) AS Value
                FROM sensor_data
                WHERE device_id = @id 
                    AND timestamp >= NOW() - INTERVAL @interval
                GROUP BY Bucket
                ORDER BY Bucket;",
                new { 
                    id = deviceId, 
                    column = new Npgsql.NpgsqlParameter(column, NpgsqlTypes.NpgsqlDbType.Text),
                    bucket = safeBucket,
                    interval 
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error getting trend for device {DeviceId}, metric {Metric}, period {Period}", 
                deviceId, metric, period);
            throw;
        }
    }

    public async Task<IEnumerable<AnomalyDto>> GetAnomaliesAsync(int deviceId, string metric, string period, double threshold)
    {
        var column = metric?.ToLower() == "humidity" ? "humidity" : "temperature";
        var interval = MapPeriodToInterval(period);
        await using var conn = new NpgsqlConnection(_connStr);

        try
        {
            var sql = $@"WITH stats AS (
                            SELECT AVG({column}) AS avg, STDDEV_POP({column}) AS std
                            FROM sensor_data
                            WHERE device_id = @id AND timestamp >= NOW() - INTERVAL '{interval}'
                         )
                         SELECT timestamp AT TIME ZONE 'UTC' AS Timestamp, 
                                {column} AS Value
                         FROM sensor_data, stats
                         WHERE device_id = @id
                           AND timestamp >= NOW() - INTERVAL '{interval}'
                           AND ABS({column} - stats.avg) > @thres * stats.std
                         ORDER BY Timestamp DESC
                         LIMIT 100";

            return await conn.QueryAsync<AnomalyDto>(sql, new { id = deviceId, thres = threshold });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error getting anomalies for device {DeviceId}, metric {Metric}, period {Period}, threshold {Threshold}", 
                deviceId, metric, period, threshold);
            throw;
        }
    }

    private static string MapPeriodToInterval(string period) => period?.ToLower() switch
    {
        "day" or "1d" => "1 day",
        "week" or "1w" => "7 days",
        "month" or "30d" => "30 days",
        _ => "1 hour"
    };

    private static string ValidateBucket(string bucket)
    {
        var safeBucket = bucket?.Trim().ToLower() ?? "5 minutes";
        var allowedBuckets = new[] { 
            "1 minute", "5 minutes", "15 minutes", "30 minutes",
            "1 hour", "3 hours", "6 hours", "12 hours", "1 day"
        };
        return Array.IndexOf(allowedBuckets, safeBucket) >= 0 
            ? safeBucket 
            : "5 minutes";
    }
}