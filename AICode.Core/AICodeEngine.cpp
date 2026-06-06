#include "AICodeEngine.h"

#include <cstring>
#include <cwchar>
#include <filesystem>
#include <fstream>
#include <sstream>
#include <utility>
#include <windows.h>

using json = nlohmann::json;

namespace AICode::Core
{
    namespace
    {
        std::string WideToUtf8(const std::wstring& value)
        {
            if (value.empty())
            {
                return {};
            }

            const int requiredSize = WideCharToMultiByte(
                CP_UTF8,
                0,
                value.c_str(),
                static_cast<int>(value.size()),
                nullptr,
                0,
                nullptr,
                nullptr);

            if (requiredSize <= 0)
            {
                return {};
            }

            std::string utf8Value(requiredSize, '\0');
            WideCharToMultiByte(
                CP_UTF8,
                0,
                value.c_str(),
                static_cast<int>(value.size()),
                &utf8Value[0],
                requiredSize,
                nullptr,
                nullptr);

            return utf8Value;
        }

        std::wstring Utf8ToWide(const std::string& value)
        {
            if (value.empty())
            {
                return {};
            }

            const int requiredSize = MultiByteToWideChar(
                CP_UTF8,
                0,
                value.c_str(),
                static_cast<int>(value.size()),
                nullptr,
                0);

            if (requiredSize <= 0)
            {
                return {};
            }

            std::wstring wideValue(requiredSize, L'\0');
            MultiByteToWideChar(
                CP_UTF8,
                0,
                value.c_str(),
                static_cast<int>(value.size()),
                &wideValue[0],
                requiredSize);

            return wideValue;
        }

        std::string SafeUtf8String(const wchar_t* value)
        {
            return value != nullptr ? WideToUtf8(value) : std::string();
        }

        wchar_t* AllocateWideString(const std::string& value)
        {
            const auto wideValue = Utf8ToWide(value);
            const auto size = wideValue.size() + 1;
            auto* buffer = new wchar_t[size];
            std::wmemcpy(buffer, wideValue.c_str(), size);
            return buffer;
        }

        NativeCompletionResponse ToNativeResponse(const CompletionResponse& response)
        {
            return {
                AllocateWideString(response.CompletionText),
                AllocateWideString(response.ModelUsed),
                response.TokensUsed,
                response.Success,
                AllocateWideString(response.ErrorMessage)
            };
        }
    }

    AICodeEngine::AICodeEngine() : m_initialized(false), m_workerRunning(false)
    {
    }

    AICodeEngine::~AICodeEngine()
    {
        if (m_workerRunning)
        {
            m_workerRunning = false;
            m_queueCondition.notify_one();
            if (m_workerThread.joinable())
            {
                m_workerThread.join();
            }
        }
    }

    bool AICodeEngine::Initialize(ModelType modelType, const std::string& apiKey, const std::string& apiBaseUrl)
    {
        try
        {
            if (m_workerRunning)
            {
                m_workerRunning = false;
                m_queueCondition.notify_one();
                if (m_workerThread.joinable())
                {
                    m_workerThread.join();
                }
            }

            m_client = std::make_unique<OpenAIClient>(modelType, apiKey, apiBaseUrl);
            m_initialized = true;

            // 启动异步工作线程
            m_workerRunning = true;
            m_workerThread = std::thread(&AICodeEngine::WorkerThread, this);

            return true;
        }
        catch (const std::exception&)
        {
            return false;
        }
    }

    void AICodeEngine::WorkerThread()
    {
        while (m_workerRunning)
        {
            std::unique_lock<std::mutex> lock(m_queueMutex);
            m_queueCondition.wait(lock, [this] { return !m_taskQueue.empty() || !m_workerRunning; });

            if (!m_workerRunning) break;

            AsyncTask task = m_taskQueue.front();
            m_taskQueue.pop();
            lock.unlock();

            CompletionResponse response = GetCodeCompletion(task.request);
            if (task.callback)
            {
                task.callback(response);
            }
        }
    }

    CompletionResponse AICodeEngine::GetCodeCompletion(const CompletionRequest& request)
    {
        if (!m_initialized)
        {
            return { "", "", 0, false, "引擎未初始化" };
        }

        json messages = BuildCompletionMessages(request);
        return m_client->CreateChatCompletion(messages, request.MaxTokens, request.Temperature);
    }

