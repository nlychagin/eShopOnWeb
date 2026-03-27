using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using System.Text;

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
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Processing order reservation request.");

            var body = await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(body))
            {
                var bad = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Request body empty.");
                return bad;
            }

            try
            {
                var conn = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                var container = new BlobContainerClient(conn, "orders");
                await container.CreateIfNotExistsAsync();

                var blobName = $"{Guid.NewGuid()}.json";
                var blob = container.GetBlobClient(blobName);
                await blob.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes(body)));

                var ok = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await ok.WriteStringAsync($"Saved as {blobName}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving order");
                var error = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await error.WriteStringAsync($"Error: {ex.Message}");
                return error;
            }
        }
    }
}
