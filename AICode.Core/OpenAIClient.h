#pragma once

#include "AICodeCore.h"
#include <nlohmann/json.hpp>
#include <string>

namespace AICode::Core
{
    class OpenAIClient
    {
    public:
        OpenAIClient(ModelType modelType, const std::string& apiKey, const std::string& apiBaseUrl);

        CompletionResponse CreateChatCompletion(const nlohmann::json& messages, int maxTokens, float temperature);

    private:
        ModelType m_modelType;
        std::string m_apiKey;
        std::string m_apiBaseUrl;

        std::string GetModelName() const;
        std::string GetDefaultBaseUrl() const;
        CompletionResponse CreateOpenAICompletion(const nlohmann::json& messages, int maxTokens, float temperature) const;
        CompletionResponse CreateClaudeCompletion(const nlohmann::json& messages, int maxTokens, float temperature) const;
        CompletionResponse CreateGeminiCompletion(const nlohmann::json& messages, int maxTokens, float temperature) const;
    };
}
