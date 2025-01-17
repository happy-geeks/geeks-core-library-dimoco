using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.Payments.Dimoco.Models;

/// <summary>
/// Model for an API result, can be used for both request and webhook.
/// Some properties are only used in the response of a request and some are only used in the webhook.
/// </summary>
[XmlRoot("result")]
public class ApiResultModel
{
    /// <summary>
    /// The action this result refers to.
    /// Only used in webhooks.
    /// </summary>
    [XmlElement("action")]
    public string? Action { get; set; }

    /// <summary>
    /// The result of the action.
    /// Used in webhooks and API responses.
    /// </summary>
    [XmlElement("action_result")]
    public ActionResultModel ActionResult { get; set; } = new();

    /// <summary>
    /// A list of additional results for the action.
    /// Only used in webhooks.
    /// </summary>
    [XmlArray("additional_results")]
    [XmlArrayItem("additional_result", typeof(AdditionalResultModel))]
    public List<AdditionalResultModel> AdditionalResults { get; set; } = [];

    /// <summary>
    /// A list of custom parameters that were passed to the original API call.
    /// Only used in webhooks.
    /// </summary>
    [XmlArray("custom_parameters")]
    [XmlArrayItem("custom_parameter", typeof(CustomParameterModel))]
    public List<CustomParameterModel> CustomParameters { get; set; } = [];

    /// <summary>
    /// The customer that made the transaction.
    /// Only used in webhooks.
    /// </summary>
    [XmlElement("customer")]
    public CustomerModel Customer { get; set; } = new();

    /// <summary>
    /// The payment parameters for the transaction.
    /// Only used in webhooks.
    /// </summary>
    [XmlElement("payment_parameters")]
    public PaymentParametersModel PaymentParameters { get; set; } = new();

    /// <summary>
    /// Correlation id for matching callback with initiating API call.
    /// Used in webhooks and API responses.
    /// </summary>
    [XmlElement("reference")]
    public string? Reference { get; set; }

    /// <summary>
    /// Request_id provided with the API request.
    /// Used in webhooks and API responses.
    /// </summary>
    [XmlElement("request_id")]
    public string? RequestId { get; set; }

    /// <summary>
    /// Any transactions that were made to make the payment.
    /// Only used in webhooks.
    /// </summary>
    [XmlArray("transactions")]
    [XmlArrayItem("transaction", typeof(TransactionModel))]
    public List<TransactionModel> Transactions { get; set; } = [];
}