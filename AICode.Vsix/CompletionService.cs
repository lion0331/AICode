using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

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

        public async Task<CompletionContext> GetCompletionContextAsync(IAsyncCompletionSession session, 
            CompletionTrigger trigger, SnapshotPoint triggerLocation, SnapshotSpan applicableToSpan, 
            CancellationToken token)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(token);
            
            var engine = AICodePackage.Instance?.Engine;
            if (engine == null || !AICodePackage.Instance.Settings.EnableInlineCompletion)
            {
                return CompletionContext.Empty;
            }

            // 获取文件信息
            var document = triggerLocation.Snapshot.TextBuffer.GetRelatedDocuments().FirstOrDefault();
            string filePath = document?.FilePath ?? "unknown.cpp";
            string language = document?.ContentType.TypeName.ToLower() ?? "cpp";

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
            var tcs = new TaskCompletionSource<CompletionResponse>();
            engine.GetCodeCompletionAsync(request, response => tcs.SetResult(response));
            
            var response = await tcs.Task.WithCancellation(token);
            
            if (!response.Success || string.IsNullOrEmpty(response.CompletionText))
            {
                return CompletionContext.Empty;
            }

            // 创建补全项
            var completionItem = new CompletionItem(
                displayText: "AI: " + response.CompletionText.TrimStart().Split('\n')[0].Substring(0, Math.Min(50, response.CompletionText.Length)),
                source: this,
                icon: null,
                iconAutomationText: "AI Completion",
                filterText: response.CompletionText,
                insertText: response.CompletionText,
                description: new System.Collections.Generic.List<CompletionDescription>
                {
                    new CompletionDescription($"由 {response.ModelUsed} 生成\n\n{response.CompletionText}")
                },
                attributeIcons: null,
                commitCharacters: _triggerCharacters,
                applicableToSpan: applicableToSpan,
                properties: null);

            return new CompletionContext(new[] { completionItem }, applicableToSpan);
        }

        public Task<object> GetDescriptionAsync(IAsyncCompletionSession session, CompletionItem item, 
            CancellationToken token)
        {
            return Task.FromResult<object>(item.Description.FirstOrDefault());
        }

        public bool ShouldTriggerCompletion(ITextView textView, SnapshotPoint triggerLocation, 
            CompletionTrigger trigger, char? typedChar)
        {
            if (!AICodePackage.Instance.Settings.EnableInlineCompletion)
                return false;

            // 只在特定触发字符时触发
            if (typedChar.HasValue && !_triggerCharacters.Contains(typedChar.Value))
                return false;

            // 只在C++和C#文件中触发
            var document = triggerLocation.Snapshot.TextBuffer.GetRelatedDocuments().FirstOrDefault();
            if (document == null)
                return false;

            string contentType = document.ContentType.TypeName.ToLower();
            return contentType == "c/c++" || contentType == "csharp";
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
