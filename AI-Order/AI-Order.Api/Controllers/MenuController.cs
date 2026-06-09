using AI_Order.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AI_Order.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MenuController : ControllerBase
{
    private readonly IMenuService _menuService;

    public MenuController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMenu([FromQuery] string? userId = null)
    {
        var menu = await _menuService.GetMenuAsync(userId);
        return Ok(menu);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetMenuItem(string id, [FromQuery] string? userId = null)
    {
        var item = await _menuService.GetMenuItemAsync(id, userId);
        if (item == null) return NotFound();
        return Ok(item);
    }
}
