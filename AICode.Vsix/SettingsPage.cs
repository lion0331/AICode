using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace AICode.Vsix
{
    public class SettingsPage : DialogPage
    {
        [Category("模型设置")]
        [DisplayName("选择模型")]
        [Description("要使用的AI大模型")]
        public ModelType SelectedModel { get; set; } = ModelType.GPT4o;

        [Category("模型设置")]
        [DisplayName("API密钥")]
        [Description("大模型API密钥")]
        [PasswordPropertyText(true)]
        public string ApiKey { get; set; } = "";

        [Category("模型设置")]
        [DisplayName("API基础地址")]
        [Description("自定义API基础地址（用于代理或兼容服务）")]
        public string ApiBaseUrl { get; set; } = "";

        [Category("补全设置")]
        [DisplayName("启用内联补全")]
        [Description("是否在编辑器中显示内联代码补全")]
        public bool EnableInlineCompletion { get; set; } = true;

        [Category("补全设置")]
        [DisplayName("最大补全Token数")]
        [Description("单次补全生成的最大Token数量")]
        public int MaxCompletionTokens { get; set; } = 1024;

        [Category("补全设置")]
        [DisplayName("补全温度")]
        [Description("控制补全的随机性，0表示最确定，1表示最随机")]
        public float CompletionTemperature { get; set; } = 0.2f;

        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);
            AICodePackage.Instance?.ReinitializeEngine();
        }
    }
}
