using System.Globalization;
using System.Net.Mime;
using CloudNative.CloudEvents.Core;
using NATS.Client;

namespace CloudNative.CloudEvents.NATS;

public static class Extensions
{
    private const string NatsHeader = "ce-";
    
    public static bool IsCloudEvent(this Msg msg)
    {
        return msg.HasHeaders && MimeUtilities.IsCloudEventsContentType((msg.Header["content-type"]));
    }

    public static Msg ToNatsMessage(this CloudEvent ce, CloudEventFormatter formatter, string subject)
    {
        return ce.ToNatsMessage(formatter, subject, null);
    }

    public static Msg ToNatsMessage(this CloudEvent ce, CloudEventFormatter formatter, string subject, string reply)
    {
        _ = ce ?? throw new ArgumentNullException(nameof(ce));

        var headers = MapToHeaders(ce);
        
        var data = formatter.EncodeStructuredModeMessage(ce, out var contentType);

        var msg = reply is null
            ? new Msg(subject, headers, data.ToArray())
            : new Msg(subject, reply, headers, data.ToArray());

        return msg;
    }

    public static CloudEvent ToCloudEvent(this Msg msg, CloudEventFormatter formatter, IEnumerable<CloudEventAttribute> extensionAttributes)
    {
        _ = msg ?? throw new ArgumentNullException(nameof(msg));
        _ = formatter ?? throw new ArgumentNullException(nameof(formatter));
        
        if (msg.IsCloudEvent())
        {
            var contentType = new ContentType(msg.Header["content-type"]);
            return formatter.DecodeStructuredModeMessage(msg.Data, contentType, extensionAttributes);
        }
        else
        {
            var headers = msg.HasHeaders ? msg.Header : throw new ArgumentException("NATS message has no headers");
            
            if (!headers.TryGetValue("ce-specversion", out var specVersion))
            {
                throw new ArgumentException("Message is not an CloudEvent");
            }

            var version = CloudEventsSpecVersion.FromVersionId(specVersion) ??
                          throw new ArgumentException("Unknown CloudEvents spec version {Version}", specVersion);

            var cloudEvent = new CloudEvent(version, extensionAttributes);

            foreach (var key in headers.Keys.OfType<string>())
            {
                if (!key.StartsWith(NatsHeader))
                    continue;

                var attributeName = key.Substring(NatsHeader.Length).ToLowerInvariant();

                if (attributeName == CloudEventsSpecVersion.SpecVersionAttribute.Name)
                    continue;

                if (attributeName == "time" && headers.TryGetValue("ce-time", out var timeValue))
                {
                    var time = DateTime.Parse(timeValue);
                    if (time.Kind != DateTimeKind.Utc)
                        time = DateTime.SpecifyKind(time, DateTimeKind.Utc);

                    cloudEvent[attributeName] = (DateTimeOffset) time;
                }
                else
                {
                    cloudEvent.SetAttributeFromString(attributeName,headers[key]);
                }
            }
            
            formatter.DecodeBinaryModeEventData(msg.Data, cloudEvent);

            return Validation.CheckCloudEventArgument(cloudEvent, nameof(msg));
        }
    }

    private static MsgHeader MapToHeaders(CloudEvent ce)
    {
        MsgHeader headers = new() {{"ce-specversion", ce.SpecVersion.VersionId}};

        foreach (var (attribute, value) in ce.GetPopulatedAttributes())
        {
            if (attribute == ce.SpecVersion.DataContentTypeAttribute)
                continue;

            var headerKey = NatsHeader + attribute.Name;

            var headerValue = value switch
            {
                Uri uri => uri.ToString(),
                DateTimeOffset time => time.UtcDateTime.ToString(CultureInfo.InvariantCulture),
                _ => value.ToString()
            };
            
            headers.Add(headerKey, headerValue);
        }

        return headers;
    }

    private static bool TryGetValue(this MsgHeader header, string key, out string value)
    {
        var headerValue = header[key];

        if (headerValue is not null)
        {
            value = headerValue;
            return true;
        }

        value = default;
        return false;
    }
}