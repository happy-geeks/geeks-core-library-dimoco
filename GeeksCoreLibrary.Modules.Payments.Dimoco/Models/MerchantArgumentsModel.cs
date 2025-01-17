using Newtonsoft.Json;

namespace GeeksCoreLibrary.Modules.Payments.Dimoco.Models;

/// <summary>
/// Model for a product argument for the "prompt_merchant_args" parameter in an API call.
/// </summary>
public class MerchantArgumentsModel
{
    /// <summary>
    /// The image of the company logo.
    /// </summary>
    [JsonProperty("logo")]
    public PictureModel Logo { get; set; } = new();
}