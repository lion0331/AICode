namespace AICode.Services;

public interface ICredentialService
{
    void SaveApiKey(string providerName, string apiKey);
    string GetApiKey(string providerName);
    void DeleteApiKey(string providerName);
}