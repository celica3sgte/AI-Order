using AI_Order.Api.Hubs;
using AI_Order.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// CORS - allow Blazor WASM client and Management server
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorClient", policy =>
    {
        var origins = (builder.Configuration["AllowedOrigins"] ?? "https://localhost:7001")
            .Split(',', StringSplitOptions.RemoveEmptyEntries);
        policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// HttpClient for Anthropic API
builder.Services.AddHttpClient("Anthropic", client =>
{
    client.BaseAddress = new Uri("https://api.anthropic.com");
    client.DefaultRequestHeaders.Add("x-api-key", builder.Configuration["Anthropic:ApiKey"]);
    client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
}).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(2),
    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
});

// App services
builder.Services.AddSingleton<IMenuService, MenuService>();
builder.Services.AddScoped<IClaudeService, ClaudeService>();
builder.Services.AddSingleton<ISquareService, SquareService>();
builder.Services.AddSingleton<CosmosOrderService>();
builder.Services.AddSignalR();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("BlazorClient");
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();
app.MapHub<OrderHub>("/hubs/orders");
app.MapFallbackToFile("index.html");

app.Run();
