using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace AICode.Vsix
{
    public partial class AICodeToolWindowControl : UserControl
    {
        private string _selectedCode;
        private string _filePath;
        private int _startLine;
        private int _endLine;

        public AICodeToolWindowControl()
        {
            InitializeComponent();
        }

        public void SetSelectedCode(string code, string filePath, int startLine, int endLine)
        {
            _selectedCode = code;
            _filePath = filePath;
            _startLine = startLine;
            _endLine = endLine;
            SelectedCodeTextBox.Text = code;
        }

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            var engine = AICodePackage.Instance?.Engine;
            if (engine == null)
            {
                ResultTextBox.Text = "AICode引擎未初始化，请先在设置中配置API密钥。";
                return;
            }

            string prompt = PromptTextBox.Text;
            if (string.IsNullOrEmpty(prompt))
            {
                ResultTextBox.Text = "请输入生成需求。";
                return;
            }

            ResultTextBox.Text = "正在生成代码...";
            GenerateButton.IsEnabled = false;

            try
            {
                var document = GetActiveDocument();
                string language = document?.ContentType.TypeName.ToLower() ?? "cpp";
                string[] contextFiles = string.IsNullOrEmpty(_filePath) ? Array.Empty<string>() : new[] { _filePath };

                var response = await System.Threading.Tasks.Task.Run(() => 
                    engine.GenerateCode(prompt, language, contextFiles));

                if (response.Success)
                {
                    ResultTextBox.Text = response.CompletionText;
                }
                else
                {
                    ResultTextBox.Text = $"生成失败: {response.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                ResultTextBox.Text = $"生成异常: {ex.Message}";
            }
            finally
            {
                GenerateButton.IsEnabled = true;
            }
        }

        private async void RefactorButton_Click(object sender, RoutedEventArgs e)
        {
            var engine = AICodePackage.Instance?.Engine;
            if (engine == null)
            {
                ResultTextBox.Text = "AICode引擎未初始化，请先在设置中配置API密钥。";
                return;
            }

            if (string.IsNullOrEmpty(_selectedCode))
            {
                ResultTextBox.Text = "请先在编辑器中选择要重构的代码。";
                return;
            }

            string instruction = PromptTextBox.Text;
            if (string.IsNullOrEmpty(instruction))
            {
                ResultTextBox.Text = "请输入重构指令。";
                return;
            }

            ResultTextBox.Text = "正在重构代码...";
            RefactorButton.IsEnabled = false;

            try
            {
                var response = await System.Threading.Tasks.Task.Run(() => 
                    engine.RefactorCode(_filePath, _selectedCode, instruction, _startLine, _endLine));

                if (response.Success)
                {
                    ResultTextBox.Text = response.CompletionText;
                }
                else
                {
                    ResultTextBox.Text = $"重构失败: {response.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                ResultTextBox.Text = $"重构异常: {ex.Message}";
            }
            finally
            {
                RefactorButton.IsEnabled = true;
            }
        }

        private async void ExplainButton_Click(object sender, RoutedEventArgs e)
        {
            var engine = AICodePackage.Instance?.Engine;
            if (engine == null)
            {
                ResultTextBox.Text = "AICode引擎未初始化，请先在设置中配置API密钥。";
                return;
            }

            if (string.IsNullOrEmpty(_selectedCode))
            {
                ResultTextBox.Text = "请先在编辑器中选择要解释的代码。";
                return;
            }

            ResultTextBox.Text = "正在解释代码...";
            ExplainButton.IsEnabled = false;

            try
            {
                var document = GetActiveDocument();
                string language = document?.ContentType.TypeName.ToLower() ?? "cpp";

                string explanation = await System.Threading.Tasks.Task.Run(() => 
                    engine.ExplainCode(_selectedCode, language));

                ResultTextBox.Text = explanation;
            }
            catch (Exception ex)
            {
                ResultTextBox.Text = $"解释异常: {ex.Message}";
            }
            finally
            {
                ExplainButton.IsEnabled = true;
            }
        }

        private async void FindIssuesButton_Click(object sender, RoutedEventArgs e)
        {
            var engine = AICodePackage.Instance?.Engine;
            if (engine == null)
            {
                ResultTextBox.Text = "AICode引擎未初始化，请先在设置中配置API密钥。";
                return;
            }

            if (string.IsNullOrEmpty(_selectedCode))
            {
                ResultTextBox.Text = "请先在编辑器中选择要检查的代码。";
                return;
            }

            ResultTextBox.Text = "正在检查代码问题...";
            FindIssuesButton.IsEnabled = false;

            try
            {
                var document = GetActiveDocument();
                string language = document?.ContentType.TypeName.ToLower() ?? "cpp";

                string issues = await System.Threading.Tasks.Task.Run(() => 
                    engine.FindCodeIssues(_selectedCode, language));

                ResultTextBox.Text = issues;
            }
            catch (Exception ex)
            {
                ResultTextBox.Text = $"检查异常: {ex.Message}";
            }
            finally
            {
                FindIssuesButton.IsEnabled = true;
            }
        }

        private async void InsertButton_Click(object sender, RoutedEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            
            var textView = GetActiveTextView();
            if (textView == null)
            {
                ResultTextBox.Text = "无法获取活动编辑器。";
                return;
            }

            string textToInsert = ResultTextBox.Text;
            if (string.IsNullOrEmpty(textToInsert))
            {
                return;
            }

            using (var edit = textView.TextBuffer.CreateEdit())
            {
                if (!string.IsNullOrEmpty(_selectedCode) && textView.Selection.SelectedSpans.Count > 0)
                {
                    // 替换选中的代码
                    edit.Replace(textView.Selection.SelectedSpans[0], textToInsert);
                }
                else
                {
                    // 在光标处插入
                    edit.Insert(textView.Caret.Position.BufferPosition, textToInsert);
                }
                edit.Apply();
            }
        }

        private ITextView GetActiveTextView()
        {
            return ServiceProvider.GlobalProvider.GetService(typeof(Microsoft.VisualStudio.TextManager.Interop.SVsTextManager)) is Microsoft.VisualStudio.TextManager.Interop.IVsTextManager textManager
                && textManager.GetActiveView(1, null, out var textView) == Microsoft.VisualStudio.VSConstants.S_OK
                ? textView as ITextView
                : null;
        }

        private ITextDocument GetActiveDocument()
        {
            var textView = GetActiveTextView();
            return textView?.TextBuffer.GetRelatedDocuments().FirstOrDefault();
        }
    }
}
