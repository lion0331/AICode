using AICode.Services;
using AICodeAssistant.Services;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using System.Reflection.Metadata;

namespace AICodeAssistant.Commands;

[CommandIcon("Icon", IconSettings.IconAndText)]
public class GenerateCodeCommand : Command
{
    private readonly ICoreService _coreService;
    private readonly IExtensibility _extensibility;

    public GenerateCodeCommand(ICoreService coreService, IExtensibility extensibility)
    {
        _coreService = coreService;
        _extensibility = extensibility;
    }

    public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
    {
        var document = await _extensibility.Documents().GetActiveDocumentAsync(cancellationToken);
        if (document == null)
            return;

        var selection = await document.GetSelectionAsync(cancellationToken);
        var selectedText = await selection.GetTextAsync(cancellationToken);

        string prompt;
        if (string.IsNullOrEmpty(selectedText))
        {
            prompt = "请在当前光标位置生成合适的代码";
        }
        else
        {
            prompt = $"请根据以下注释生成代码：\n{selectedText}";
        }

        var contextText = await GetCurrentCodeContextAsync(document, cancellationToken);
        var fullPrompt = $"当前代码上下文：\n{contextText}\n\n用户请求：{prompt}\n\n只输出代码，不要任何解释。";

        var generatedCode = await _coreService.GenerateAsync(fullPrompt, cancellationToken);

        var editPoint = await selection.CreateEditPointAsync(cancellationToken);
        if (!string.IsNullOrEmpty(selectedText))
        {
            await selection.DeleteAsync(cancellationToken);
        }
        await editPoint.InsertAsync(generatedCode, cancellationToken);
    }

    private async Task<string> GetCurrentCodeContextAsync(Document document, CancellationToken cancellationToken)
    {
        var textDocument = await document.GetTextDocumentAsync(cancellationToken);
        var text = await textDocument.GetTextAsync(cancellationToken);
        var cursorPosition = await document.GetCursorPositionAsync(cancellationToken);

        var lines = text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        var startLine = Math.Max(0, cursorPosition.Line - 50);
        var endLine = Math.Min(lines.Length, cursorPosition.Line + 20);

        return string.Join(Environment.NewLine, lines.Skip(startLine).Take(endLine - startLine));
    }
}