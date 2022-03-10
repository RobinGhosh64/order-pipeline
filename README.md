# Order Processing Example

This is an example of an event-driven processing pipeline utilizing Azure Functions, Azure Cosmos DB, and Azure Event Grid.

In this example, any number of fake orders can be generated and run through an order processing pipeline. This pipeline has 5 states for an order:

* Created - The initial state when the order is first received through the Web API.
* Pending Payment Approval - When the order total has been calculated and the order is over $100.
* Paid - The order has been paid for.
* Shipped - The order has been shipped.
* Notified - The customer has been notified and order processing is complete.

## Architecture

![Architecture](./Assets/architecture.png)