    json AICodeEngine::BuildCompletionMessages(const CompletionRequest& request)
    {
        json messages = json::array();

        std::string systemPrompt = R"(你是一个专业的C++和C#代码助手，工作在Visual Studio 2026环境中。
请根据光标前后的代码上下文，生成高质量、符合C++/C#规范的代码补全。
只输出代码本身，不要添加任何解释、注释或markdown格式。
确保代码语法正确、类型安全、性能良好。)";

        messages.push_back({ {"role", "system"}, {"content", systemPrompt} });

        std::string userPrompt = "文件路径: " + request.FilePath + "\n";
        userPrompt += "语言: " + request.Language + "\n";
        userPrompt += "光标位置: 第" + std::to_string(request.LineNumber) + "行，第" + std::to_string(request.ColumnNumber) + "列\n\n";
        userPrompt += "光标前的代码:\n" + request.CodeBeforeCursor + "\n\n";
        userPrompt += "光标后的代码:\n" + request.CodeAfterCursor + "\n\n";
        userPrompt += "请生成光标处的代码补全:";

        messages.push_back({ {"role", "user"}, {"content", userPrompt} });

        return messages;
    }

    CompletionResponse AICodeEngine::GenerateCode(const GenerationRequest& request)
    {
        if (!m_initialized)
        {
            return { "", "", 0, false, "引擎未初始化" };
        }

        json messages = BuildGenerationMessages(request);
        return m_client->CreateChatCompletion(messages, request.MaxTokens, request.Temperature);
    }

    json AICodeEngine::BuildGenerationMessages(const GenerationRequest& request)
    {
        json messages = json::array();

        std::string systemPrompt = R"(你是一个专业的C++和C#代码生成器，工作在Visual Studio 2026环境中。
请根据用户的需求生成高质量、可直接编译运行的代码。
代码要符合现代C++/C#规范，结构清晰，注释适当。
如果用户提供了上下文文件，请参考这些文件的代码风格和架构。)";

        messages.push_back({ {"role", "system"}, {"content", systemPrompt} });

        std::string userPrompt = "生成需求: " + request.Prompt + "\n";
        userPrompt += "目标语言: " + request.Language + "\n\n";

        if (!request.ContextFiles.empty())
        {
            userPrompt += "相关上下文文件:\n";
            for (const auto& file : request.ContextFiles)
            {
                userPrompt += "--- " + file + " ---\n";
                userPrompt += ReadFile(file) + "\n\n";
            }
        }

        userPrompt += "请生成完整的代码:";

        messages.push_back({ {"role", "user"}, {"content", userPrompt} });

        return messages;
    }

    CompletionResponse AICodeEngine::RefactorCode(const RefactorRequest& request)
    {
        if (!m_initialized)
        {
            return { "", "", 0, false, "引擎未初始化" };
        }

        json messages = json::array();

        std::string systemPrompt = R"(你是一个专业的C++和C#代码重构专家。
请根据用户的指令对提供的代码进行重构。
重构后的代码应该保持原有功能不变，但具有更好的可读性、可维护性和性能。
只输出重构后的代码，不要添加任何解释。)";

        messages.push_back({ {"role", "system"}, {"content", systemPrompt} });

        std::string userPrompt = "文件路径: " + request.FilePath + "\n";
        userPrompt += "代码范围: 第" + std::to_string(request.StartLine) + "行到第" + std::to_string(request.EndLine) + "行\n\n";
        userPrompt += "原始代码:\n" + request.OriginalCode + "\n\n";
        userPrompt += "重构指令: " + request.RefactorInstruction + "\n\n";
        userPrompt += "请输出重构后的代码:";

        messages.push_back({ {"role", "user"}, {"content", userPrompt} });

        return m_client->CreateChatCompletion(messages, 4096, 0.3f);
    }

    std::string AICodeEngine::ExplainCode(const std::string& code, const std::string& language)
    {
        if (!m_initialized)
        {
            return "引擎未初始化";
        }

        json messages = json::array();

        std::string systemPrompt = R"(你是一个专业的C++和C#代码解释器。
请用清晰、简洁的语言解释用户提供的代码的功能、工作原理和关键技术点。
解释要通俗易懂，适合不同水平的开发者理解。)";

        messages.push_back({ {"role", "system"}, {"content", systemPrompt} });

        std::string userPrompt = "语言: " + language + "\n\n";
        userPrompt += "代码:\n" + code + "\n\n";
        userPrompt += "请详细解释这段代码:";

        messages.push_back({ {"role", "user"}, {"content", userPrompt} });

        CompletionResponse response = m_client->CreateChatCompletion(messages, 2048, 0.5f);
        return response.Success ? response.CompletionText : response.ErrorMessage;
    }

    std::string AICodeEngine::FindCodeIssues(const std::string& code, const std::string& language)
    {
        if (!m_initialized)
        {
            return "引擎未初始化";
        }

        json messages = json::array();

        std::string systemPrompt = R"(你是一个专业的C++和C#代码审查专家。
请仔细检查用户提供的代码，找出其中可能存在的问题，包括：
- 语法错误
- 逻辑错误
- 内存泄漏
- 空指针异常
- 性能问题
- 安全漏洞
- 代码风格问题
- 不符合最佳实践的地方

请按问题严重程度排序，并给出具体的修复建议。)";

        messages.push_back({ {"role", "system"}, {"content", systemPrompt} });

        std::string userPrompt = "语言: " + language + "\n\n";
        userPrompt += "代码:\n" + code + "\n\n";
        userPrompt += "请找出这段代码中的所有问题并给出修复建议:";

        messages.push_back({ {"role", "user"}, {"content", userPrompt} });

        CompletionResponse response = m_client->CreateChatCompletion(messages, 2048, 0.3f);
        return response.Success ? response.CompletionText : response.ErrorMessage;
    }

    std::string AICodeEngine::ReadFile(const std::string& filePath)
    {
        std::ifstream file(std::filesystem::path(Utf8ToWide(filePath)), std::ios::in | std::ios::binary);
        if (!file.is_open())
        {
            return "";
        }

        std::stringstream buffer;
        buffer << file.rdbuf();
        return buffer.str();
    }

    bool AICodeEngine::WriteFile(const std::string& filePath, const std::string& content)
    {
        std::ofstream file(std::filesystem::path(Utf8ToWide(filePath)), std::ios::out | std::ios::binary | std::ios::trunc);
        if (!file.is_open())
        {
            return false;
        }

        file.write(content.c_str(), content.size());
        return file.good();
    }

    void AICodeEngine::GetCodeCompletionAsync(const CompletionRequest& request, 
        std::function<void(const CompletionResponse&)> callback)
    {
        std::lock_guard<std::mutex> lock(m_queueMutex);
        m_taskQueue.push({ request, callback });
        m_queueCondition.notify_one();
    }

    // 导出函数实现
}

