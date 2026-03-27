using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

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
            FunctionContext context)
        {
            _logger.LogInformation("Processing order reservation request.");

            // Read request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Request body is empty.");
                return badResponse;
            }

            try
            {
                // Connect to Azure Blob Storage
                var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                var containerClient = new BlobContainerClient(connectionString, "orders");

                // Create the container if it doesn't exist
                await containerClient.CreateIfNotExistsAsync();

                // Generate unique filename
                var blobName = $"{Guid.NewGuid()}.json";
                var blobClient = containerClient.GetBlobClient(blobName);

                // Upload JSON content
                using var ms = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
                await blobClient.UploadAsync(ms);

                // Return success response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync($"Order processed and saved to storage as {blobName}.");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving order to Blob Storage.");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error saving order: {ex.Message}");
                return errorResponse;
            }
        }
    }
}
