using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using System.Collections.Generic;

var gatewayUrl = Environment.GetEnvironmentVariable("GATEWAY") ?? "http://gateway";

var client = new HttpClient();
var token = Environment.GetEnvironmentVariable("TOKEN");

if (string.IsNullOrWhiteSpace(token))
{
    var identityUrl = Environment.GetEnvironmentVariable("IDENTITY_URL") ?? "http://identity";
    var clientId     = Environment.GetEnvironmentVariable("CLIENT_ID")     ?? "service-client";
    var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET") ?? "secret";

    var form = new Dictionary<string, string>
    {
        ["client_id"]     = clientId,
        ["client_secret"] = clientSecret,
        ["grant_type"]    = "client_credentials",
        ["scope"]         = "api"
    };

    using var idClient = new HttpClient();
    var resp = await idClient.PostAsync(
        $"{identityUrl}/connect/token",
        new FormUrlEncodedContent(form));
    resp.EnsureSuccessStatusCode();

    var payload = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
    token = payload.RootElement.GetProperty("access_token").GetString();
    Console.WriteLine($"Obtained new token from {identityUrl}");
}

client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!);

var random = new Random();

while (true)
{
    var data = new
    {
        deviceId = 1,
        temperature = random.Next(15, 30),
        humidity = random.Next(30, 60),
        timestamp = DateTime.UtcNow
    };

    var json = JsonSerializer.Serialize(data);
    try
    {
        var resp = await client.PostAsync($"{gatewayUrl}/api/data", new StringContent(json, Encoding.UTF8, "application/json"));
        Console.WriteLine($"[{DateTime.Now:T}] Sent {json} -> {(int)resp.StatusCode}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }

    await Task.Delay(5000);
} 