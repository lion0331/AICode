#pragma once

#include "AICodeCore.h"
#include <curl/curl.h>
#include <nlohmann/json.hpp>

namespace AICode::Core
{
    class OpenAIClient
    {
    public:
        OpenAIClient(ModelType modelType, const std::string& apiKey, const std::string& apiBaseUrl);
        ~OpenAIClient();

        CompletionResponse CreateChatCompletion(const nlohmann::json& messages, 
            int maxTokens, float temperature);

    private:
        ModelType m_modelType;
        std::string m_apiKey;
        std::string m_apiBaseUrl;
        CURL* m_curl;

        static size_t WriteCallback(void* contents, size_t size, size_t nmemb, std::string* s);
        std::string GetModelName() const;
    };
}
