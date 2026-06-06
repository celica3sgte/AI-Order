# 🍽️ AI-Order

AI-powered restaurant ordering system using Claude, Blazor WebAssembly, and Square POS.

## Projects

| Project | Description |
|---|---|
| `AI-Order.Api` | ASP.NET Core Web API — hosts Claude & Square integrations |
| `AI-Order.Client` | Blazor WebAssembly — voice/chat ordering UI |
| `AI-Order.Shared` | Shared DTOs used by both projects |

## Architecture

```
[Browser: Blazor WASM]
  ↕ Voice (Web Speech API / JS Interop)
  ↕ HTTP (chat, menu, orders)
[API: ASP.NET Core]
  ↕ Anthropic API (Claude)
  ↕ Square API (Catalog + Orders)
```

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9)
- Anthropic API key → https://console.anthropic.com
- Square Developer account → https://developer.squareup.com

## Configuration

Edit `AI-Order.Api/appsettings.json` (or use User Secrets in dev):

```json
{
  "Anthropic": {
    "ApiKey": "sk-ant-...",
    "Model": "claude-sonnet-4-20250514"
  },
  "Square": {
    "AccessToken": "EAA...",
    "LocationId": "YOUR_LOCATION_ID",
    "Environment": "Sandbox"
  },
  "Restaurant": {
    "Name": "My Restaurant",
    "TaxRate": 0.0825
  }
}
```

> ⚠️ Never commit real API keys. Use `dotnet user-secrets` for local dev:
> ```
> cd AI-Order.Api
> dotnet user-secrets set "Anthropic:ApiKey" "sk-ant-..."
> dotnet user-secrets set "Square:AccessToken" "EAA..."
> dotnet user-secrets set "Square:LocationId" "..."
> ```

## Running Locally

Open two terminals:

**Terminal 1 — API:**
```bash
cd AI-Order.Api
dotnet run
# Runs on https://localhost:7000
```

**Terminal 2 — Client:**
```bash
cd AI-Order.Client
dotnet run
# Runs on https://localhost:7001
```

Then open https://localhost:7001 in Chrome or Edge (required for Web Speech API).

## Key Features

- 🎤 **Voice input** via browser Web Speech API
- 🔊 **Voice responses** via browser SpeechSynthesis
- 🤖 **Claude AI** understands natural language orders, answers ingredient/allergy questions
- 📋 **Live order panel** updates as Claude parses your order
- 🟦 **Square POS integration** — menu synced from Catalog API, orders pushed to kitchen
- 📡 **Streaming responses** — Claude tokens stream in real time via SSE

## Menu Data

On startup, the API loads your menu from Square's Catalog API. In development or if Square
isn't configured, it falls back to a built-in demo menu you can customize in:
`AI-Order.Api/Services/MenuService.cs` → `GetFallbackMenu()`

## Project Structure

```
AI-Order/
├── AI-Order.sln
├── AI-Order.Api/
│   ├── Controllers/
│   │   ├── ChatController.cs       # /api/chat (standard + streaming)
│   │   ├── MenuController.cs       # /api/menu
│   │   └── OrdersController.cs     # /api/orders/submit
│   ├── Services/
│   │   ├── ClaudeService.cs        # Anthropic API integration
│   │   ├── MenuService.cs          # Menu cache + Square sync
│   │   └── SquareService.cs        # Square Catalog & Orders APIs
│   ├── appsettings.json
│   └── Program.cs
├── AI-Order.Client/
│   ├── Pages/
│   │   └── OrderPage.razor         # Main chat + order UI
│   ├── Services/
│   │   ├── OrderingService.cs      # API client
│   │   └── SpeechService.cs        # JS interop for speech
│   ├── wwwroot/
│   │   ├── css/app.css
│   │   ├── js/speech.js            # Web Speech API wrapper
│   │   └── index.html
│   └── Program.cs
└── AI-Order.Shared/
    └── Models/Dtos.cs              # Shared request/response models
```

## Next Steps

- [ ] Add customer authentication / loyalty
- [ ] Payment via Square Web Payments SDK
- [ ] Multi-language support
- [ ] Admin dashboard for menu management
- [ ] Order history and receipts
