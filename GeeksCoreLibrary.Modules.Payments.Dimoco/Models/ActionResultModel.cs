using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.Payments.Dimoco.Models;

/// <summary>
/// Model for the result of an action, can be used for both request and webhook.
/// Some properties are only used in the response of a request and some are only used in the webhook.
/// </summary>
[XmlRoot("action_result")]
public class ActionResultModel
{
    /// <summary>
    /// Additional code of the reported status providing further details.
    /// Used in webhooks and API responses.
    /// </summary>
    [XmlElement("code")]
    public string? Code { get; set; }

    /// <summary>
    /// Verbose description of the status.
    /// Used in webhooks and API responses.
    /// </summary>
    [XmlElement("detail")]
    public string? Detail { get; set; }

    /// <summary>
    /// Error code and/or error description from PSP.
    /// Only used in webhooks.
    /// </summary>
    [XmlElement("detail_psp")]
    public string? DetailPsp { get; set; }

    /// <summary>
    /// URL where the end user shall be redirected (only present with status 3).
    /// Only used in API responses.
    /// </summary>
    [XmlElement("redirect")]
    public RedirectModel Redirect { get; set; } = new();

    /// <summary>
    /// 0 = success,
    /// 1 = failure,
    /// 3 = redirect required,
    /// 4 = validation failed,
    /// 5 = pending – result will be reported in callback.
    /// Used in webhooks and API responses.
    /// </summary>
    [XmlElement("status")]
    public int Status { get; set; }
}