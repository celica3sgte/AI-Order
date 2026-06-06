using AI_Order.Client;
using AI_Order.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// API base URL - points to the ASP.NET Core API project
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7000";

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddScoped<IOrderingService, OrderingService>();
builder.Services.AddScoped<ISpeechService, SpeechService>();

await builder.Build().RunAsync();
