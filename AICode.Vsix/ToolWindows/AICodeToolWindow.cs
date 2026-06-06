using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace AICode.Vsix
{
    [Guid("1a2b3c4d-5e6f-7g8h-9i0j-1k2l3m4n5o6p")]
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
