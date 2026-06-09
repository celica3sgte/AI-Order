namespace AI_Order.Shared.Models;

public class MenuItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public List<string> Ingredients { get; set; } = new();
    public List<string> Allergens { get; set; } = new();
    public bool IsAvailable { get; set; } = true;
    public List<ModifierGroupDto> ModifierGroups { get; set; } = new();
    public string? MainImage { get; set; }
    public string? Image1 { get; set; }
    public string? Image2 { get; set; }
    public string? Image3 { get; set; }
}

public class ModifierGroupDto
{
    public string Name { get; set; } = string.Empty;
    public bool Required { get; set; }
    public List<ModifierOptionDto> Options { get; set; } = new();
}

public class ModifierOptionDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal PriceModifier { get; set; }
}

public class OrderDto
{
    public string Id { get; set; } = string.Empty;
    public List<OrderLineItemDto> LineItems { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public string? CustomerName { get; set; }
    public string? SpecialInstructions { get; set; }
}

public class OrderLineItemDto
{
    public string MenuItemId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public List<string> SelectedModifiers { get; set; } = new();
    public string? SpecialInstructions { get; set; }
}

public enum OrderStatus
{
    Draft,
    Confirmed,
    Submitted,
    InProgress,
    Ready,
    Completed,
    Cancelled
}

public class ChatMessageDto
{
    public string Role { get; set; } = string.Empty; // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ChatRequestDto
{
    public List<ChatMessageDto> Messages { get; set; } = new();
    public OrderDto? CurrentOrder { get; set; }
    public string? UserId { get; set; }
}

public class ChatResponseDto
{
    public string Message { get; set; } = string.Empty;
    public OrderDto? UpdatedOrder { get; set; }
    public bool OrderReady { get; set; }
}

public class SubmitOrderRequestDto
{
    public OrderDto Order { get; set; } = new();
    public string? CustomerName { get; set; }
    public string? TableId { get; set; }
    public string? SpecialInstructions { get; set; }
    public string? AspNetUserId { get; set; }
}

public class TableOrder
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string AspNetUserId { get; set; } = string.Empty;
    public string TableId { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? SpecialInstructions { get; set; }
    public List<OrderLineItemDto> LineItems { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Submitted;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? SquareOrderId { get; set; }
}

public class SubmitOrderResponseDto
{
    public bool Success { get; set; }
    public string? SquareOrderId { get; set; }
    public string? Message { get; set; }
}

public class UpdateOrderStatusRequest
{
    public string AspNetUserId { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
}

public class KitchenOrderDto
{
    public string Id { get; set; } = string.Empty;
    public string TableId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int Version { get; set; } = 1;
    public List<KitchenLineItemDto> LineItems { get; set; } = new();
    public string? Note { get; set; }
}

public class KitchenLineItemDto
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Note { get; set; }
}
