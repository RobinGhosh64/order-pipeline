using System;
using System.Collections.Generic;
using Bogus;
using Company.Function.Models;

namespace Company.Function.Generators;

public class OrderGenerator
{
    private readonly Faker<LineItem> lineItemFaker;

    private readonly Faker<Order> orderFaker;

    public OrderGenerator()
    {
        lineItemFaker = new Faker<LineItem>()
            .RuleFor(o => o.ProductName, f => f.Commerce.Product())
            .RuleFor(o => o.Price, f => Math.Round(f.Random.Decimal(0, 100), 2))
            .RuleFor(o => o.Quantity, f => f.Random.Int(1, 10));

        orderFaker = new Faker<Order>()
            .RuleFor(o => o.Id, f => Guid.NewGuid())
            .RuleFor(o => o.FirstName, f => f.Name.FirstName())
            .RuleFor(o => o.LastName, f => f.Name.LastName())
            .RuleFor(o => o.Address, f => f.Address.FullAddress())
            .RuleFor(o => o.Email, (f, u) => f.Internet.Email(u.FirstName, u.LastName))
            .RuleFor(o => o.LineItems, f => lineItemFaker.Generate(f.Random.Int(1, 5)));
    }

    public IList<Order> Generate(int count)
    {
        return orderFaker.Generate(count);
    }
}