using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace AICode.Vsix.Commands
{
    internal sealed class GenerateCodeCommand : BaseCommand
    {
        private static readonly Guid CommandSet = new Guid("1a2b3c4d-5e6f-7g8h-9i0j-1k2l3m4n5o6p");
        private const int CommandId = 0x0102;

        private GenerateCodeCommand(AsyncPackage package) : base(package, CommandSet, CommandId)
        {
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            var command = new GenerateCodeCommand(package);
            await command.InitializeAsync();
        }

        protected override void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            var textView = GetActiveTextView();
            if (textView == null)
                return;

            var window = Package.FindToolWindow(typeof(AICodeToolWindow), 0, true) as AICodeToolWindow;
            if (window?.Frame == null)
                return;

            var document = textView.TextBuffer.GetRelatedDocuments().FirstOrDefault();
            string filePath = document?.FilePath ?? "unknown.cpp";

            // 获取选中的代码
            string selectedCode = string.Empty;
            int startLine = 0;
            int endLine = 0;

            if (textView.Selection.SelectedSpans.Count > 0)
            {
                var span = textView.Selection.SelectedSpans[0];
                selectedCode = span.GetText();
                startLine = span.Start.GetContainingLine().LineNumber + 1;
                endLine = span.End.GetContainingLine().LineNumber + 1;
            }

            window.SetSelectedCode(selectedCode, filePath, startLine, endLine);

            // 显示工具窗口
            Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame windowFrame = (Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        private ITextView GetActiveTextView()
        {
            return ServiceProvider.GlobalProvider.GetService(typeof(Microsoft.VisualStudio.TextManager.Interop.SVsTextManager)) is Microsoft.VisualStudio.TextManager.Interop.IVsTextManager textManager
                && textManager.GetActiveView(1, null, out var textView) == Microsoft.VisualStudio.VSConstants.S_OK
                ? textView as ITextView
                : null;
        }
    }
}
