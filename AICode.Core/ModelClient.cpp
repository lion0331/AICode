#include "pch.h"
#include "ModelClient.h"

ModelClient::ModelClient(const std::string& apiKey, const std::string& model)
    : m_apiKey(apiKey), m_model(model)
{
    // 初始化WinINet
    m_hInternet = InternetOpenA("VS-AI-Code-Assistant", INTERNET_OPEN_TYPE_DIRECT, NULL, NULL, 0);
    if (!m_hInternet)
    {
        throw std::runtime_error("WinINet初始化失败");
    }

    // 连接对应API服务器
    if (model.starts_with("claude"))
    {
        m_hConnect = InternetConnectA(m_hInternet, "api.anthropic.com", INTERNET_DEFAULT_HTTPS_PORT, NULL, NULL, INTERNET_SERVICE_HTTP, 0, 0);
    }
    else
    {
        m_hConnect = InternetConnectA(m_hInternet, "api.openai.com", INTERNET_DEFAULT_HTTPS_PORT, NULL, NULL, INTERNET_SERVICE_HTTP, 0, 0);
    }

    if (!m_hConnect)
    {
        InternetCloseHandle(m_hInternet);
        throw std::runtime_error("无法连接到API服务器");
    }
}

ModelClient::~ModelClient()
{
    if (m_hConnect) InternetCloseHandle(m_hConnect);
    if (m_hInternet) InternetCloseHandle(m_hInternet);
}

std::string ModelClient::ParseStreamChunk(const std::string& data)
{
    try
    {
        nlohmann_json j = nlohmann_json::parse(data);

        if (m_model.starts_with("claude"))
        {
            if (j.contains("type") && j["type"] == "content_block_delta"
                && j.contains("delta") && j["delta"].contains("text"))
            {
                return j["delta"]["text"].get<std::string>();
            }
        }
        else
        {
            if (j.contains("choices") && !j["choices"].empty()
                && j["choices"][0].contains("delta") && j["choices"][0]["delta"].contains("content"))
            {
                return j["choices"][0]["delta"]["content"].get<std::string>();
            }
        }
    }
    catch (...)
    {
    }

    return "";
}

void ModelClient::GenerateStreamAsync(const std::string& prompt, StreamCallback callback)
{
    // 构造请求体
    nlohmann_json requestBody;
    std::string path;
    std::string headers;

    if (m_model.starts_with("claude"))
    {
        path = "/v1/messages";
        headers = "Content-Type: application/json\r\nx-api-key: " + m_apiKey + "\r\nanthropic-version: 2023-06-01\r\n";

        requestBody = {
            {"model", m_model},
            {"max_tokens", 4096},
            {"temperature", 0.2},
            {"stream", true},
            {"system", "你是专业的C++和C#开发助手，只输出代码和必要解释"},
            {"messages", {{
                {"role", "user"},
                {"content", prompt}
            }}}
        };
    }
    else
    {
        path = "/v1/chat/completions";
        headers = "Content-Type: application/json\r\nAuthorization: Bearer " + m_apiKey + "\r\n";

        requestBody = {
            {"model", m_model},
            {"max_tokens", 4096},
            {"temperature", 0.2},
            {"stream", true},
            {"messages", {
                {{"role", "system"}, {"content", "你是专业的C++和C#开发助手，只输出代码和必要解释"}},
                {{"role", "user"}, {"content", prompt}}
            }}
        };
    }

    std::string requestData = requestBody.dump();

    // 发送HTTP请求
    HINTERNET hRequest = HttpOpenRequestA(m_hConnect, "POST", path.c_str(), NULL, NULL, NULL, INTERNET_FLAG_SECURE, 0);
    if (!hRequest)
    {
        callback("错误：无法创建HTTP请求");
        return;
    }

    // 发送请求
    if (!HttpSendRequestA(hRequest, headers.c_str(), headers.length(), (LPVOID)requestData.c_str(), requestData.length()))
    {
        InternetCloseHandle(hRequest);
        callback("错误：HTTP请求发送失败");
        return;
    }

    // 读取流式响应
    char buffer[4096];
    DWORD bytesRead;
    std::string lineBuffer;

    while (InternetReadFile(hRequest, buffer, sizeof(buffer) - 1, &bytesRead) && bytesRead > 0)
    {
        buffer[bytesRead] = '\0';
        lineBuffer += buffer;

        // 按行解析SSE响应
        size_t pos;
        while ((pos = lineBuffer.find('\n')) != std::string::npos)
        {
            std::string line = lineBuffer.substr(0, pos);
            lineBuffer = lineBuffer.substr(pos + 1);

            if (line.empty() || !line.starts_with("data: "))
                continue;

            std::string data = line.substr(6);
            if (data == "[DONE]")
                break;

            std::string content = ParseStreamChunk(data);
            if (!content.empty())
            {
                callback(content);
            }
        }
    }

    InternetCloseHandle(hRequest);
    callback("[END]");
}

std::string ModelClient::GenerateSync(const std::string& prompt)
{
    std::string result;
    GenerateStreamAsync(prompt, [&result](const std::string& chunk) {
        if (chunk != "[END]")
            result += chunk;
        });
    return result;
}