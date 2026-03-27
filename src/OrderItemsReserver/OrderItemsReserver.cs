using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace eShopOnWeb.OrderItemsReserver
{
    public class OrderItemsReserver
    {
        private readonly ILogger _logger;

        public OrderItemsReserver(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<OrderItemsReserver>();
        }

        [Function("OrderItemsReserver")]
        [BlobOutput("orders/{sys.randguid}.json", Connection = "AzureWebJobsStorage")]
        public async Task<string> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Processing order reservation request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            return requestBody;
        }
    }
}
