using AICode.Services;

namespace AICode.Services;

public class CoreService : ICoreService
{
    private readonly PipeClient _pipeClient = new();

    public bool Initialize(string apiKey, string model)
    {
        if (NativeMethods.IsCoreInitialized()) return true;
        return NativeMethods.InitializeCore(apiKey, model);
    }

    public void Shutdown()
    {
        NativeMethods.ShutdownCore();
    }

    public bool IsInitialized => NativeMethods.IsCoreInitialized();

    public IAsyncEnumerable<string> GenerateStreamAsync(string prompt, CancellationToken cancellationToken)
    {
        if (!IsInitialized)
        {
            throw new InvalidOperationException("核心服务未初始化，请先在工具-选项中配置API密钥");
        }

        return _pipeClient.GenerateStreamAsync(prompt, cancellationToken);
    }

    public Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken)
    {
        if (!IsInitialized)
        {
            throw new InvalidOperationException("核心服务未初始化，请先在工具-选项中配置API密钥");
        }

        return _pipeClient.GenerateAsync(prompt, cancellationToken);
    }
}