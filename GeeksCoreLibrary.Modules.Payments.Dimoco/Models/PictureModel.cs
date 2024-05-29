using Newtonsoft.Json;

namespace GeeksCoreLibrary.Modules.Payments.Dimoco.Models;

/// <summary>
/// Model for a product picture argument for the "prompt_product_args" parameter in an API call.
/// </summary>
public class PictureModel
{
    /// <summary>
    /// The URL of the product picture.
    /// </summary>
    [JsonProperty("img")]
    public string Url { get; set; }

    /// <summary>
    /// The alt text of the product picture.
    /// </summary>
    [JsonProperty("alt")]
    public string AltText { get; set; }
}