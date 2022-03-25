using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Company.Function.Generators;
using Company.Function.Models;
using System.Collections.Generic;
using System;

namespace Company.Function
{
    public static class OrderApiHandler
    {
        private static OrderGenerator orderGenerator = new OrderGenerator();

        [FunctionName("GenerateOrders")]
        public static async Task<IActionResult> GenerateOrders(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "post",
                Route = "orders")] HttpRequest req,
            [CosmosDB(
                "%DatabaseName%",
                "%CollectionName%",
                ConnectionStringSetting = "CosmosDBConnection")] IAsyncCollector<Order> ordersOutput,
            ILogger log)
        {
            // Get the number of orders to generate.
            string strCount = req.Query["count"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            strCount = strCount ?? data?.count;

            if (int.TryParse(strCount, out int count) && count > 0)
            {
                var orders = orderGenerator.Generate(count);
                var tasks = new List<Task>();

                // Add the orders to Cosmos.
                foreach (var order in orders)
                {
                    order.CreatedTimestamp = DateTime.UtcNow;
                    log.LogInformation($"Generated order: {order.Id}");
                    tasks.Add(ordersOutput.AddAsync(order));
                }

                await Task.WhenAll(tasks);
                return new OkObjectResult(orders);
            }
            else
            {
                log.LogError($"No order count specified");
                return new BadRequestObjectResult("No order count specified. Use query string param 'count' or JSON body with property 'count'");
            }
        }

        [FunctionName("GetOrders")]
        public static IActionResult GetOrders(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "orders")] HttpRequest req,
            [CosmosDB(
                "%DatabaseName%",
                "%CollectionName%",
                ConnectionStringSetting = "CosmosDBConnection",
                SqlQuery = "Select * FROM o ORDER BY o._ts DESC")] IEnumerable<Order> orders,
            ILogger log)
        {
            return new OkObjectResult(orders);
        }

        [FunctionName("ApproveOrder")]
        public static async Task<IActionResult> ApproveOrder(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "post",
                Route = "orders/{id}/approve")] HttpRequest req,
            [CosmosDB(
                "%DatabaseName%",
                "%CollectionName%",
                ConnectionStringSetting = "CosmosDBConnection",
                Id = "{id}",
                PartitionKey = "{id}")] Order order,
            Guid id,
            [CosmosDB(
                "%DatabaseName%",
                "%CollectionName%",
                ConnectionStringSetting = "CosmosDBConnection")] IAsyncCollector<Order> ordersOutput,
            ILogger log)
        {
            if (order != null)
            {
                if (order.PaymentApprovedTimestamp != null)
                {
                    log.LogError($"Order {id} has no pending approvals");
                    return new BadRequestObjectResult($"Order {id} has no pending approvals");
                }
                else
                {
                    order.PaymentApprovedTimestamp = DateTime.UtcNow;
                    log.LogInformation($"Approved order payment: {order.Id}");
                    await ordersOutput.AddAsync(order);
                    return new OkObjectResult(order);
                }
            }
            else
            {
                log.LogError($"Order not found: {id}");
                return new NotFoundObjectResult($"Order not found: {id}");
            }
        }
    }
}