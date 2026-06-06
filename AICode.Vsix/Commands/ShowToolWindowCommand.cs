using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace AICode.Vsix.Commands
{
    internal sealed class ShowToolWindowCommand : BaseCommand
    {
        private static readonly Guid CommandSet = new Guid("1a2b3c4d-5e6f-7g8h-9i0j-1k2l3m4n5o6p");
        private const int CommandId = 0x0100;

        private ShowToolWindowCommand(AsyncPackage package) : base(package, CommandSet, CommandId)
        {
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            var command = new ShowToolWindowCommand(package);
            await command.InitializeAsync();
        }

        protected override void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            var window = Package.FindToolWindow(typeof(AICodeToolWindow), 0, true);
            if (window?.Frame == null)
            {
                throw new NotSupportedException("无法创建AICode工具窗口");
            }

            Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame windowFrame = (Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
    }
}
