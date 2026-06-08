using Microsoft.Azure.Cosmos;
using AI_Order.Shared.Models;

namespace AI_Order.Api.Services;

public class CosmosOrderService
{
    private readonly CosmosClient? _client;
    private readonly string _databaseId;
    private readonly string _containerId;
    private readonly bool _enabled;
    private Container? _container;

    public CosmosOrderService(IConfiguration configuration)
    {
        var endpoint = configuration["CosmosDb:Endpoint"];
        var key = configuration["CosmosDb:Key"];
        _databaseId = configuration["CosmosDb:DatabaseId"] ?? "QrOrder";
        _containerId = configuration["CosmosDb:ContainerId"] ?? "Order";

        if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(key))
        {
            _client = new CosmosClient(endpoint, key, new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            });
            _enabled = true;
        }
    }

    // Creates database + container on first use; partition key path must match camelCase property name.
    private async Task<Container> GetContainerAsync()
    {
        if (_container is not null) return _container;

        var db = await _client!.CreateDatabaseIfNotExistsAsync(_databaseId);
        var response = await db.Database.CreateContainerIfNotExistsAsync(
            new ContainerProperties(_containerId, "/aspNetUserId")
            {
                DefaultTimeToLive = -1 // no TTL
            });
        _container = response.Container;
        return _container;
    }

    public async Task SaveOrderAsync(TableOrder order)
    {
        if (!_enabled) return;
        try
        {
            var container = await GetContainerAsync();
            await container.UpsertItemAsync(order, new PartitionKey(order.AspNetUserId));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[CosmosDB] SaveOrderAsync failed: {ex.Message}");
            throw; // re-throw so the caller knows the save failed
        }
    }

    public async Task<List<TableOrder>> GetOrdersAsync(string aspNetUserId)
    {
        if (!_enabled) return new();
        try
        {
            var container = await GetContainerAsync();
            var query = new QueryDefinition(
                    "SELECT * FROM c WHERE c.aspNetUserId = @userId" +
                    " AND c.status != @completed AND c.status != @cancelled")
                .WithParameter("@userId", aspNetUserId)
                .WithParameter("@completed", (int)OrderStatus.Completed)
                .WithParameter("@cancelled", (int)OrderStatus.Cancelled);
            var iterator = container.GetItemQueryIterator<TableOrder>(query,
                requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(aspNetUserId) });

            var results = new List<TableOrder>();
            while (iterator.HasMoreResults)
                results.AddRange(await iterator.ReadNextAsync());
            return results;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[CosmosDB] GetOrdersAsync failed: {ex.Message}");
            return new();
        }
    }

    public async Task<bool> UpdateOrderStatusAsync(string id, string aspNetUserId, OrderStatus status)
    {
        if (!_enabled) return false;
        try
        {
            var container = await GetContainerAsync();
            var patch = new[] { PatchOperation.Replace("/status", (int)status) };
            await container.PatchItemAsync<TableOrder>(id, new PartitionKey(aspNetUserId), patch);
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[CosmosDB] UpdateOrderStatusAsync failed: {ex.Message}");
            return false;
        }
    }
}
