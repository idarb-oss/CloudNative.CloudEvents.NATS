using System;
using System.IO;
using NATS.Client;
using Xunit;
using System.Text.Json;

namespace CloudNative.CloudEvents.NATS.UnitTests;

public class TestFixture : IClassFixture<TestFixture>
{
    private readonly string _jsonData;
    private readonly TestData _jsonObj;

    public TestFixture()
    {
        _jsonData = File.ReadAllText("test-data.json");
        _jsonObj = JsonSerializer.Deserialize<TestData>(_jsonData);
    }

    public CloudEvent CreateCloudEvent()
    {
        return new CloudEvent(CloudEventsSpecVersion.V1_0)
        {
            Id = "id",
            Source = new Uri("https://ce"),
            Subject = "subject",
            Time = DateTimeOffset.UtcNow,
            Type = "store.order",
            Data = _jsonObj
        };
    }

    public Msg CreateNatsMessage()
    {
        var headers = new MsgHeader();
        headers.Add("ce-id", "id");
        headers.Add("ce-source", "https://ce");
        headers.Add("ce-subject", "subject");
        headers.Add("ce-time", DateTimeOffset.UtcNow.ToString());
        headers.Add("ce-type", "store.order");
        headers.Add("ce-specversion", "1.0");

        var data = JsonSerializer.SerializeToUtf8Bytes<TestData>(_jsonObj);
        return new Msg("subject", headers, data);
    }
}