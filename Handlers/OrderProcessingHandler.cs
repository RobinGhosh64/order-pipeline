using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Company.Function.Models;
using System.Collections.Generic;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Azure.Messaging;
using System;
using Azure.Messaging.EventGrid;

namespace Company.Function
{
    public static class OrderProcessingHandler
    {
        [FunctionName("FilterOrders")]
        public static async Task FilterOrders(
           [CosmosDBTrigger(
                "%DatabaseName%",
                "%CollectionName%",
                ConnectionStringSetting = "CosmosDBConnection",
                LeaseCollectionName = "OrderLeases",
                CreateLeaseCollectionIfNotExists = true)] IReadOnlyList<Document> documents,
           [EventGrid(
                TopicEndpointUri = "OrderPaymentTopicEndpoint",
                TopicKeySetting = "OrderPaymentTopicKey")] IAsyncCollector<EventGridEvent> orderPaymentEvents,
           [EventGrid(
                TopicEndpointUri = "OrderShippingTopicEndpoint",
                TopicKeySetting = "OrderShippingTopicKey")] IAsyncCollector<EventGridEvent> orderShippingEvents,
           [EventGrid(
                TopicEndpointUri = "OrderNotificationTopicEndpoint",
                TopicKeySetting = "OrderNotificationTopicKey")] IAsyncCollector<EventGridEvent> orderNotificationEvents,
           ILogger log)
        {
            var tasks = new List<Task>();

            foreach (var document in documents)
            {
                var order = JsonConvert.DeserializeObject<Order>(document.ToString());

                if (order.PaidTimestamp == null)
                {
                    // Send an order payment event.
                    var orderPaymentEvent = new EventGridEvent("OrderCreated", "Created", "1.0", order);
                    log.LogInformation($"Filtered order for payment: {order.Id}", order);
                    tasks.Add(orderPaymentEvents.AddAsync(orderPaymentEvent));
                }
                else if (order.ShippedTimestamp == null)
                {
                    // Send an order shipping event.
                    var orderShippingEvent = new EventGridEvent("OrderPaid", "Paid", "1.0", order);
                    log.LogInformation($"Filtered order for shipping: {order.Id}", order);
                    tasks.Add(orderShippingEvents.AddAsync(orderShippingEvent));
                }
                else if (order.NotifiedTimestamp == null)
                {
                    // Send an order notification event.
                    var orderNotificationEvent = new EventGridEvent("OrderShipped", "1.0", "Shipped", order);
                    log.LogInformation($"Filtered order for notification: {order.Id}", order);
                    tasks.Add(orderNotificationEvents.AddAsync(orderNotificationEvent));
                }
            }

            await Task.WhenAll(tasks);
        }

        // The payment processing step requires approval if the order is over $100.
        [FunctionName("ProcessOrderPayment")]
        public static async Task ProcessOrderPayment(
            [EventGridTrigger] EventGridEvent orderEvent,
            [CosmosDB(
                "%DatabaseName%",
                "%CollectionName%",
                ConnectionStringSetting = "CosmosDBConnection")] IAsyncCollector<Order> ordersOutput,
            ILogger log)
        {
            var order = JsonConvert.DeserializeObject<Order>(orderEvent.Data.ToString());

            // Calculate order payment and run validation.
            if (order.Total == null)
            {
                log.LogInformation(JsonConvert.SerializeObject(order));
                var subtotal = order.LineItems.Sum(o => o.Price * o.Quantity);
                order.Tax = Math.Round(subtotal * 0.0825m, 2);
                order.Total = subtotal + order.Tax;
            }

            // Check for payment approval for any order over $100.
            // Ensure we don't save the order if the payment is pending approval.
            if (order.Approvals != null && order.Approvals.Any(a => a.Type == ApprovalType.PaymentOver100 && !a.isApproved))
            {
                log.LogInformation($"Order {order.Id} is pending approval.", order);
            }
            else if (order.Total < 100 ||
                order.Total > 100 && order.Approvals != null && order.Approvals.Any(a => a.Type == ApprovalType.PaymentOver100 && a.isApproved))
            {
                order.PaidTimestamp = DateTime.UtcNow;
                log.LogInformation($"Processed order payment: {order.Id}", order);
                await ordersOutput.AddAsync(order);
            }
            else
            {
                order.Approvals = order.Approvals ?? new List<Approval>();
                order.Approvals.Add(new Approval
                {
                    Type = ApprovalType.PaymentOver100,
                    Description = "Payment over $100",
                    isApproved = false
                });

                log.LogInformation($"Requesting order payment approval: {order.Id}", order);
                await ordersOutput.AddAsync(order);
            }
        }

        [FunctionName("ProcessOrderShipment")]
        public static async Task ProcessOrderShipment(
            [EventGridTrigger] EventGridEvent orderEvent,
            [CosmosDB(
                "%DatabaseName%",
                "%CollectionName%",
                ConnectionStringSetting = "CosmosDBConnection")] IAsyncCollector<Order> ordersOutput,
            ILogger log)
        {
            var order = JsonConvert.DeserializeObject<Order>(orderEvent.Data.ToString());

            Random rnd = new Random();
            order.AnticipatedDeliveryDate = DateTime.UtcNow.AddDays(rnd.Next(1, 7)).Date;

            order.ShippedTimestamp = DateTime.UtcNow;
            log.LogInformation($"Shipped order: {order.Id}", order);
            await ordersOutput.AddAsync(order);
        }

        [FunctionName("ProcessOrderNotification")]
        public static async Task ProcessOrderNotification(
            [EventGridTrigger] EventGridEvent orderEvent,
            [CosmosDB(
                "%DatabaseName%",
                "%CollectionName%",
                ConnectionStringSetting = "CosmosDBConnection")] IAsyncCollector<Order> ordersOutput,
            ILogger log)
        {
            var order = JsonConvert.DeserializeObject<Order>(orderEvent.Data.ToString());
            order.NotifiedTimestamp = DateTime.UtcNow;
            log.LogInformation($"Notified customer of order: {order.Id}", order);
            await ordersOutput.AddAsync(order);
        }
    }
}
