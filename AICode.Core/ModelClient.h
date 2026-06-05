#pragma once
#include "pch.h"

class ModelClient
{
public:
    ModelClient(const std::string& apiKey, const std::string& model = "claude-3-5-sonnet-20241022");
    ~ModelClient() = default;

    using StreamCallback = std::function<void(const std::string&)>;
    pplx::task<void> GenerateStreamAsync(const std::string& prompt, StreamCallback callback);
    pplx::task<std::string> GenerateAsync(const std::string& prompt);

    void SetModel(const std::string& model)
    {
        m_model = model;
    }
    void SetApiKey(const std::string& apiKey)
    {
        m_apiKey = apiKey;
    }

private:
    std::string m_apiKey;
    std::string m_model;
    std::unique_ptr<http_client> m_client;
};