using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.Payments.Dimoco.Models;

/// <summary>
/// Model for a customer/user, is only used in webhooks.
/// </summary>
[XmlRoot("customer")]
public class CustomerModel
{
    /// <summary>
    /// ISO 3166-1 Alpha 2 code.
    /// </summary>
    [XmlElement("country")]
    public string Country { get; set; }

    /// <summary>
    /// End user’s id, aka alias.
    /// </summary>
    [XmlElement("id")]
    public string Id { get; set; }

    /// <summary>
    /// IP address of end user’s device.
    /// </summary>
    [XmlElement("ip")]
    public string IpAddress { get; set; }

    /// <summary>
    /// ISO 639-1 code.
    /// </summary>
    [XmlElement("language")]
    public string Language { get; set; }

    /// <summary>
    /// End user’s MSISDN.
    /// </summary>
    [XmlElement("msisdn")]
    public string Msisdn { get; set; }

    /// <summary>
    /// End user’s operator.
    /// </summary>
    [XmlElement("operator")]
    public string Operator { get; set; }
}