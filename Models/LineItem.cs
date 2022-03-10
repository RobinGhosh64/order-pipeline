using Newtonsoft.Json;

namespace Company.Function.Models;

public class LineItem
{
    [JsonProperty("productName")]
    public string ProductName { get; set; }

    [JsonProperty("price")]
    public decimal Price { get; set; }

    [JsonProperty("quantity")]
    public int Quantity { get; set; }
}