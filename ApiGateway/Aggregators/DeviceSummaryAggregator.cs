using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using Ocelot.Middleware;
using Ocelot.Multiplexer;

namespace ApiGateway.Aggregators;

/// <summary>
/// Собирает единый JSON с сырыми измерениями и агрегатами по устройству.
/// </summary>
public class DeviceSummaryAggregator : IDefinedAggregator
{
    public async Task<DownstreamResponse> Aggregate(List<HttpContext> responses)
    {
        // Предполагаем, что порядок RouteKeys соответствует порядку ответов
        // 0 -> /devices/{id}/data, 1 -> /devices/{id}/aggregates
        var rawDataJson = await responses[0].Items.DownstreamResponse().Content.ReadAsStringAsync();
        var aggregatesJson = await responses[1].Items.DownstreamResponse().Content.ReadAsStringAsync();

        var merged = new JObject
        {
            ["data"] = TryParse(rawDataJson),
            ["aggregates"] = TryParse(aggregatesJson)
        };

        var stringContent = new StringContent(merged.ToString(), Encoding.UTF8, "application/json");

        // Собираем все downstream-заголовки
        var headers = responses.SelectMany(r => r.Items.DownstreamResponse().Headers).ToList();
        return new DownstreamResponse(stringContent, HttpStatusCode.OK, headers, "OK");
    }

    private static JToken TryParse(string json)
    {
        try
        {
            return JToken.Parse(json);
        }
        catch
        {
            return JValue.CreateString(json);
        }
    }
} 