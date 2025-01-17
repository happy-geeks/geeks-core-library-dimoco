using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.Payments.Dimoco.Models;

/// <summary>
/// Model for a custom parameter, is only used in webhooks.
/// </summary>
[XmlRoot("custom_parameter")]
public class CustomParameterModel
{
    /// <summary>
    /// Name of custom parameter passed to the API call.
    /// </summary>
    [XmlElement("key")]
    public string? Key { get; set; }

    /// <summary>
    /// Value of custom parameter passed to the API call.
    /// </summary>
    [XmlElement("value")]
    public string? Value { get; set; }
}