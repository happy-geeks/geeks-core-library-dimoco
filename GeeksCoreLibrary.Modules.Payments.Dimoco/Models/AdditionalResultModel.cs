using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.Payments.Dimoco.Models;

/// <summary>
/// Model for an additional result from an action, is only used in webhooks.
/// </summary>
[XmlRoot("additional_result")]
public class AdditionalResultModel
{
    /// <summary>
    /// Name of additional dynamic return parameter.
    /// </summary>
    [XmlElement("key")]
    public string? Key { get; set; }

    /// <summary>
    /// Value of additional dynamic return parameter.
    /// </summary>
    [XmlElement("value")]
    public string? Value { get; set; }
}