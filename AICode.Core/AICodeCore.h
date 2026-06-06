#pragma once

#include <string>
#include <vector>
#include <functional>

#ifdef AICODECORE_EXPORTS
#define AICODECORE_API __declspec(dllexport)
#else
#define AICODECORE_API __declspec(dllimport)
#endif

namespace AICode::Core
{
    // 大模型类型
    enum class ModelType
    {
        GPT4o,
        GPT4Turbo,
        Claude35Sonnet,
        Claude3Opus,
        GeminiPro
    };

    // 代码补全请求
    struct CompletionRequest
    {
        std::string FilePath;
        std::string Language; // "cpp", "csharp", "python"等
        std::string CodeBeforeCursor;
        std::string CodeAfterCursor;
        int LineNumber;
        int ColumnNumber;
        int MaxTokens = 1024;
        float Temperature = 0.2f;
    };

    // 代码补全响应
    struct CompletionResponse
    {
        std::string CompletionText;
        std::string ModelUsed;
        int TokensUsed;
        bool Success;
        std::string ErrorMessage;
    };

    // 代码生成请求
    struct GenerationRequest
    {
        std::string Prompt;
        std::string Language;
        std::vector<std::string> ContextFiles; // 相关文件路径
        int MaxTokens = 4096;
        float Temperature = 0.7f;
    };

    // 代码重构请求
    struct RefactorRequest
    {
        std::string FilePath;
        std::string OriginalCode;
        std::string RefactorInstruction;
        int StartLine;
        int EndLine;
    };

    // 核心引擎接口
    class AICODECORE_API IAICodeEngine
    {
    public:
        virtual ~IAICodeEngine() = default;

        // 初始化引擎
        virtual bool Initialize(ModelType modelType, const std::string& apiKey, const std::string& apiBaseUrl = "") = 0;

        // 代码补全
        virtual CompletionResponse GetCodeCompletion(const CompletionRequest& request) = 0;

        // 代码生成
        virtual CompletionResponse GenerateCode(const GenerationRequest& request) = 0;

        // 代码重构
        virtual CompletionResponse RefactorCode(const RefactorRequest& request) = 0;

        // 解释代码
        virtual std::string ExplainCode(const std::string& code, const std::string& language) = 0;

        // 查找代码问题
        virtual std::string FindCodeIssues(const std::string& code, const std::string& language) = 0;

        // 读取文件内容
        virtual std::string ReadFile(const std::string& filePath) = 0;

        // 写入文件内容
        virtual bool WriteFile(const std::string& filePath, const std::string& content) = 0;

        // 异步版本接口
        virtual void GetCodeCompletionAsync(const CompletionRequest& request, 
            std::function<void(const CompletionResponse&)> callback) = 0;
    };

    // 创建引擎实例
    AICODECORE_API IAICodeEngine* CreateAICodeEngine();

    // 释放引擎实例
    AICODECORE_API void ReleaseAICodeEngine(IAICodeEngine* engine);
}
