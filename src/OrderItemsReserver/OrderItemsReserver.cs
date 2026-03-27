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
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
            [BlobOutput("orders/{sys.randguid}.json", Connection = "AzureWebJobsStorage")] IAsyncCollector<string> blobCollector)
        {
            _logger.LogInformation("Processing order reservation request.");

            // Read the JSON from the HTTP request
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            // Write to Blob
            await blobCollector.AddAsync(requestBody);

            // Create HTTP response
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync("Order processed and saved to storage.");

            return response;
        }
    }
}
