using Microsoft.JSInterop;

namespace AI_Order.Client.Services;

public interface ISpeechService
{
    Task<bool> IsSupportedAsync();
    Task StartListeningAsync(DotNetObjectReference<object> dotnetRef);
    Task StopListeningAsync();
    Task SpeakAsync(string text);
    Task StopSpeakingAsync();
}

public class SpeechService : ISpeechService
{
    private readonly IJSRuntime _js;

    public SpeechService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task<bool> IsSupportedAsync()
    {
        return await _js.InvokeAsync<bool>("SpeechInterop.isSupported");
    }

    public async Task StartListeningAsync(DotNetObjectReference<object> dotnetRef)
    {
        await _js.InvokeVoidAsync("SpeechInterop.startListening", dotnetRef);
    }

    public async Task StopListeningAsync()
    {
        await _js.InvokeVoidAsync("SpeechInterop.stopListening");
    }

    public async Task SpeakAsync(string text)
    {
        await _js.InvokeVoidAsync("SpeechInterop.speak", text);
    }

    public async Task StopSpeakingAsync()
    {
        await _js.InvokeVoidAsync("SpeechInterop.stopSpeaking");
    }
}
