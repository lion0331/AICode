using AICode.Services;
using AICodeAssistant.Services;
using Microsoft.VisualStudio.Extensibility.Options;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace AICodeAssistant.Options;

[ComVisible(true)]
public class GeneralOptionsPage : DialogPage
{
    private ICredentialService? _credentialService;
    private ICoreService? _coreService;

    [Category("API设置")]
    [DisplayName("OpenAI API密钥")]
    [PasswordPropertyText(true)]
    public string OpenAIApiKey { get; set; } = "";

    [Category("API设置")]
    [DisplayName("Anthropic API密钥")]
    [PasswordPropertyText(true)]
    public string AnthropicApiKey { get; set; } = "";

    [Category("模型设置")]
    [DisplayName("默认模型")]
    [TypeConverter(typeof(ModelListConverter))]
    public string DefaultModel { get; set; } = "claude-3-5-sonnet-20241022";

    protected override void OnActivate(CancelEventArgs e)
    {
        base.OnActivate(e);

        _credentialService = this.Extensibility.ServiceProvider.GetRequiredService<ICredentialService>();
        _coreService = this.Extensibility.ServiceProvider.GetRequiredService<ICoreService>();

        OpenAIApiKey = _credentialService.GetApiKey("OpenAI");
        AnthropicApiKey = _credentialService.GetApiKey("Anthropic");
        DefaultModel = _credentialService.GetApiKey("DefaultModel") ?? "claude-3-5-sonnet-20241022";
    }

    protected override void OnApply(PageApplyEventArgs e)
    {
        base.OnApply(e);

        if (e.ApplyBehavior == ApplyBehavior.Apply && _credentialService != null && _coreService != null)
        {
            _credentialService.SaveApiKey("OpenAI", OpenAIApiKey);
            _credentialService.SaveApiKey("Anthropic", AnthropicApiKey);
            _credentialService.SaveApiKey("DefaultModel", DefaultModel);

            if (!string.IsNullOrEmpty(AnthropicApiKey))
            {
                _coreService.Initialize(AnthropicApiKey, DefaultModel);
            }
            else if (!string.IsNullOrEmpty(OpenAIApiKey))
            {
                _coreService.Initialize(OpenAIApiKey, DefaultModel);
            }
        }
    }
}

public class ModelListConverter : StringConverter
{
    private static readonly StandardValuesCollection _models = new(new[]
    {
        "claude-3-5-sonnet-20241022",
        "claude-3-opus-20240229",
        "gpt-4o-2026-05-13",
        "gpt-4-turbo-2024-04-09"
    });

    public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) => true;
    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context) => _models;
    public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) => true;
}