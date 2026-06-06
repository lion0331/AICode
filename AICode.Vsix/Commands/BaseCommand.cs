using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace AICode.Vsix.Commands
{
    internal abstract class BaseCommand
    {
        protected readonly AsyncPackage Package;
        protected readonly int CommandId;
        protected readonly Guid CommandSet;

        protected BaseCommand(AsyncPackage package, Guid commandSet, int commandId)
        {
            Package = package ?? throw new ArgumentNullException(nameof(package));
            CommandSet = commandSet;
            CommandId = commandId;
        }

        protected async Task InitializeAsync()
        {
            if (await Package.GetServiceAsync(typeof(IMenuCommandService)) is IMenuCommandService commandService)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(Execute, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        protected abstract void Execute(object sender, EventArgs e);
    }
}
