using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Company.Function.Generators;

namespace Company.Function
{
    public static class OrderApiHandler
    {
        private static OrderGenerator orderGenerator = new OrderGenerator();

        [FunctionName("GenerateOrders")]
        public static async Task<IActionResult> GenerateOrders(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders")] HttpRequest req,
            ILogger log)
        {
            string strCount = req.Query["count"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            strCount = strCount ?? data?.count;

            if (int.TryParse(strCount, out int count) && count > 0)
            {
                var orders = orderGenerator.Generate(count);
                log.LogInformation($"{count} orders generated");
                return new OkObjectResult(orders);
            }
            else
            {
                log.LogError($"No order count specified");
                return new BadRequestObjectResult("No order count specified. Use query string param 'count' or JSON body with property 'count'");
            }
        }
    }
}
