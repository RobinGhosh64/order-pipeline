using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Company.Function.Models;

public class Order
{
    [JsonProperty("id")]
    public Guid Id { get; set; }

    [JsonProperty("firstName")]
    public string FirstName { get; set; }

    [JsonProperty("lastName")]
    public string LastName { get; set; }

    [JsonProperty("address")]
    public string Address { get; set; }

    [JsonProperty("email")]
    public string Email { get; set; }

    [JsonProperty("tax")]
    public decimal? Tax { get; set; }

    [JsonProperty("total")]
    public decimal? Total { get; set; }

    [JsonProperty("lineItems")]
    public IList<LineItem> LineItems { get; set; }

    [JsonProperty("approvals")]
    public IList<Approval> Approvals { get; set; }

    [JsonProperty("anticipatedDeliveryDate")]
    public DateTime? AnticipatedDeliveryDate { get; set; }

    [JsonProperty("createdTimestamp")]
    public DateTime? CreatedTimestamp { get; set; }

    [JsonProperty("paidTimestamp")]
    public DateTime? PaidTimestamp { get; set; }

    [JsonProperty("shippedTimestamp")]
    public DateTime? ShippedTimestamp { get; set; }

    [JsonProperty("notifiedTimestamp")]
    public DateTime? NotifiedTimestamp { get; set; }
}