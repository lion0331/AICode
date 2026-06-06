using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace AICode.Vsix.Commands
{
    internal sealed class SettingsCommand : BaseCommand
    {
        private new static readonly Guid CommandSet = new Guid("1A2B3C4D-5E6F-4A8B-9C0D-1E2F3A4B5C6D");
        private new const int CommandId = 0x0101;

        private SettingsCommand(AsyncPackage package) : base(package, CommandSet, CommandId)
        {
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            var command = new SettingsCommand(package);
            await command.InitializeAsync();
        }

        protected override void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Package.ShowOptionPage(typeof(SettingsPage));
        }
    }
}
