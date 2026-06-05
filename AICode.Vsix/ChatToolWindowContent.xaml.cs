using AICode.Services;
using AICodeAssistant.Services;
using Microsoft.VisualStudio.Extensibility;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace AICodeAssistant.ToolWindows;

public partial class ChatToolWindowContent : UserControl
{
    private readonly ICoreService _coreService;
    private readonly IExtensibility _extensibility;
    private CancellationTokenSource? _cancellationTokenSource;

    public ChatToolWindowContent(ICoreService coreService, IExtensibility extensibility)
    {
        InitializeComponent();
        _coreService = coreService;
        _extensibility = extensibility;
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        var userInput = InputTextBox.Text.Trim();
        if (string.IsNullOrEmpty(userInput))
            return;

        InputTextBox.Clear();
        _cancellationTokenSource = new CancellationTokenSource();

        AddMessage("你", userInput, Brushes.LightBlue);

        var aiMessageBlock = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 5, 0, 5) };
        var aiMessage = new Run();
        aiMessageBlock.Inlines.Add(aiMessage);
        AddMessage("AI", aiMessageBlock, Brushes.LightGreen);

        try
        {
            var context = await GetCurrentCodeContextAsync();
            var fullPrompt = $"当前代码上下文：\n{context}\n\n用户请求：{userInput}";

            await foreach (var chunk in _coreService.GenerateStreamAsync(fullPrompt, _cancellationTokenSource.Token))
            {
                aiMessage.Text += chunk;
                ChatScrollViewer.ScrollToEnd();
            }
        }
        catch (OperationCanceledException)
        {
            aiMessage.Text += "\n\n[已取消]";
        }
        catch (Exception ex)
        {
            aiMessage.Text += $"\n\n错误：{ex.Message}";
        }
    }

    private void InputTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter && !e.KeyboardDevice.IsKeyDown(System.Windows.Input.Key.LeftShift))
        {
            e.Handled = true;
            SendButton_Click(sender, e);
        }
    }

    private void AddMessage(string sender, object content, Brush background)
    {
        var border = new Border
        {
            Background = background,
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 5, 0, 5)
        };

        var stackPanel = new StackPanel();
        stackPanel.Children.Add(new TextBlock { Text = sender, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) });

        if (content is string text)
        {
            stackPanel.Children.Add(new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap });
        }
        else if (content is UIElement element)
        {
            stackPanel.Children.Add(element);
        }

        border.Child = stackPanel;
        ChatPanel.Children.Add(border);
        ChatScrollViewer.ScrollToEnd();
    }

    private async Task<string> GetCurrentCodeContextAsync()
    {
        try
        {
            var document = await _extensibility.Documents().GetActiveDocumentAsync(CancellationToken.None);
            if (document == null)
                return "无活动文档";

            var textDocument = await document.GetTextDocumentAsync(CancellationToken.None);
            var text = await textDocument.GetTextAsync(CancellationToken.None);
            var cursorPosition = await document.GetCursorPositionAsync(CancellationToken.None);

            var lines = text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            var startLine = Math.Max(0, cursorPosition.Line - 100);
            var endLine = Math.Min(lines.Length, cursorPosition.Line + 50);

            return string.Join(Environment.NewLine, lines.Skip(startLine).Take(endLine - startLine));
        }
        catch
        {
            return "无法获取代码上下文";
        }
    }
}
