using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace OrderDeliveryProcessor;

public class OrderDeliveryFunction
{
    private readonly ILogger<OrderDeliveryFunction> _logger;

    public OrderDeliveryFunction(ILogger<OrderDeliveryFunction> logger)
    {
        _logger = logger;
    }

    [Function("OrderDeliveryProcessor")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation("OrderDeliveryProcessor function triggered.");

        var body = await new StreamReader(req.Body).ReadToEndAsync();

        var order = JsonSerializer.Deserialize<DeliveryOrder>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (order == null)
        {
            var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("Order request body is empty or invalid.");
            return badResponse;
        }

        order.Id = Guid.NewGuid().ToString();

        var connStr = Environment.GetEnvironmentVariable("CosmosDbConnectionString");
        var dbName = Environment.GetEnvironmentVariable("CosmosDbName");
        var containerName = Environment.GetEnvironmentVariable("CosmosDbContainerName");

        // Serialize to dictionary to ensure lowercase "id" for CosmosDB
        var document = new Dictionary<string, object?>
        {
            ["id"] = order.Id,
            ["shippingAddress"] = order.ShippingAddress,
            ["items"] = order.Items,
            ["finalPrice"] = order.FinalPrice
        };

        var cosmosClient = new CosmosClient(connStr);
        var container = cosmosClient.GetContainer(dbName, containerName);
        await container.CreateItemAsync(document, new PartitionKey(order.Id));

        _logger.LogInformation("Order saved to CosmosDB with id: {Id}", order.Id);

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteStringAsync($"Order saved to CosmosDB with id: {order.Id}");
        return response;
    }
}

public class DeliveryOrder
{
    public string? Id { get; set; }
    public Address? ShippingAddress { get; set; }
    public List<OrderItem>? Items { get; set; }
    public decimal FinalPrice { get; set; }
}

public class Address
{
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? ZipCode { get; set; }
}

public class OrderItem
{
    public int ItemId { get; set; }
    public string? ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
