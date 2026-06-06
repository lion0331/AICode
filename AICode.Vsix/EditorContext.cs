using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace AICode.Vsix
{
    internal static class EditorContext
    {
        public static ITextView GetActiveTextView()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (ServiceProvider.GlobalProvider.GetService(typeof(SVsTextManager)) is not IVsTextManager textManager)
            {
                return null;
            }

            if (textManager.GetActiveView(1, null, out IVsTextView viewAdapter) != Microsoft.VisualStudio.VSConstants.S_OK ||
                viewAdapter == null)
            {
                return null;
            }

            if (ServiceProvider.GlobalProvider.GetService(typeof(SComponentModel)) is not IComponentModel componentModel)
            {
                return null;
            }

            var editorAdaptersFactory = componentModel.GetService<IVsEditorAdaptersFactoryService>();
            return editorAdaptersFactory?.GetWpfTextView(viewAdapter);
        }
    }
}
