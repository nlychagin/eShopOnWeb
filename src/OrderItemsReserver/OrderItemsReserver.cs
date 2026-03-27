using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using System.Net;
using System.Text;

namespace eShopOnWeb.OrderItemsReserver
{
    public class OrderItemsReserver
    {
        private readonly ILogger _logger;
        private readonly BlobContainerClient _containerClient;

        public OrderItemsReserver(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<OrderItemsReserver>();

            // Connect to the 'orders' container using connection string from AzureWebJobsStorage
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                                   ?? throw new InvalidOperationException("AzureWebJobsStorage not set");
            _containerClient = new BlobContainerClient(connectionString, "orders");
        }

        [Function("OrderItemsReserver")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Processing order reservation request.");

            // Read the JSON from HTTP request
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            // Generate a random filename
            string blobName = $"{Guid.NewGuid()}.json";

            // Upload to Blob Storage
            var blobClient = _containerClient.GetBlobClient(blobName);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
            await blobClient.UploadAsync(stream);

            // Respond
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync($"Order processed and saved as {blobName}.");

            return response;
        }
    }
}
