using System;
using System.Collections.Generic;

namespace Company.Function.Models;

public class Order
{
    public Guid Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Address { get; set; }
    public string Email { get; set; }
    public decimal? Tax { get; set; }
    public decimal? Total { get; set; }
    public IList<LineItem> LineItems { get; set; }
    public DateTime? AnticipatedDeliveryDate { get; set; }
    public DateTime? Created { get; set; }
    public DateTime? Paid { get; set; }
    public DateTime? Shipped { get; set; }
    public DateTime? Notified { get; set; }
}