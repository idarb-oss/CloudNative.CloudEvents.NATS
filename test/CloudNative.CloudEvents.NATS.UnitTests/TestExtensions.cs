using Xunit;
using CloudNative.CloudEvents.SystemTextJson;
using FluentAssertions;
using FluentAssertions.Common;

namespace CloudNative.CloudEvents.NATS.UnitTests;

public class TestExtensions : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;

    public TestExtensions(TestFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    private void From_NATS_Msg_To_Cloud_Event()
    {
        var msg = _fixture.CreateNatsMessage();
        var ce = msg.ToCloudEvent(new JsonEventFormatter(), null);

        ce.Type.Should().BeEquivalentTo(msg.Header["ce-type"]);
        ce.Id.Should().BeEquivalentTo(msg.Header["ce-id"]);
        ce.Subject.Should().BeEquivalentTo(msg.Header["ce-subject"]);
        ce.Time.Should().BeExactly(DateTime.SpecifyKind(DateTime.Parse(msg.Header["ce-time"]), DateTimeKind.Utc).ToDateTimeOffset());
    }

    [Fact]
    private void To_Cloud_Event_Missing_Spec_Version_Should_Throw()
    {
        var msg = _fixture.CreateNatsMessage();
        msg.Header.Remove("ce-specversion");

        var func = () => { msg.ToCloudEvent(new JsonEventFormatter(), null); };

        func.Should().Throw<ArgumentException>();
    }

    [Fact]
    private void To_Cloud_Event_With_Content_Type()
    {
        var msg = _fixture.CreateNatsMessage();
        msg.Header["content-type"] = "application/cloudevents+json";

        var ce = msg.ToCloudEvent(new JsonEventFormatter(), null);
        
        ce.Type.Should().BeEquivalentTo(msg.Header["ce-type"]);
        ce.Id.Should().BeEquivalentTo(msg.Header["ce-id"]);
    }

    [Fact]
    private void From_Cloud_Event_To_NATS_Msg()
    {
        var ce = _fixture.CreateCloudEvent();
        var msg = ce.ToNatsMessage(new JsonEventFormatter(), "store.order");

        msg.HasHeaders.Should().BeTrue();
        msg.Header["ce-type"].Should().BeEquivalentTo(ce.Type);
        msg.Header["ce-id"].Should().BeEquivalentTo(ce.Id);
        msg.Header["ce-subject"].Should().BeEquivalentTo(ce.Subject);
    }
}