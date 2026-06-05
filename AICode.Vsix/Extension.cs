using AICode.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.ToolWindows;

namespace AICode;

[VisualStudioContribution]
public class AICodeExtension : Extension
{
    private ICoreService? _coreService;

    public override ExtensionConfiguration ExtensionConfiguration => new()
    {
        Metadata = new()
        {
            Id = "AICodeAssistant.Hybrid.9f8b7c6d-5e4f-3a2b-1c0d-9e8f7a6b5c4d",
            Version = this.ExtensionAssemblyVersion,
            PublisherName = "Your Name",
            DisplayName = "AI Code Assistant (Hybrid)",
            Description = "C# + C++混合架构的Visual Studio 2026 AI代码助手"
        }
    };

    protected override void InitializeServices(IServiceCollection services)
    {
        base.InitializeServices(services);
        services.AddSingleton<ICredentialService, CredentialService>();
        services.AddSingleton<ICoreService, CoreService>();
    }

    public override async Task OnInitializedAsync(CancellationToken cancellationToken)
    {
        await base.OnInitializedAsync(cancellationToken);

        _coreService = this.Extensibility.ServiceProvider.GetRequiredService<ICoreService>();

        var credentialService = this.Extensibility.ServiceProvider.GetRequiredService<ICredentialService>();
        var openAiKey = credentialService.GetApiKey("OpenAI");
        var anthropicKey = credentialService.GetApiKey("Anthropic");
        var defaultModel = credentialService.GetApiKey("DefaultModel") ?? "claude-3-5-sonnet-20241022";

        if (!string.IsNullOrEmpty(anthropicKey))
        {
            _coreService.Initialize(anthropicKey, defaultModel);
        }
        else if (!string.IsNullOrEmpty(openAiKey))
        {
            _coreService.Initialize(openAiKey, defaultModel);
        }
    }

    protected override void Dispose(bool disposing)
    {
        _coreService?.Shutdown();
        base.Dispose(disposing);
    }

    [VisualStudioContribution]
    public static ToolWindowConfiguration ChatToolWindow => new(
        toolWindowType: typeof(ChatToolWindow),
        title: "AI Code Assistant")
    {
        Placement = ToolWindowPlacement.Right,
        DockingStyle = DockingStyle.Tabbed,
        Width = 400
    };

    [VisualStudioContribution]
    public static CommandConfiguration GenerateCodeCommand => new(
        commandType: typeof(GenerateCodeCommand),
        displayName: "Generate Code with AI")
    {
        Placements = new[]
        {
            CommandPlacement.KnownPlacements.ToolsMenu,
            CommandPlacement.KnownPlacements.EditorContextMenu
        }
    };

    [VisualStudioContribution]
    public static OptionsPageConfiguration GeneralOptionsPage => new(
        optionsPageType: typeof(GeneralOptionsPage),
        category: "AI Code Assistant",
        pageName: "General")
    {
        Description = "配置AI代码助手的API密钥和模型设置"
    };
}