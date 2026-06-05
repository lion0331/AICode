#include "ModelClient.h"

ModelClient::ModelClient(const std::string& apiKey, const std::string& model)
    : m_apiKey(apiKey), m_model(model)
{
    if (model.starts_with("claude"))
    {
        m_client = std::make_unique<http_client>(U("https://api.anthropic.com/v1/messages"));
    }
    else
    {
        m_client = std::make_unique<http_client>(U("https://api.openai.com/v1/chat/completions"));
    }
}

pplx::task<void> ModelClient::GenerateStreamAsync(const std::string& prompt, StreamCallback callback)
{
    json requestBody;

    if (m_model.starts_with("claude"))
    {
        requestBody = {
            {"model", m_model},
            {"max_tokens", 4096},
            {"temperature", 0.2},
            {"stream", true},
            {"system", "你是一个专业的C++和C#开发助手。只输出代码和必要的解释，保持简洁。"},
            {"messages", {
                {{"role", "user"}, {"content", prompt}}
            }}
        };
    }
    else
    {
        requestBody = {
            {"model", m_model},
            {"max_tokens", 4096},
            {"temperature", 0.2},
            {"stream", true},
            {"messages", {
                {{"role", "system"}, {"content", "你是一个专业的C++和C#开发助手。只输出代码和必要的解释，保持简洁。"}},
                {{"role", "user"}, {"content", prompt}}
            }}
        };
    }

    http_request request(methods::POST);

    if (m_model.starts_with("claude"))
    {
        request.headers().add(U("x-api-key"), utility::conversions::to_string_t(m_apiKey));
        request.headers().add(U("anthropic-version"), U("2023-06-01"));
    }
    else
    {
        request.headers().add(U("Authorization"), U("Bearer ") + utility::conversions::to_string_t(m_apiKey));
    }

    request.headers().set_content_type(U("application/json"));
    request.set_body(requestBody.dump());

    return m_client->request(request).then([callback](http_response response)
        {
            if (response.status_code() != status_codes::OK)
            {
                throw std::runtime_error("API请求失败: " + std::to_string(response.status_code()));
            }

            return response.body().read_to_end().then([callback](const std::vector<unsigned char>& body)
                {
                    std::string responseStr(body.begin(), body.end());
                    std::istringstream iss(responseStr);
                    std::string line;

                    while (std::getline(iss, line))
                    {
                        if (line.starts_with("data: "))
                        {
                            std::string data = line.substr(6);
                            if (data == "[DONE]") break;

                            try
                            {
                                json j = json::parse(data);
                                std::string content;

                                if (j.contains("choices") && !j["choices"].empty() && j["choices"][0].contains("delta") && j["choices"][0]["delta"].contains("content"))
                                {
                                    content = j["choices"][0]["delta"]["content"].get<std::string>();
                                }
                                else if (j.contains("delta") && j["delta"].contains("text"))
                                {
                                    content = j["delta"]["text"].get<std::string>();
                                }

                                if (!content.empty())
                                {
                                    callback(content);
                                }
                            }
                            catch (...)
                            {
                                // 忽略解析错误
                            }
                        }
                    }
                });
        });
}

pplx::task<std::string> ModelClient::GenerateAsync(const std::string& prompt)
{
    std::string result;
    return GenerateStreamAsync(prompt, [&result](const std::string& chunk)
        {
            result += chunk;
        }).then([result]() { return result; });
}