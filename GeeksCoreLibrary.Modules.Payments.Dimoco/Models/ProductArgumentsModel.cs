using Newtonsoft.Json;

namespace GeeksCoreLibrary.Modules.Payments.Dimoco.Models;

/// <summary>
/// Model for a product argument for the "prompt_product_args" parameter in an API call.
/// </summary>
public class ProductArgumentsModel
{
    /// <summary>
    /// The image of the product.
    /// </summary>
    [JsonProperty("pic")]
    public PictureModel Picture { get; set; } = new();

    /// <summary>
    /// The description of the product. Key should be the language code and value the description in that language.
    /// </summary>
    [JsonProperty("desc")]
    public Dictionary<string, string> Description { get; set; } = new();
}