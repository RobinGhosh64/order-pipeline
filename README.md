# Order Processing Example

This is an example of an event-driven processing pipeline utilizing Azure Functions, Azure Cosmos DB, and Azure Event Grid.

In this example, any number of fake orders can be generated and run through an order processing pipeline.

## Architecture

![Architecture](./Assets/architecture.png)

There are 6 deployed components for this architecture:

1. Function App - Acts as both the API for order interaction and the processing pipeline.
1. Cosmos DB (Core API) - Used to persist data and notify the system of order updates.
1. Event Grid Topic - Sends events to subscribers (Azure Functions) with filters to ensure the workflow/pipeline executes.
1. Application Insights - Captures telemetry from the Function App for logging and metrics.
1. Log Analytics Workspace - The log store behind Application Insights.
1. Storage Account (Not pictured) - Holds the deployment files for the Function App.

## The Pipeline

The following steps are modeled in the processing pipeline to simulate a simple, linear business process:

1. The Order API creates new orders and saves them to the database.
1. Order updates from the Cosmos DB change feed are forwarded to the Event Grid Topic through the ProcessOrderUpdates function. This happens on every update to an order in the DB and facilitates the overall workflow.
1. The new orders are sent to ProcessOrderTotal for total calculation. Orders below $100 are auto-approved for payment.
    * If the order is above $100, an approval for order payment must be set to the Order API
1. After order approval, ProcessOrderPayment charges the customer for the order.
1. The order is then shipped with ProcessOrderShipment.
1. The customer is then notified of the shipment with ProcessOrderNotification.

Each of the functions triggered by the Event Grid order topic receive filtered events from their subscriptions to ensure they only receive events for their step in the workflow. The filters for each function are:

* ProcessOrderTotal
  * data.total - Is null or undefined
* ProcessOrderPayment
  * data.paymentApprovedTimestamp - Is not null
  * data.paidTimestamp - Is null or undefined
* ProcessOrderShipment
  * data.paidTimestamp - Is not null
  * data.shippedTimestamp - Is null or undefined
* ProcessOrderNotification
  * data.shippedTimestamp - Is not null
  * data.notifiedTimestamp - Is null or undefined

## Environment Variables

When running locally, the following need to be set for the **local.settings.json** file. When deployed, the values section must be set as Application Settings so they will be injected as environment variables.

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "[storage-account-connection-string]",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "DatabaseName": "Sales",
    "CollectionName": "Orders",
    "CosmosDBConnection": "[cosmos-account-connection-string]",
    "OrderTopicEndpoint": "[event-grid-topic-endpoint]",
    "OrderTopicKey": "[event-grid-topic-key]"
  }
}
```
