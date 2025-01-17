using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.Payments.Dimoco.Models;

/// <summary>
/// Model for an SMS message, is only used in webhooks.
/// </summary>
[XmlRoot("sms_message")]
public class SmsMessageModel
{
    /// <summary>
    /// Id of billing SMS.
    /// </summary>
    [XmlElement("id")]
    public string? Id { get; set; }
}