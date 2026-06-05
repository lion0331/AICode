using AICode.Services;
using CredentialManagement;
using System.Net;

namespace AICode.Services;

public class CredentialService : ICredentialService
{
    private const string CredentialPrefix = "AICode_";

    public void SaveApiKey(string providerName, string apiKey)
    {
        using var credential = new Credential
        {
            Target = $"{CredentialPrefix}{providerName}",
            Username = "user",
            Password = apiKey,
            Type = CredentialType.Generic,
            PersistanceType = PersistanceType.LocalComputer
        };
        credential.Save();
    }

    public string GetApiKey(string providerName)
    {
        using var credential = new Credential
        {
            Target = $"{CredentialPrefix}{providerName}",
            Type = CredentialType.Generic
        };
        return credential.Load() ? credential.Password : string.Empty;
    }

    public void DeleteApiKey(string providerName)
    {
        using var credential = new Credential
        {
            Target = $"{CredentialPrefix}{providerName}",
            Type = CredentialType.Generic
        };
        credential.Delete();
    }
}