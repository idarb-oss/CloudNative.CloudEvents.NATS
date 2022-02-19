using System.Text.Json.Serialization;

namespace CloudNative.CloudEvents.NATS.UnitTests;

public record TestData()
{
    [JsonPropertyName("specversion")]
    public string SpecVersion { get; init; }
    
    [JsonPropertyName("id")]
    public string Id { get; init; }
    
    [JsonPropertyName("type")]
    public string Type { get; init; }
    
    [JsonPropertyName("source")]
    public string Source { get; init; }

    [JsonPropertyName("data")]
    public Data Data { get; init; }
}

public record Data
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; init; }
    
    [JsonPropertyName("total")]
    public double Total { get; init; }
    
    [JsonPropertyName("items")]
    public Items[] Items { get; init; }
}

public record Items
{
    [JsonPropertyName("sku")]
    public string Sku { get; init; }
    
    [JsonPropertyName("price")]
    public double Price { get; init; }
}