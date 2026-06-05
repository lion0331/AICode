namespace AICode.Services;

public interface ICoreService
{
    bool Initialize(string apiKey, string model);
    void Shutdown();
    bool IsInitialized { get; }
    IAsyncEnumerable<string> GenerateStreamAsync(string prompt, CancellationToken cancellationToken);
    Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken);
}