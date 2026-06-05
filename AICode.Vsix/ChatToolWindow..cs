using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.IO.Packaging;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace AICode.Vsix
{
    [Guid(ChatToolWindowGuidString)]
    public class ChatToolWindow : ToolWindowPane
    {
        public const string ChatToolWindowGuidString = "87654321-4321-4321-4321-BA9876543210"; // 替换为自己的GUID

        public ChatToolWindow() : base(null)
        {
            Caption = "AI Code Chat";
            Content = new ChatToolWindowContent(); // 关联XAML内容
        }

        // 工具窗口命令（用于打开窗口）
        internal static class ChatToolWindowCommand
        {
            public static async System.Threading.Tasks.Task InitializeAsync(Package package)
            {
                var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
                if (commandService == null) return;

                var cmdID = new CommandID(new Guid(PackageGuidString), 0x0100); // 命令ID
                var menuCmd = new MenuCommand(ShowToolWindow, cmdID);
                commandService.AddCommand(menuCmd);
            }

            private static void ShowToolWindow(object sender, EventArgs e)
            {
                var package = ((MenuCommand)sender).CommandService?.Site as Package;
                if (package == null) return;

                var window = package.FindToolWindow(typeof(ChatToolWindow), 0, true);
                if (window?.Frame == null)
                    throw new NotSupportedException("无法创建工具窗口");

                var windowFrame = (IVsWindowFrame)window.Frame;
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
            }
        }
    }
}