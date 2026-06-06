#include "OpenAIClient.h"

#include <curl/curl.h>
#include <functional>
#include <sstream>
#include <vector>

using json = nlohmann::json;

namespace
{
    class CurlGlobalInitializer
    {
    public:
        CurlGlobalInitializer()
        {
            curl_global_init(CURL_GLOBAL_DEFAULT);
        }

        ~CurlGlobalInitializer()
        {
            curl_global_cleanup();
        }
    };

    CurlGlobalInitializer g_curlGlobalInitializer;

    size_t WriteCallback(void* contents, size_t size, size_t nmemb, void* userData)
    {
        const size_t totalSize = size * nmemb;
        auto* buffer = static_cast<std::string*>(userData);
        buffer->append(static_cast<const char*>(contents), totalSize);
        return totalSize;
    }

    AICode::Core::CompletionResponse ExecuteJsonPost(
        const std::string& url,
        const json& body,
        const std::vector<std::string>& headerValues,
        std::function<AICode::Core::CompletionResponse(const json&)> parseResponse)
    {
        AICode::Core::CompletionResponse response{};

        CURL* curl = curl_easy_init();
        if (curl == nullptr)
        {
            response.ErrorMessage = "CURL initialization failed";
            return response;
        }

        std::string responseText;
        std::string requestBody = body.dump();
        struct curl_slist* headers = nullptr;

        headers = curl_slist_append(headers, "Content-Type: application/json");
        for (const auto& headerValue : headerValues)
        {
            headers = curl_slist_append(headers, headerValue.c_str());
        }

        curl_easy_setopt(curl, CURLOPT_URL, url.c_str());
        curl_easy_setopt(curl, CURLOPT_HTTPHEADER, headers);
        curl_easy_setopt(curl, CURLOPT_POST, 1L);
        curl_easy_setopt(curl, CURLOPT_POSTFIELDS, requestBody.c_str());
        curl_easy_setopt(curl, CURLOPT_POSTFIELDSIZE, static_cast<long>(requestBody.size()));
        curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION, WriteCallback);
        curl_easy_setopt(curl, CURLOPT_WRITEDATA, &responseText);
        curl_easy_setopt(curl, CURLOPT_TIMEOUT, 120L);
        curl_easy_setopt(curl, CURLOPT_CONNECTTIMEOUT, 30L);
        curl_easy_setopt(curl, CURLOPT_FOLLOWLOCATION, 1L);
        curl_easy_setopt(curl, CURLOPT_SSL_VERIFYPEER, 1L);

        const CURLcode result = curl_easy_perform(curl);
        long httpStatusCode = 0;
        curl_easy_getinfo(curl, CURLINFO_RESPONSE_CODE, &httpStatusCode);

        curl_slist_free_all(headers);
        curl_easy_cleanup(curl);

        if (result != CURLE_OK)
        {
            response.ErrorMessage = "CURL request failed: " + std::string(curl_easy_strerror(result));
            return response;
        }

        try
        {
            const json responseJson = json::parse(responseText);
            if (httpStatusCode >= 400)
            {
                if (responseJson.contains("error"))
                {
                    if (responseJson["error"].is_object() && responseJson["error"].contains("message"))
                    {
                        response.ErrorMessage = responseJson["error"]["message"].get<std::string>();
                    }
                    else if (responseJson["error"].is_string())
                    {
                        response.ErrorMessage = responseJson["error"].get<std::string>();
                    }
                }

                if (response.ErrorMessage.empty())
                {
                    response.ErrorMessage = "HTTP request failed, status code: " + std::to_string(httpStatusCode);
                }
                return response;
            }

            return parseResponse(responseJson);
        }
        catch (const std::exception& ex)
        {
            response.ErrorMessage = "JSON parse failed: " + std::string(ex.what()) + "\nResponse: " + responseText;
            return response;
        }
    }
}

namespace AICode::Core
{
    OpenAIClient::OpenAIClient(ModelType modelType, const std::string& apiKey, const std::string& apiBaseUrl)
        : m_modelType(modelType),
        m_apiKey(apiKey),
        m_apiBaseUrl(apiBaseUrl.empty() ? GetDefaultBaseUrl() : apiBaseUrl)
    {
    }

    CompletionResponse OpenAIClient::CreateChatCompletion(const json& messages, int maxTokens, float temperature)
    {
        switch (m_modelType)
        {
        case ModelType::Claude35Sonnet:
        case ModelType::Claude3Opus:
            return CreateClaudeCompletion(messages, maxTokens, temperature);
        case ModelType::GeminiPro:
            return CreateGeminiCompletion(messages, maxTokens, temperature);
        default:
            return CreateOpenAICompletion(messages, maxTokens, temperature);
        }
    }

