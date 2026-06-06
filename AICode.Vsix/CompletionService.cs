using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AICode.Vsix
{
    [Export(typeof(IAsyncCompletionSourceProvider))]
    [Name("AICode Completion Provider")]
    [ContentType("C/C++")]
    [ContentType("CSharp")]
    [Order(Before = "default")]
    internal class CompletionSourceProvider : IAsyncCompletionSourceProvider
    {
        [Import]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        public IAsyncCompletionSource GetOrCreate(ITextView textView)
        {
            if (textView == null)
                throw new ArgumentNullException(nameof(textView));

            return textView.Properties.GetOrCreateSingletonProperty(() =>
                new CompletionSource(textView, NavigatorService));
        }
    }

    internal class CompletionSource : IAsyncCompletionSource
    {
        private readonly ITextView _textView;
        private readonly ITextStructureNavigatorSelectorService _navigatorService;
        private static readonly char[] _triggerCharacters = { '.', '>', ':', '(', '[', '{', '=', ' ', '\t' };

        public CompletionSource(ITextView textView, ITextStructureNavigatorSelectorService navigatorService)
        {
            _textView = textView;
            _navigatorService = navigatorService;
        }

        public CompletionStartData InitializeCompletion(CompletionTrigger trigger, SnapshotPoint triggerLocation, CancellationToken token)
        {
            if (!ShouldTriggerCompletion(_textView, triggerLocation, trigger, trigger.Character))
            {
                return CompletionStartData.DoesNotParticipateInCompletion;
            }

            // 找到当前单词的起始位置
            var navigator = _navigatorService.GetTextStructureNavigator(triggerLocation.Snapshot.TextBuffer);
            var extent = navigator.GetExtentOfWord(triggerLocation - 1);
            var applicableToSpan = new SnapshotSpan(extent.Span.Start, triggerLocation);

            return new CompletionStartData(CompletionParticipation.ProvidesItems, applicableToSpan);
        }

        public async Task<CompletionContext> GetCompletionContextAsync(IAsyncCompletionSession session,
            CompletionTrigger trigger, SnapshotPoint triggerLocation, SnapshotSpan applicableToSpan,
            CancellationToken token)
        {
            var engine = AICodePackage.Instance?.Engine;
            if (engine == null || !AICodePackage.Instance.Settings.EnableInlineCompletion)
            {
                return CompletionContext.Empty;
            }

            // 获取文件信息
            triggerLocation.Snapshot.TextBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument document);
            string filePath = document?.FilePath ?? "unknown.cpp";
            string language = GetLanguageFromFilePath(filePath);

            // 获取光标前后的代码
            int cursorPosition = triggerLocation.Position;
            string codeBeforeCursor = triggerLocation.Snapshot.GetText(0, cursorPosition);
            string codeAfterCursor = triggerLocation.Snapshot.GetText(cursorPosition,
                triggerLocation.Snapshot.Length - cursorPosition);

            // 获取行号和列号
            var line = triggerLocation.GetContainingLine();
            int lineNumber = line.LineNumber + 1;
            int columnNumber = triggerLocation.Position - line.Start.Position + 1;

            // 创建请求
            var request = new CompletionRequest
            {
                FilePath = filePath,
                Language = language,
                CodeBeforeCursor = codeBeforeCursor,
                CodeAfterCursor = codeAfterCursor,
                LineNumber = lineNumber,
                ColumnNumber = columnNumber,
                MaxTokens = AICodePackage.Instance.Settings.MaxCompletionTokens,
                Temperature = AICodePackage.Instance.Settings.CompletionTemperature
            };

            // 异步获取补全
            var tcs = new TaskCompletionSource<CompletionResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var cancellationRegistration = token.Register(() => tcs.TrySetCanceled(token));
            engine.GetCodeCompletionAsync(request, response => tcs.TrySetResult(response));

            // 等待任务完成或取消
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, token));
            if (completedTask != tcs.Task)
            {
                return CompletionContext.Empty;
            }

            var response = await tcs.Task.ConfigureAwait(false);

            if (!response.Success || string.IsNullOrEmpty(response.CompletionText))
            {
                return CompletionContext.Empty;
            }

            // 创建补全项
            var completionText = response.CompletionText;
            var firstLine = completionText.TrimStart().Split('\n').FirstOrDefault() ?? string.Empty;
            var previewLength = Math.Min(50, firstLine.Length);
            var displayText = "AI: " + firstLine.Substring(0, previewLength);

            var completionItem = new CompletionItem(
                displayText: displayText,
                source: this,
                icon: null,
                filters: ImmutableArray<CompletionFilter>.Empty,
                suffix: "",
                insertText: completionText,
                sortText: displayText,
                filterText: completionText,
                attributeIcons: ImmutableArray<ImageElement>.Empty);

            // 添加详细描述
            completionItem.Properties["Description"] = $"由 {response.ModelUsed} 生成\n\n{completionText}";

            return new CompletionContext(ImmutableArray.Create(completionItem));
        }

        public Task<object> GetDescriptionAsync(IAsyncCompletionSession session, CompletionItem item,
            CancellationToken token)
        {
            if (item.Properties.ContainsProperty("Description"))
            {
                return Task.FromResult(item.Properties["Description"]);
            }
            return Task.FromResult<object>(null);
        }

        public bool ShouldTriggerCompletion(ITextView textView, SnapshotPoint triggerLocation,
            CompletionTrigger trigger, char? typedChar)
        {
            if (AICodePackage.Instance == null || !AICodePackage.Instance.Settings.EnableInlineCompletion)
                return false;

            // 只在特定触发字符时触发
            if (typedChar.HasValue && !_triggerCharacters.Contains(typedChar.Value))
                return false;

            // 只在C++和C#文件中触发
            triggerLocation.Snapshot.TextBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument document);
            if (document == null)
                return false;

            string language = GetLanguageFromFilePath(document.FilePath);
            return language == "cpp" || language == "csharp";
        }

        private string GetLanguageFromFilePath(string filePath)
        {
            if (filePath.EndsWith(".cpp", StringComparison.OrdinalIgnoreCase) ||
                filePath.EndsWith(".h", StringComparison.OrdinalIgnoreCase) ||
                filePath.EndsWith(".hpp", StringComparison.OrdinalIgnoreCase))
            {
                return "cpp";
            }
            else if (filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                return "csharp";
            }
            return "unknown";
        }
    }

    internal static class CompletionService
    {
        public static Task InitializeAsync(AsyncPackage package)
        {
            // MEF组件会自动注册，这里不需要额外操作
            return Task.CompletedTask;
        }
    }
}
