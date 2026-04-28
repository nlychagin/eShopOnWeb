using System.Text.Json;
using Azure;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
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
    public async Task Run(
        [ServiceBusTrigger("order-items-reserver", Connection = "ServiceBusConnection")] string message)
    {
        _logger.LogInformation("OrderItemsReserver function triggered via Service Bus.");

        var items = JsonSerializer.Deserialize<List<OrderItem>>(message, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (items == null || items.Count == 0)
        {
            _logger.LogWarning("Order message is empty or invalid.");
            throw new ArgumentException("Order message is empty or invalid.");
        }

        var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
        var fileName = $"{DateTime.UtcNow:yyyy-MM-ddTHH-mm-ss}-{Guid.NewGuid()}-order.json";

        var connStr = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

        // Retry policy: up to 3 attempts
        int maxRetries = 3;
        int attempt = 0;
        bool uploaded = false;

        while (attempt < maxRetries && !uploaded)
        {
            try
            {
                attempt++;
                _logger.LogInformation("Upload attempt {Attempt} for {FileName}", attempt, fileName);

                var blobClient = new BlobClient(connStr, "orders", fileName);
                await blobClient.UploadAsync(new BinaryData(json));

                uploaded = true;
                _logger.LogInformation("Uploaded order to blob: {FileName}", fileName);
            }
            catch (RequestFailedException ex)
            {
                _logger.LogWarning("Attempt {Attempt} failed: {Message}", attempt, ex.Message);
                if (attempt >= maxRetries)
                {
                    _logger.LogError("All {MaxRetries} upload attempts failed for {FileName}.", maxRetries, fileName);
                    throw; // Re-throw so Service Bus moves message to dead letter queue
                }
                await Task.Delay(TimeSpan.FromSeconds(2 * attempt)); // exponential backoff
            }
        }
    }
}

public record OrderItem(int ItemId, int Quantity);