    std::string OpenAIClient::GetModelName() const
    {
        switch (m_modelType)
        {
        case ModelType::GPT4o:
            return "gpt-4o";
        case ModelType::GPT4Turbo:
            return "gpt-4-turbo";
        case ModelType::Claude35Sonnet:
            return "claude-3-5-sonnet-20240620";
        case ModelType::Claude3Opus:
            return "claude-3-opus-20240229";
        case ModelType::GeminiPro:
            return "gemini-pro";
        default:
            return "gpt-4o";
        }
    }

    std::string OpenAIClient::GetDefaultBaseUrl() const
    {
        switch (m_modelType)
        {
        case ModelType::Claude35Sonnet:
        case ModelType::Claude3Opus:
            return "https://api.anthropic.com/v1/messages";
        case ModelType::GeminiPro:
            return "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent";
        default:
            return "https://api.openai.com/v1/chat/completions";
        }
    }

    CompletionResponse OpenAIClient::CreateOpenAICompletion(const json& messages, int maxTokens, float temperature) const
    {
        json requestBody;
        requestBody["model"] = GetModelName();
        requestBody["messages"] = messages;
        requestBody["max_tokens"] = maxTokens;
        requestBody["temperature"] = temperature;
        requestBody["stream"] = false;

        return ExecuteJsonPost(
            m_apiBaseUrl,
            requestBody,
            { "Authorization: Bearer " + m_apiKey },
            [](const json& responseJson)
            {
                CompletionResponse response{};
                response.CompletionText = responseJson.at("choices").at(0).at("message").at("content").get<std::string>();
                response.ModelUsed = responseJson.value("model", std::string());
                response.TokensUsed = responseJson.at("usage").value("total_tokens", 0);
                response.Success = true;
                return response;
            });
    }

    CompletionResponse OpenAIClient::CreateClaudeCompletion(const json& messages, int maxTokens, float temperature) const
    {
        json requestMessages = json::array();
        std::string systemPrompt;

        for (const auto& message : messages)
        {
            const auto role = message.value("role", std::string());
            const auto content = message.value("content", std::string());

            if (role == "system")
            {
                if (!systemPrompt.empty())
                {
                    systemPrompt += "\n\n";
                }
                systemPrompt += content;
                continue;
            }

            requestMessages.push_back({
                { "role", role == "assistant" ? "assistant" : "user" },
                { "content", content }
            });
        }

        json requestBody;
        requestBody["model"] = GetModelName();
        requestBody["max_tokens"] = maxTokens;
        requestBody["temperature"] = temperature;
        requestBody["messages"] = requestMessages;
        if (!systemPrompt.empty())
        {
            requestBody["system"] = systemPrompt;
        }

        return ExecuteJsonPost(
            m_apiBaseUrl,
            requestBody,
            {
                "x-api-key: " + m_apiKey,
                "anthropic-version: 2023-06-01"
            },
            [](const json& responseJson)
            {
                CompletionResponse response{};
                response.CompletionText = responseJson.at("content").at(0).at("text").get<std::string>();
                response.ModelUsed = responseJson.value("model", std::string());
                response.TokensUsed =
                    responseJson.at("usage").value("input_tokens", 0) +
                    responseJson.at("usage").value("output_tokens", 0);
                response.Success = true;
                return response;
            });
    }

    CompletionResponse OpenAIClient::CreateGeminiCompletion(const json& messages, int maxTokens, float temperature) const
    {
        json contents = json::array();
        std::string systemPrompt;

        for (const auto& message : messages)
        {
            const auto role = message.value("role", std::string());
            const auto content = message.value("content", std::string());
            if (role == "system")
            {
                if (!systemPrompt.empty())
                {
                    systemPrompt += "\n\n";
                }
                systemPrompt += content;
                continue;
            }

            contents.push_back({
                { "role", role == "assistant" ? "model" : "user" },
                { "parts", json::array({ { { "text", content } } }) }
            });
        }

        json requestBody;
        requestBody["contents"] = contents;
        requestBody["generationConfig"] = {
            { "maxOutputTokens", maxTokens },
            { "temperature", temperature }
        };

        if (!systemPrompt.empty())
        {
            requestBody["systemInstruction"] = {
                { "parts", json::array({ { { "text", systemPrompt } } }) }
            };
        }

        const auto separator = m_apiBaseUrl.find('?') == std::string::npos ? "?" : "&";
        const auto url = m_apiBaseUrl + separator + "key=" + m_apiKey;

        return ExecuteJsonPost(
            url,
            requestBody,
            {},
            [](const json& responseJson)
            {
                CompletionResponse response{};
                response.CompletionText = responseJson.at("candidates").at(0).at("content").at("parts").at(0).at("text").get<std::string>();
                response.ModelUsed = "gemini-pro";
                response.TokensUsed = responseJson.contains("usageMetadata")
                    ? responseJson["usageMetadata"].value("totalTokenCount", 0)
                    : 0;
                response.Success = true;
                return response;
            });
    }
}
