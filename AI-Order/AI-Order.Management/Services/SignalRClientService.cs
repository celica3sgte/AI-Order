using Microsoft.AspNetCore.SignalR.Client;

namespace AI_Order.Management.Services;

public class SignalRClientService : IAsyncDisposable
{
    private const int RetryDelaySeconds = 15;

    private HubConnection? _hubConnection;
    private string _groupName = "";
    private readonly string _hubUrl;
    private readonly OrderNotificationService _notifications;
    private readonly CancellationTokenSource _cts = new();
    private bool _isStarting;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public SignalRClientService(IConfiguration configuration, OrderNotificationService notifications)
    {
        _notifications = notifications;
        _hubUrl = configuration["ApiHubUrl"]
            ?? throw new InvalidOperationException("ApiHubUrl is not configured.");
    }

    public async Task StartAsync(string groupName)
    {
        if (_isStarting || IsConnected) return;

        _isStarting = true;
        _groupName = groupName;

        try
        {
            while (!IsConnected && !_cts.IsCancellationRequested)
            {
                HubConnection? attempt = null;
                try
                {
                    attempt = new HubConnectionBuilder()
                        .WithUrl(_hubUrl)
                        .WithAutomaticReconnect()
                        .Build();

                    attempt.On<string>("ReceiveOrderUpdate", userId =>
                    {
                        try { _notifications.NotifyOrderUpdated(userId); }
                        catch (Exception ex) { Console.WriteLine($"[SignalR] handler error: {ex.Message}"); }
                    });

                    attempt.Reconnected += async _ =>
                    {
                        try { await JoinGroupAsync(attempt, _groupName); }
                        catch (Exception ex) { Console.WriteLine($"[SignalR] rejoin group error: {ex.Message}"); }
                    };

                    await attempt.StartAsync(_cts.Token);

                    if (_hubConnection is not null)
                        await _hubConnection.DisposeAsync();
                    _hubConnection = attempt;

                    await JoinGroupAsync(_hubConnection, groupName);
                    Console.WriteLine($"[SignalR] Connected and joined group '{groupName}'.");
                    return;
                }
                catch (OperationCanceledException)
                {
                    if (attempt is not null) await attempt.DisposeAsync();
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SignalR] failed: {ex.Message}. Retrying in {RetryDelaySeconds}s...");
                    if (attempt is not null) await attempt.DisposeAsync();
                }

                try { await Task.Delay(TimeSpan.FromSeconds(RetryDelaySeconds), _cts.Token); }
                catch (OperationCanceledException) { return; }
            }
        }
        finally
        {
            _isStarting = false;
        }
    }

    public async Task StopAsync()
    {
        if (_hubConnection is not null)
        {
            try { await _hubConnection.SendAsync("RemoveFromGroup", _groupName); }
            catch { }
            await _hubConnection.StopAsync();
        }
    }

    private static async Task JoinGroupAsync(HubConnection connection, string groupName)
    {
        if (connection.State != HubConnectionState.Connected) return;
        try { await connection.SendAsync("AddToGroup", groupName); }
        catch (Exception ex) { Console.WriteLine($"[SignalR] AddToGroup error: {ex.Message}"); }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        if (_hubConnection is not null)
        {
            try { await _hubConnection.StopAsync(); } catch { }
            await _hubConnection.DisposeAsync();
        }
        _cts.Dispose();
    }
}
