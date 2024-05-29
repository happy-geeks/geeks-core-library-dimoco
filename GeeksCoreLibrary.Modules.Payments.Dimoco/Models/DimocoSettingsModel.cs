using GeeksCoreLibrary.Components.OrderProcess.Models;

namespace GeeksCoreLibrary.Modules.Payments.Dimoco.Models;

public class DimocoSettingsModel : PaymentServiceProviderSettingsModel
{
    /// <summary>
    /// Gets or sets the merchant ID.
    /// </summary>
    public string MerchantId { get; set; }

    /// <summary>
    /// Gets or sets the order ID. This has nothing to do with orders from our database,
    /// this is a static number for Dimoco that is always the same.
    /// </summary>
    public string OrderId { get; set; }

    /// <summary>
    /// Gets or sets the client secret.
    /// </summary>
    public string ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the URL to the logo of the webshop. This logo will be shown on the payment page of Dimoco.
    /// </summary>
    public string LogoUrl { get; set; }

    /// <summary>
    /// Gets or sets the name of the service. This name will be shown on the payment page of Dimoco.
    /// </summary>
    public string ServiceName { get; set; }
}