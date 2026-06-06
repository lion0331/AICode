using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace AICode.Vsix
{
    [Guid("1A2B3C4D-5E6F-7A8B-9C0D-1E2F3A4B5C6D")]
    public class AICodeToolWindow : ToolWindowPane
    {
        private readonly AICodeToolWindowControl _control;

        public AICodeToolWindow() : base(null)
        {
            Caption = "AICode Assistant";
            BitmapImageMoniker = Microsoft.VisualStudio.Imaging.KnownMonikers.Code;
            _control = new AICodeToolWindowControl();
            Content = _control;
        }

        public void SetSelectedCode(string code, string filePath, int startLine, int endLine)
        {
            _control.SetSelectedCode(code, filePath, startLine, endLine);
        }
    }
}
