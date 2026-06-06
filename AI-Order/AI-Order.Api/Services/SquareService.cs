using AI_Order.Shared.Models;
using Square;
using Square.Catalog;
using Square.Orders;

namespace AI_Order.Api.Services;

public interface ISquareService
{
    Task<List<MenuItemDto>> GetCatalogItemsAsync();
    Task<SubmitOrderResponseDto> CreateOrderAsync(SubmitOrderRequestDto request);
    Task<List<KitchenOrderDto>> GetKitchenOrdersAsync();
    Task<bool> CompleteOrderAsync(string orderId, int version);
}

public class SquareService : ISquareService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SquareService> _logger;
    private readonly SquareClient _squareClient;

    public SquareService(IConfiguration config, ILogger<SquareService> logger)
    {
        _config = config;
        _logger = logger;

        var accessToken = config["Square:AccessToken"] ?? string.Empty;
        var environment = config["Square:Environment"] ?? "Sandbox";

        var baseUrl = environment == "Production"
            ? "https://connect.squareup.com"
            : "https://connect.squareupsandbox.com";

        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
        };

        _squareClient = new SquareClient(accessToken, clientOptions: new ClientOptions
        {
            BaseUrl = baseUrl,
            HttpClient = new HttpClient(handler)
        });
    }

    public async Task<List<MenuItemDto>> GetCatalogItemsAsync()
    {
        var result = new List<MenuItemDto>();

        try
        {
            var pager = await _squareClient.Catalog.ListAsync(new ListCatalogRequest
            {
                Types = "ITEM,ITEM_VARIATION,MODIFIER_LIST"
            });

            await foreach (var catalogObject in pager)
            {
                if (!catalogObject.IsItem) continue;

                var squareItem = catalogObject.AsItem();
                var itemData = squareItem.ItemData;
                if (itemData == null) continue;

                long priceAmount = 0;
                var firstVariation = itemData.Variations?.FirstOrDefault();
                if (firstVariation != null && firstVariation.TryAsItemVariation(out var varObj))
                    priceAmount = varObj?.ItemVariationData?.PriceMoney?.Amount ?? 0;

                var menuItem = new MenuItemDto
                {
                    Id = squareItem.Id ?? string.Empty,
                    Name = itemData.Name ?? "Unknown Item",
                    Description = itemData.Description ?? string.Empty,
                    Price = (decimal)priceAmount / 100m,
                    Category = itemData.CategoryId ?? "Other",
                    IsAvailable = !squareItem.IsDeleted.GetValueOrDefault(),
                };

                if (itemData.ModifierListInfo?.Any() == true)
                {
                    foreach (var modInfo in itemData.ModifierListInfo)
                    {
                        menuItem.ModifierGroups.Add(new ModifierGroupDto
                        {
                            Name = modInfo.ModifierListId ?? string.Empty,
                            Required = modInfo.MinSelectedModifiers > 0
                        });
                    }
                }

                result.Add(menuItem);
            }
        }
        catch (SquareApiException ex)
        {
            _logger.LogError("Square Catalog API error {StatusCode}: {Body}", ex.StatusCode, ex.Body);
        }

        return result;
    }

    public async Task<SubmitOrderResponseDto> CreateOrderAsync(SubmitOrderRequestDto request)
    {
        var locationId = _config["Square:LocationId"] ?? string.Empty;

        if (string.IsNullOrEmpty(locationId))
        {
            return new SubmitOrderResponseDto
            {
                Success = false,
                Message = "Square Location ID is not configured."
            };
        }

        try
        {
            var lineItems = request.Order.LineItems.Select(li => new OrderLineItem
            {
                Quantity = li.Quantity.ToString(),
                Name = li.Name,
                BasePriceMoney = new Money
                {
                    Amount = (long)(li.UnitPrice * 100),
                    Currency = Currency.Usd
                },
                Note = li.SpecialInstructions
            }).ToList();

            var response = await _squareClient.Orders.CreateAsync(new CreateOrderRequest
            {
                Order = new Order
                {
                    LocationId = locationId,
                    ReferenceId = request.TableId,
                    LineItems = lineItems,
                    Fulfillments =
                    [
                        new Fulfillment
                        {
                            Type = FulfillmentType.Pickup,
                            PickupDetails = new FulfillmentPickupDetails
                            {
                                ScheduleType = FulfillmentPickupDetailsScheduleType.Asap,
                                Note = request.SpecialInstructions
                            }
                        }
                    ]
                },
                IdempotencyKey = Guid.NewGuid().ToString()
            });

            return new SubmitOrderResponseDto
            {
                Success = true,
                SquareOrderId = response.Order?.Id,
                Message = "Order submitted successfully!"
            };
        }
        catch (SquareApiException ex)
        {
            var errorMsg = string.Join(", ", ex.Errors?.Select(e => e.Detail) ?? []);
            var body = ex.Body?.ToString() ?? string.Empty;
            _logger.LogError("Square Orders API error {StatusCode}: {Body}", ex.StatusCode, body);
            return new SubmitOrderResponseDto { Success = false, Message = string.IsNullOrEmpty(errorMsg) ? body : errorMsg };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Square order");
            return new SubmitOrderResponseDto
            {
                Success = false,
                Message = $"Failed to submit order: {ex.Message}"
            };
        }
    }

    public async Task<List<KitchenOrderDto>> GetKitchenOrdersAsync()
    {
        var locationId = _config["Square:LocationId"] ?? string.Empty;
        var result = new List<KitchenOrderDto>();

        try
        {
            var response = await _squareClient.Orders.SearchAsync(new SearchOrdersRequest
            {
                LocationIds = [locationId],
                Query = new SearchOrdersQuery
                {
                    Filter = new SearchOrdersFilter
                    {
                        StateFilter = new SearchOrdersStateFilter
                        {
                            States = [OrderState.Open]
                        }
                    },
                    Sort = new SearchOrdersSort
                    {
                        SortField = SearchOrdersSortField.CreatedAt,
                        SortOrder = SortOrder.Asc
                    }
                }
            });

            var orders = response.Orders?.ToList() ?? [];
            _logger.LogInformation("Kitchen search returned {Count} orders", orders.Count);

            foreach (var order in orders)
            {
                DateTime createdAt = DateTime.UtcNow;
                if (order.CreatedAt != null)
                    DateTime.TryParse(order.CreatedAt.ToString(), null,
                        System.Globalization.DateTimeStyles.RoundtripKind, out createdAt);

                result.Add(new KitchenOrderDto
                {
                    Id = order.Id ?? string.Empty,
                    TableId = order.ReferenceId ?? "Unknown",
                    CreatedAt = createdAt,
                    Version = order.Version ?? 1,
                    Note = order.Fulfillments?.FirstOrDefault()?.PickupDetails?.Note,
                    LineItems = order.LineItems?.Select(li => new KitchenLineItemDto
                    {
                        Name = li.Name ?? string.Empty,
                        Quantity = int.TryParse(li.Quantity, out var q) ? q : 1,
                        Note = li.Note
                    }).ToList() ?? []
                });
            }
        }
        catch (SquareApiException ex)
        {
            _logger.LogError("Square Search Orders error {StatusCode}: {Body}", ex.StatusCode, ex.Body?.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in GetKitchenOrdersAsync");
        }

        return result;
    }

    public async Task<bool> CompleteOrderAsync(string orderId, int version)
    {
        var locationId = _config["Square:LocationId"] ?? string.Empty;
        _logger.LogInformation("Completing order {OrderId} version {Version}", orderId, version);

        try
        {
            var response = await _squareClient.Orders.UpdateAsync(new UpdateOrderRequest
            {
                OrderId = orderId,
                Order = new Order
                {
                    LocationId = locationId,
                    State = OrderState.Completed,
                    Version = version
                },
                IdempotencyKey = Guid.NewGuid().ToString()
            });

            _logger.LogInformation("Order {OrderId} updated — state now: {State}", orderId, response.Order?.State);
            return true;
        }
        catch (SquareApiException ex)
        {
            _logger.LogError("Square Complete Order error {StatusCode}: {Body}", ex.StatusCode, ex.Body?.ToString());
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error completing order {OrderId}", orderId);
            return false;
        }
    }

}
