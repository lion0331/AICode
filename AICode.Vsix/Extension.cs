using AICodeAssistant.Options;
using AICodeAssistant.ToolWindows;
using Microsoft.VisualStudio.Shell;
using System;
using System.IO.Packaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AICode.Vsix
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(ChatToolWindow), Style = VsDockStyle.Tabbed, Window = "{3AE79031-E1BC-11D0-8F78-00A0C9110057}")]
    [ProvideOptionPage(typeof(GeneralOptionsPage), "AICode", "General", 0, 0, true)]
    public sealed class AICodePackage : Package
    {
        public const string PackageGuidString = "12345678-1234-1234-1234-1234567890AB"; // 替换为自己的GUID

        protected override async System.Threading.Tasks.Task InitializeAsync(
            System.Threading.CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);
            await GenerateCodeCommand.InitializeAsync(this);
            await ChatToolWindowCommand.InitializeAsync(this); // 补充工具窗口命令初始化
        }
    }
}