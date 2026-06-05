#pragma once
#include "pch.h"

class ModelClient
{
public:
    ModelClient(const std::string& apiKey, const std::string& model);
    ~ModelClient();

    using StreamCallback = std::function<void(const std::string&)>;
    void GenerateStreamAsync(const std::string& prompt, StreamCallback callback);
    std::string GenerateSync(const std::string& prompt);

private:
    std::string m_apiKey;
    std::string m_model;
    HINTERNET m_hInternet;
    HINTERNET m_hConnect;

    std::string ParseStreamChunk(const std::string& data);
};