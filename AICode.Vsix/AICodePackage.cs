using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace AICode.Vsix
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid("9A5B3C7D-1E2F-4A6B-8C9D-0E1F2A3B4C5D")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(AICodeToolWindow), Style = VsDockStyle.Tabbed, Window = "DocumentWell", Orientation = ToolWindowOrientation.Right)]
    [ProvideOptionPage(typeof(SettingsPage), "AICode Assistant", "General", 0, 0, true)]
    public sealed class AICodePackage : AsyncPackage
    {
        public const string PackageGuidString = "9A5B3C7D-1E2F-4A6B-8C9D-0E1F2A3B4C5D";
        public static AICodePackage Instance { get; private set; }
        public AICodeEngine Engine { get; private set; }
        public SettingsPage Settings { get; private set; }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            
            Instance = this;
            
            // 加载设置
            Settings = (SettingsPage)GetDialogPage(typeof(SettingsPage));
            
            // 初始化引擎
            InitializeEngine();
            
            // 注册命令
            await Commands.ShowToolWindowCommand.InitializeAsync(this);
            await Commands.SettingsCommand.InitializeAsync(this);
            await Commands.GenerateCodeCommand.InitializeAsync(this);
            await Commands.RefactorCodeCommand.InitializeAsync(this);
            await Commands.ExplainCodeCommand.InitializeAsync(this);
            await Commands.FindIssuesCommand.InitializeAsync(this);
            
            // 注册编辑器补全服务
            await CompletionService.InitializeAsync(this);
        }

        private void InitializeEngine()
        {
            try
            {
                Engine = new AICodeEngine();
                
                if (!string.IsNullOrEmpty(Settings.ApiKey))
                {
                    bool success = Engine.Initialize(Settings.SelectedModel, Settings.ApiKey, Settings.ApiBaseUrl);
                    if (!success)
                    {
                        VsShellUtilities.ShowMessageBox(
                            this,
                            "AICode引擎初始化失败，请检查API密钥和网络连接。",
                            "AICode Assistant",
                            OLEMSGICON.OLEMSGICON_WARNING,
                            OLEMSGBUTTON.OLEMSGBUTTON_OK,
                            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    }
                }
            }
            catch (Exception ex)
            {
                VsShellUtilities.ShowMessageBox(
                    this,
                    $"AICode引擎初始化异常: {ex.Message}",
                    "AICode Assistant",
                            OLEMSGICON.OLEMSGICON_CRITICAL,
                            OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        public void ReinitializeEngine()
        {
            Engine?.Dispose();
            InitializeEngine();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Engine?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
