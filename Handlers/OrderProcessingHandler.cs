using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Company.Function.Models;
using System.Collections.Generic;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using System;
using Azure.Messaging.EventGrid;

namespace Company.Function
{
    public static class OrderProcessingHandler
    {
        [FunctionName("ProcessUpdatedOrders")]
        public static async Task FilterOrders(
           [CosmosDBTrigger(
                "%DatabaseName%",
                "%CollectionName%",
                ConnectionStringSetting = "CosmosDBConnection",
                LeaseCollectionName = "OrderLeases",
                CreateLeaseCollectionIfNotExists = true)] IReadOnlyList<Document> documents,
           [EventGrid(
                TopicEndpointUri = "OrderTopicEndpoint",
                TopicKeySetting = "OrderTopicKey")] IAsyncCollector<EventGridEvent> orderEvents,
           ILogger log)
        {
            var tasks = new List<Task>();

            foreach (var document in documents)
            {
                var order = JsonConvert.DeserializeObject<Order>(document.ToString());

                var orderEvent = new EventGridEvent($"order/{order.Id}", "Updated", "1.0", order);
                log.LogInformation($"Order updated: {order.Id}", order);
                tasks.Add(orderEvents.AddAsync(orderEvent));
            }

            await Task.WhenAll(tasks);
        }

        // The payment processing step requires approval if the order is over $100.
        [FunctionName("CalculateOrderTotal")]
        public static async Task CalculateOrderTotal(
            [EventGridTrigger] EventGridEvent orderEvent,
            [CosmosDB(
                "%DatabaseName%",
                "%CollectionName%",
                ConnectionStringSetting = "CosmosDBConnection")] IAsyncCollector<Order> ordersOutput,
            ILogger log)
        {
            var order = JsonConvert.DeserializeObject<Order>(orderEvent.Data.ToString());

            var subtotal = order.LineItems.Sum(o => o.Price * o.Quantity);
            order.Tax = Math.Round(subtotal * 0.0825m, 2);
            order.Total = subtotal + order.Tax;

            // Automatically approve orders under $100.
            if (order.Total < 100)
            {
                order.paymentApprovedTimestamp = DateTime.UtcNow;
                log.LogInformation($"Approved order payment: {order.Id}");
                await ordersOutput.AddAsync(order);
            }
            else
            {
                log.LogInformation($"Requesting order payment approval: {order.Id}");
                await ordersOutput.AddAsync(order);
            }
        }

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

            order.PaidTimestamp = DateTime.UtcNow;
            log.LogInformation($"Processed order payment: {order.Id}");
            await ordersOutput.AddAsync(order);
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
            log.LogInformation($"Shipped order: {order.Id}");
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
            log.LogInformation($"Notified customer of order: {order.Id}");
            await ordersOutput.AddAsync(order);
        }
    }
}
