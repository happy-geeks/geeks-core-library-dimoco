using System.Xml.Serialization;

namespace GeeksCoreLibrary.Modules.Payments.Dimoco.Models;

/// <summary>
/// Model for a transaction done by a user, is only used in webhooks.
/// </summary>
[XmlRoot("transaction")]
public class TransactionModel
{
    /// <summary>
    /// Requested transaction amount.
    /// </summary>
    [XmlElement("amount")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Actual billed transaction amount.
    /// </summary>
    [XmlElement("billed_amount")]
    public decimal BilledAmount { get; set; }

    /// <summary>
    /// Currency of billing transaction (ISO 4271 alphabetic code).
    /// </summary>
    [XmlElement("currency")]
    public string Currency { get; set; }

    /// <summary>
    /// Id of billing transaction.
    /// </summary>
    [XmlElement("id")]
    public string Id { get; set; }

    [XmlElement("sms_message")]
    public SmsMessageModel SmsMessage { get; set; }

    /// <summary>
    /// Status of the payment transaction.
    /// </summary>
    [XmlElement("status")]
    public int Status { get; set; }
}