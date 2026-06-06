#include "OpenAIClient.h"
#include <sstream>

using json = nlohmann::json;

namespace AICode::Core
{
    OpenAIClient::OpenAIClient(ModelType modelType, const std::string& apiKey, const std::string& apiBaseUrl)
        : m_modelType(modelType), m_apiKey(apiKey), m_apiBaseUrl(apiBaseUrl)
    {
        m_curl = curl_easy_init();
        if (m_apiBaseUrl.empty())
        {
            m_apiBaseUrl = "https://api.openai.com/v1";
        }
    }

    OpenAIClient::~OpenAIClient()
    {
        if (m_curl)
        {
            curl_easy_cleanup(m_curl);
        }
    }

    size_t OpenAIClient::WriteCallback(void* contents, size_t size, size_t nmemb, std::string* s)
    {
        size_t newLength = size * nmemb;
        try
        {
            s->append((char*)contents, newLength);
        }
        catch (std::bad_alloc& e)
        {
            return 0;
        }
        return newLength;
    }

    std::string OpenAIClient::GetModelName() const
    {
        switch (m_modelType)
        {
        case ModelType::GPT4o: return "gpt-4o";
        case ModelType::GPT4Turbo: return "gpt-4-turbo";
        case ModelType::Claude35Sonnet: return "claude-3-5-sonnet-20240620";
        case ModelType::Claude3Opus: return "claude-3-opus-20240229";
        case ModelType::GeminiPro: return "gemini-pro";
        default: return "gpt-4o";
        }
    }

    CompletionResponse OpenAIClient::CreateChatCompletion(const json& messages, 
        int maxTokens, float temperature)
    {
        CompletionResponse response{};
        response.Success = false;

        if (!m_curl)
        {
            response.ErrorMessage = "CURL初始化失败";
            return response;
        }

        json requestBody;
        std::string url;
        std::string authHeader;

        if (m_modelType == ModelType::Claude35Sonnet || m_modelType == ModelType::Claude3Opus)
        {
            // Anthropic Claude API
            url = m_apiBaseUrl.empty() ? "https://api.anthropic.com/v1/messages" : m_apiBaseUrl;
            
            requestBody["model"] = GetModelName();
            requestBody["max_tokens"] = maxTokens;
            requestBody["temperature"] = temperature;
            requestBody["messages"] = messages;

            authHeader = "x-api-key: " + m_apiKey;
        }
        else if (m_modelType == ModelType::GeminiPro)
        {
            // Google Gemini API
            url = m_apiBaseUrl.empty() ? "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent" : m_apiBaseUrl;
            url += "?key=" + m_apiKey;

            // 转换消息格式
            json contents = json::array();
            for (const auto& msg : messages)
            {
                json content;
                content["role"] = msg["role"] == "system" ? "user" : msg["role"];
                content["parts"] = json::array();
                content["parts"].push_back({ {"text", msg["content"]} });
                contents.push_back(content);
            }
            requestBody["contents"] = contents;
            requestBody["generationConfig"]["maxOutputTokens"] = maxTokens;
            requestBody["generationConfig"]["temperature"] = temperature;

            authHeader = ""; // Gemini使用URL参数传递API密钥
        }
        else
        {
            // OpenAI API
            url = m_apiBaseUrl.empty() ? "https://api.openai.com/v1/chat/completions" : m_apiBaseUrl;
            
            requestBody["model"] = GetModelName();
            requestBody["messages"] = messages;
            requestBody["max_tokens"] = maxTokens;
            requestBody["temperature"] = temperature;
            requestBody["stream"] = false;

            authHeader = "Authorization: Bearer " + m_apiKey;
        }

        std::string requestBodyStr = requestBody.dump();
        std::string responseStr;

        curl_easy_setopt(m_curl, CURLOPT_URL, url.c_str());
        curl_easy_setopt(m_curl, CURLOPT_POSTFIELDS, requestBodyStr.c_str());
        curl_easy_setopt(m_curl, CURLOPT_WRITEFUNCTION, WriteCallback);
        curl_easy_setopt(m_curl, CURLOPT_WRITEDATA, &responseStr);

        struct curl_slist* headers = nullptr;
        headers = curl_slist_append(headers, "Content-Type: application/json");
        if (!authHeader.empty())
        {
            headers = curl_slist_append(headers, authHeader.c_str());
        }
        curl_easy_setopt(m_curl, CURLOPT_HTTPHEADER, headers);

        CURLcode res = curl_easy_perform(m_curl);
        curl_slist_free_all(headers);

        if (res != CURLE_OK)
        {
            response.ErrorMessage = "CURL请求失败: " + std::string(curl_easy_strerror(res));
            return response;
        }

        try
        {
            json responseJson = json::parse(responseStr);
            
            if (responseJson.contains("error"))
            {
                response.ErrorMessage = responseJson["error"]["message"].get<std::string>();
                return response;
            }

            if (m_modelType == ModelType::Claude35Sonnet || m_modelType == ModelType::Claude3Opus)
            {
                response.CompletionText = responseJson["content"][0]["text"].get<std::string>();
                response.ModelUsed = responseJson["model"].get<std::string>();
                response.TokensUsed = responseJson["usage"]["input_tokens"].get<int>() + responseJson["usage"]["output_tokens"].get<int>();
            }
            else if (m_modelType == ModelType::GeminiPro)
            {
                response.CompletionText = responseJson["candidates"][0]["content"]["parts"][0]["text"].get<std::string>();
                response.ModelUsed = "gemini-pro";
                response.TokensUsed = 0; // Gemini API不返回token使用量
            }
            else
            {
                response.CompletionText = responseJson["choices"][0]["message"]["content"].get<std::string>();
                response.ModelUsed = responseJson["model"].get<std::string>();
                response.TokensUsed = responseJson["usage"]["total_tokens"].get<int>();
            }

            response.Success = true;
        }
        catch (const std::exception& e)
        {
            response.ErrorMessage = "JSON解析失败: " + std::string(e.what()) + "\n响应内容: " + responseStr;
        }

        return response;
    }
}
