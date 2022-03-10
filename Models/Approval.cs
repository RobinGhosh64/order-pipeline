using Newtonsoft.Json;

namespace Company.Function.Models;

public class Approval
{
    [JsonProperty("type")]
    public ApprovalType Type { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("isApproved")]
    public bool isApproved { get; set; }
}