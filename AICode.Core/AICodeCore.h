#pragma once

#include <functional>
#include <string>
#include <vector>

#ifdef AICODECORE_EXPORTS
#define AICODECORE_API __declspec(dllexport)
#else
#define AICODECORE_API __declspec(dllimport)
#endif

namespace AICode::Core
{
    enum class ModelType
    {
        GPT4o,
        GPT4Turbo,
        Claude35Sonnet,
        Claude3Opus,
        GeminiPro
    };

    struct CompletionRequest
    {
        std::string FilePath;
        std::string Language;
        std::string CodeBeforeCursor;
        std::string CodeAfterCursor;
        int LineNumber = 0;
        int ColumnNumber = 0;
        int MaxTokens = 1024;
        float Temperature = 0.2f;
    };

    struct CompletionResponse
    {
        std::string CompletionText;
        std::string ModelUsed;
        int TokensUsed = 0;
        bool Success = false;
        std::string ErrorMessage;
    };

    struct GenerationRequest
    {
        std::string Prompt;
        std::string Language;
        std::vector<std::string> ContextFiles;
        int MaxTokens = 4096;
        float Temperature = 0.7f;
    };

    struct RefactorRequest
    {
        std::string FilePath;
        std::string OriginalCode;
        std::string RefactorInstruction;
        int StartLine = 0;
        int EndLine = 0;
    };

    struct NativeCompletionResponse
    {
        wchar_t* CompletionText;
        wchar_t* ModelUsed;
        int TokensUsed;
        bool Success;
        wchar_t* ErrorMessage;
    };

    using CompletionCallback = void(*)(NativeCompletionResponse response, void* userData);

    class AICODECORE_API IAICodeEngine
    {
    public:
        virtual ~IAICodeEngine() = default;

        virtual bool Initialize(ModelType modelType, const std::string& apiKey, const std::string& apiBaseUrl = "") = 0;
        virtual CompletionResponse GetCodeCompletion(const CompletionRequest& request) = 0;
        virtual CompletionResponse GenerateCode(const GenerationRequest& request) = 0;
        virtual CompletionResponse RefactorCode(const RefactorRequest& request) = 0;
        virtual std::string ExplainCode(const std::string& code, const std::string& language) = 0;
        virtual std::string FindCodeIssues(const std::string& code, const std::string& language) = 0;
        virtual std::string ReadFile(const std::string& filePath) = 0;
        virtual bool WriteFile(const std::string& filePath, const std::string& content) = 0;
        virtual void GetCodeCompletionAsync(const CompletionRequest& request,
            std::function<void(const CompletionResponse&)> callback) = 0;
    };
}

extern "C"
{
    AICODECORE_API AICode::Core::IAICodeEngine* CreateAICodeEngine();
    AICODECORE_API void ReleaseAICodeEngine(AICode::Core::IAICodeEngine* engine);
    AICODECORE_API bool InitializeAICodeEngine(
        AICode::Core::IAICodeEngine* engine,
        AICode::Core::ModelType modelType,
        const wchar_t* apiKey,
        const wchar_t* apiBaseUrl);
    AICODECORE_API AICode::Core::NativeCompletionResponse GetCodeCompletion(
        AICode::Core::IAICodeEngine* engine,
        const wchar_t* filePath,
        const wchar_t* language,
        const wchar_t* codeBeforeCursor,
        const wchar_t* codeAfterCursor,
        int lineNumber,
        int columnNumber,
        int maxTokens,
        float temperature);
    AICODECORE_API void GetCodeCompletionAsync(
        AICode::Core::IAICodeEngine* engine,
        const wchar_t* filePath,
        const wchar_t* language,
        const wchar_t* codeBeforeCursor,
        const wchar_t* codeAfterCursor,
        int lineNumber,
        int columnNumber,
        int maxTokens,
        float temperature,
        AICode::Core::CompletionCallback callback,
        void* userData);
    AICODECORE_API AICode::Core::NativeCompletionResponse GenerateCode(
        AICode::Core::IAICodeEngine* engine,
        const wchar_t* prompt,
        const wchar_t* language,
        const wchar_t* const* contextFiles,
        int contextFilesCount,
        int maxTokens,
        float temperature);
    AICODECORE_API AICode::Core::NativeCompletionResponse RefactorCode(
        AICode::Core::IAICodeEngine* engine,
        const wchar_t* filePath,
        const wchar_t* originalCode,
        const wchar_t* refactorInstruction,
        int startLine,
        int endLine);
    AICODECORE_API wchar_t* ExplainCode(
        AICode::Core::IAICodeEngine* engine,
        const wchar_t* code,
        const wchar_t* language);
    AICODECORE_API wchar_t* FindCodeIssues(
        AICode::Core::IAICodeEngine* engine,
        const wchar_t* code,
        const wchar_t* language);
    AICODECORE_API wchar_t* ReadAICodeFile(
        AICode::Core::IAICodeEngine* engine,
        const wchar_t* filePath);
    AICODECORE_API bool WriteAICodeFile(
        AICode::Core::IAICodeEngine* engine,
        const wchar_t* filePath,
        const wchar_t* content);
    AICODECORE_API void FreeAICodeString(wchar_t* value);
    AICODECORE_API void FreeCompletionResponse(AICode::Core::NativeCompletionResponse* response);
}
