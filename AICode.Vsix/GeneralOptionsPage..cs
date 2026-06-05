using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

namespace AICode.Vsix
{
    public class GeneralOptionsPage : DialogPage
    {
        private string _apiKey = string.Empty;
        private string _apiEndpoint = "https://api.example.com/ai/code";

        [Category("AI Service")]
        [DisplayName("API Key")]
        [Description("AI服务的API密钥")]
        public string ApiKey
        {
            get => _apiKey;
            set => _apiKey = value;
        }

        [Category("AI Service")]
        [DisplayName("API Endpoint")]
        [Description("AI服务的接口地址")]
        public string ApiEndpoint
        {
            get => _apiEndpoint;
            set => _apiEndpoint = value;
        }
    }
}