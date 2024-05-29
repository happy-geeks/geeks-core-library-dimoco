using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.Payments.Dimoco.Models;

/// <summary>
/// Model for a transaction done by a user, is only used in API responses.
/// </summary>
[XmlRoot("redirect")]
public class RedirectModel
{
    /// <summary>
    /// The URL to redirect the user to.
    /// </summary>
    [XmlElement("url")]
    public string Url { get; set; }
}