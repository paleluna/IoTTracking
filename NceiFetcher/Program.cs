using System.Net.Http.Headers;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

var gatewayUrl = Environment.GetEnvironmentVariable("GATEWAY_URL") ?? "http://gateway";
var identityUrl = Environment.GetEnvironmentVariable("IDENTITY_URL") ?? "http://identity";
var clientId     = Environment.GetEnvironmentVariable("CLIENT_ID")     ?? "service-client";
var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET") ?? "service-secret";

var nceiToken    = Environment.GetEnvironmentVariable("NCEI_TOKEN") ?? throw new InvalidOperationException("NCEI_TOKEN env var required");
var datasetId    = Environment.GetEnvironmentVariable("NCEI_DATASET_ID") ?? "GHCND";
var datatypeId   = Environment.GetEnvironmentVariable("NCEI_DATATYPE_ID") ?? "TMAX";
var stationId    = Environment.GetEnvironmentVariable("NCEI_STATION_ID") ?? throw new InvalidOperationException("NCEI_STATION_ID env var required");
var fetchSeconds = int.TryParse(Environment.GetEnvironmentVariable("FETCH_INTERVAL"), out var s) ? s : 3600;
var lookbackDays = int.TryParse(Environment.GetEnvironmentVariable("LOOKBACK_DAYS"), out var d) ? d : 30;

var deviceIdEnv = Environment.GetEnvironmentVariable("DEVICE_ID");
int deviceId = !string.IsNullOrWhiteSpace(deviceIdEnv) && int.TryParse(deviceIdEnv, out var parsed)
    ? parsed
    : 999;

using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

Console.WriteLine($"Starting NCEI fetcher for station {stationId}");
Console.WriteLine($"Dataset: {datasetId}, DataType: {datatypeId}");
Console.WriteLine($"Device ID: {deviceId}, Lookback: {lookbackDays} days");

async Task<string> GetJwtAsync()
{
    var form = new Dictionary<string,string>{
        ["client_id"] = clientId,
        ["client_secret"] = clientSecret,
        ["grant_type"] = "client_credentials",
        ["scope"] = "api"
    };
    
    using var resp = await http.PostAsync(
        $"{identityUrl}/connect/token", 
        new FormUrlEncodedContent(form)
    );
    resp.EnsureSuccessStatusCode();
    
    var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
    return json.RootElement.GetProperty("access_token").GetString()!;
}

var processedDates = new HashSet<string>(); // Avoid duplicates across restarts

while (true)
{
    try
    {
        // Refresh token periodically
        var token = await GetJwtAsync();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var now = DateTime.UtcNow;
        var endDate = now;
        var startDate = endDate.AddDays(-lookbackDays);

        var start = startDate.ToString("yyyy-MM-dd");
        var end = endDate.ToString("yyyy-MM-dd");

        Console.WriteLine($"\nFetching NCEI data for {start} – {end}");

        using var ncei = new HttpClient();
        ncei.DefaultRequestHeaders.Add("token", nceiToken);
        
        var url = $"https://www.ncei.noaa.gov/cdo-web/api/v2/data?" +
                 $"datasetid={datasetId}&datatypeid={datatypeId}" +
                 $"&stationid={stationId}&units=metric" +
                 $"&startdate={start}&enddate={end}&limit=1000";

        using var resp = await ncei.GetAsync(url);
        
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($"NCEI error {(int)resp.StatusCode}: {body}");
            // For 429 (Too Many Requests), wait longer
            if ((int)resp.StatusCode == 429)
            {
                await Task.Delay(TimeSpan.FromMinutes(5));
                continue;
            }
        }
        else
        {
            var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            if (doc.RootElement.TryGetProperty("results", out var results))
            {
                var points = new List<JsonElement>();
                foreach (var result in results.EnumerateArray())
                {
                    points.Add(result);
                }
                
                // Sort chronologically
                points.Sort((a, b) => string.Compare(
                    a.GetProperty("date").GetString(),
                    b.GetProperty("date").GetString()
                ));

                var newPoints = 0;
                foreach (var point in points)
                {
                    var ts = point.GetProperty("date").GetString();
                    if (processedDates.Contains(ts))
                    {
                        continue;
                    }

                    var value = point.GetProperty("value").GetDouble() / 10.0;
                    
                    var payload = new {
                        deviceId = deviceId,
                        temperature = Math.Round(value, 1),
                        humidity = 50,
                        timestamp = ts
                    };

                    var jsonPayload = JsonSerializer.Serialize(payload);
                    var content = new StringContent(
                        jsonPayload, 
                        Encoding.UTF8, 
                        "application/json"
                    );

                    try
                    {
                        using var res = await http.PostAsync(
                            $"{gatewayUrl}/api/data", 
                            content
                        );
                        
                        if (res.IsSuccessStatusCode)
                        {
                            processedDates.Add(ts);
                            newPoints++;
                            Console.WriteLine($"Sent {ts} {value}°C");
                        }
                        else
                        {
                            Console.WriteLine($"Error sending {ts}: {(int)res.StatusCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending data: {ex.Message}");
                    }
                }

                // Trim old dates from memory
                var oldestToKeep = DateTime.UtcNow.AddDays(-lookbackDays)
                    .ToString("yyyy-MM-dd");
                processedDates.RemoveWhere(d => string.Compare(d, oldestToKeep) < 0);

                Console.WriteLine($"Processed {points.Count} records, sent {newPoints} new points");
                Console.WriteLine($"Tracking {processedDates.Count} dates in memory");
            }
            else if (doc.RootElement.TryGetProperty("metadata", out var meta) &&
                     meta.TryGetProperty("resultset", out var rs))
            {
                var cnt = rs.GetProperty("count").GetInt32();
                Console.WriteLine($"No data: resultset.count={cnt}");
            }
            else
            {
                Console.WriteLine("No data returned for given period");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }

    await Task.Delay(fetchSeconds * 1000);
}