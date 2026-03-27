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
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Processing order reservation request.");

            // Read the JSON from the web request
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            // Create a response to send back to the eShop website
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync("Order processed and saved to storage.");

            // The return value of this method is what the BlobOutput binding saves
            // In Isolated mode, we return the response object
            return response;
        }
    }
}
