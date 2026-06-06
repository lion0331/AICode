using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace AICode.Vsix.Commands
{
    internal sealed class ExplainCodeCommand : BaseCommand
    {
        private new static readonly Guid CommandSet = new Guid("1A2B3C4D-5E6F-4A8B-9C0D-1E2F3A4B5C6D");
        private new const int CommandId = 0x0104;

        private ExplainCodeCommand(AsyncPackage package) : base(package, CommandSet, CommandId)
        {
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            var command = new ExplainCodeCommand(package);
            await command.InitializeAsync();
        }

        protected override void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            var textView = EditorContext.GetActiveTextView();
            if (textView == null)
                return;

            var window = Package.FindToolWindow(typeof(AICodeToolWindow), 0, true) as AICodeToolWindow;
            if (window?.Frame == null)
                return;

            textView.TextBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument document);
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

            if (string.IsNullOrEmpty(selectedCode))
            {
                Microsoft.VisualStudio.Shell.VsShellUtilities.ShowMessageBox(
                    Package,
                    "请先选择要解释的代码。",
                    "AICode Assistant",
                    Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_WARNING,
                    Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    Microsoft.VisualStudio.Shell.Interop.OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            window.SetSelectedCode(selectedCode, filePath, startLine, endLine);

            // 显示工具窗口
            Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame windowFrame = (Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
    }
}
