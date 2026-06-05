using AICode.Services;
using AICodeAssistant.Services;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.ToolWindows;
using System.Windows;

namespace AICodeAssistant.ToolWindows;

public class ChatToolWindow : ToolWindow
{
    private readonly ICoreService _coreService;
    private readonly IExtensibility _extensibility;

    public ChatToolWindow(ICoreService coreService, IExtensibility extensibility)
    {
        _coreService = coreService;
        _extensibility = extensibility;
    }

    public override Task<FrameworkElement> CreateToolWindowContentAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<FrameworkElement>(new ChatToolWindowContent(_coreService, _extensibility));
    }
}