using namespace AICode::Core;

extern "C"
{
    IAICodeEngine* CreateAICodeEngine()
    {
        return new AICode::Core::AICodeEngine();
    }

    void ReleaseAICodeEngine(IAICodeEngine* engine)
    {
        delete engine;
    }

    bool InitializeAICodeEngine(IAICodeEngine* engine, ModelType modelType, const wchar_t* apiKey, const wchar_t* apiBaseUrl)
    {
        if (engine == nullptr)
        {
            return false;
        }

        return engine->Initialize(modelType, SafeUtf8String(apiKey), SafeUtf8String(apiBaseUrl));
    }

    NativeCompletionResponse GetCodeCompletion(
        IAICodeEngine* engine,
        const wchar_t* filePath,
        const wchar_t* language,
        const wchar_t* codeBeforeCursor,
        const wchar_t* codeAfterCursor,
        int lineNumber,
        int columnNumber,
        int maxTokens,
        float temperature)
    {
        if (engine == nullptr)
        {
            return ToNativeResponse({ "", "", 0, false, "引擎实例为空" });
        }

        CompletionRequest request;
        request.FilePath = SafeUtf8String(filePath);
        request.Language = SafeUtf8String(language);
        request.CodeBeforeCursor = SafeUtf8String(codeBeforeCursor);
        request.CodeAfterCursor = SafeUtf8String(codeAfterCursor);
        request.LineNumber = lineNumber;
        request.ColumnNumber = columnNumber;
        request.MaxTokens = maxTokens;
        request.Temperature = temperature;

        return ToNativeResponse(engine->GetCodeCompletion(request));
    }

    void GetCodeCompletionAsync(
        IAICodeEngine* engine,
        const wchar_t* filePath,
        const wchar_t* language,
        const wchar_t* codeBeforeCursor,
        const wchar_t* codeAfterCursor,
        int lineNumber,
        int columnNumber,
        int maxTokens,
        float temperature,
        CompletionCallback callback,
        void* userData)
    {
        if (engine == nullptr || callback == nullptr)
        {
            return;
        }

        CompletionRequest request;
        request.FilePath = SafeUtf8String(filePath);
        request.Language = SafeUtf8String(language);
        request.CodeBeforeCursor = SafeUtf8String(codeBeforeCursor);
        request.CodeAfterCursor = SafeUtf8String(codeAfterCursor);
        request.LineNumber = lineNumber;
        request.ColumnNumber = columnNumber;
        request.MaxTokens = maxTokens;
        request.Temperature = temperature;

        engine->GetCodeCompletionAsync(request, [callback, userData](const CompletionResponse& response)
        {
            callback(ToNativeResponse(response), userData);
        });
    }

    NativeCompletionResponse GenerateCode(
        IAICodeEngine* engine,
        const wchar_t* prompt,
        const wchar_t* language,
        const wchar_t* const* contextFiles,
        int contextFilesCount,
        int maxTokens,
        float temperature)
    {
        if (engine == nullptr)
        {
            return ToNativeResponse({ "", "", 0, false, "引擎实例为空" });
        }

        GenerationRequest request;
        request.Prompt = SafeUtf8String(prompt);
        request.Language = SafeUtf8String(language);
        request.MaxTokens = maxTokens;
        request.Temperature = temperature;

        for (int index = 0; index < contextFilesCount; ++index)
        {
            request.ContextFiles.push_back(SafeUtf8String(contextFiles[index]));
        }

        return ToNativeResponse(engine->GenerateCode(request));
    }

    NativeCompletionResponse RefactorCode(
        IAICodeEngine* engine,
        const wchar_t* filePath,
        const wchar_t* originalCode,
        const wchar_t* refactorInstruction,
        int startLine,
        int endLine)
    {
        if (engine == nullptr)
        {
            return ToNativeResponse({ "", "", 0, false, "引擎实例为空" });
        }

        RefactorRequest request;
        request.FilePath = SafeUtf8String(filePath);
        request.OriginalCode = SafeUtf8String(originalCode);
        request.RefactorInstruction = SafeUtf8String(refactorInstruction);
        request.StartLine = startLine;
        request.EndLine = endLine;

        return ToNativeResponse(engine->RefactorCode(request));
    }

    wchar_t* ExplainCode(IAICodeEngine* engine, const wchar_t* code, const wchar_t* language)
    {
        if (engine == nullptr)
        {
            return AllocateWideString("Engine instance is null");
        }

        return AllocateWideString(engine->ExplainCode(SafeUtf8String(code), SafeUtf8String(language)));
    }

    wchar_t* FindCodeIssues(IAICodeEngine* engine, const wchar_t* code, const wchar_t* language)
    {
        if (engine == nullptr)
        {
            return AllocateWideString("Engine instance is null");
        }

        return AllocateWideString(engine->FindCodeIssues(SafeUtf8String(code), SafeUtf8String(language)));
    }

    wchar_t* ReadAICodeFile(IAICodeEngine* engine, const wchar_t* filePath)
    {
        if (engine == nullptr)
        {
            return AllocateWideString("");
        }

        return AllocateWideString(engine->ReadFile(SafeUtf8String(filePath)));
    }

    bool WriteAICodeFile(IAICodeEngine* engine, const wchar_t* filePath, const wchar_t* content)
    {
        if (engine == nullptr)
        {
            return false;
        }

        return engine->WriteFile(SafeUtf8String(filePath), SafeUtf8String(content));
    }

    void FreeAICodeString(wchar_t* value)
    {
        delete[] value;
    }

    void FreeCompletionResponse(NativeCompletionResponse* response)
    {
        if (response == nullptr)
        {
            return;
        }

        FreeAICodeString(response->CompletionText);
        FreeAICodeString(response->ModelUsed);
        FreeAICodeString(response->ErrorMessage);
        response->CompletionText = nullptr;
        response->ModelUsed = nullptr;
        response->ErrorMessage = nullptr;
    }
}
