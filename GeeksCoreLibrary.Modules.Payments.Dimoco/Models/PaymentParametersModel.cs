using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.Payments.Dimoco.Models;

/// <summary>
/// Model for a payment parameter, is only used in webhooks.
/// </summary>
[XmlRoot("payment_parameters")]
public class PaymentParametersModel
{
    /// <summary>
    /// End user authorization technique – one of web, wap or sms.
    /// </summary>
    [XmlElement("channel")]
    public string? Channel { get; set; }

    /// <summary>
    /// Payment method used – one of OPERATOR or ISP.
    /// </summary>
    [XmlElement("method")]
    public string? Method { get; set; }
}