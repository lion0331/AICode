using AICode.Services;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.IO.Packaging;

namespace AICode.Vsix
{
    internal sealed class GenerateCodeCommand
    {
        private readonly Package _package;
        public const int CommandId = 0x0200;
        public static readonly Guid CommandSet = new Guid(AICodePackage.PackageGuidString);

        private GenerateCodeCommand(Package package)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            if (package.GetService(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(Execute, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        public static GenerateCodeCommand Instance { get; private set; }

        public static async System.Threading.Tasks.Task InitializeAsync(Package package)
        {
            await System.Threading.Tasks.Task.Run(() => Instance = new GenerateCodeCommand(package));
        }

        private void Execute(object sender, EventArgs e)
        {
            // 代码生成逻辑（示例：弹出提示）
            var uiShell = _package.GetService(typeof(SVsUIShell)) as IVsUIShell;
            var clsid = Guid.Empty;
            int result;
            uiShell.ShowMessageBox(
                0, ref clsid, "AICode", "开始生成AI代码...", string.Empty,
                0, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                OLEMSGICON.OLEMSGICON_INFO, 0, out result);

            // 调用CoreService生成代码
            var coreService = new CoreService();
            coreService.GenerateCode();
        }
    }
}