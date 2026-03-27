using System.Text.Json;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace OrderItemsReserver;

public class OrderReserveFunction
{
    private readonly ILogger<OrderReserveFunction> _logger;

    public OrderReserveFunction(ILogger<OrderReserveFunction> logger)
    {
        _logger = logger;
    }

    [Function("OrderItemsReserver")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation("OrderItemsReserver function triggered.");

        // Read the request body
        var body = await new StreamReader(req.Body).ReadToEndAsync();

        // Deserialize and validate
        var items = JsonSerializer.Deserialize<List<OrderItem>>(body);
        if (items == null || items.Count == 0)
        {
            var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("Order request body is empty or invalid.");
            return badResponse;
        }

        // Serialize to indented JSON
        var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });

        // Generate a unique filename using timestamp + GUID
        var fileName = $"{DateTime.UtcNow:yyyy-MM-ddTHH-mm-ss}-{Guid.NewGuid()}-order.json";

        // Upload to Blob Storage
        var connStr = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        var blobClient = new BlobClient(connStr, "orders", fileName);
        await blobClient.UploadAsync(new BinaryData(json));

        _logger.LogInformation("Uploaded order to blob: {FileName}", fileName);

        // Return 200 OK
        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteStringAsync($"Order uploaded successfully as {fileName}");
        return response;
    }
}

public record OrderItem(int ItemId, int Quantity);
