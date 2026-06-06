#pragma once

#include "AICodeCore.h"
#include "OpenAIClient.h"
#include <memory>
#include <thread>
#include <queue>
#include <mutex>
#include <condition_variable>

namespace AICode::Core
{
    class AICodeEngine : public IAICodeEngine
    {
    public:
        AICodeEngine();
        ~AICodeEngine() override;

        bool Initialize(ModelType modelType, const std::string& apiKey, const std::string& apiBaseUrl) override;

        CompletionResponse GetCodeCompletion(const CompletionRequest& request) override;
        CompletionResponse GenerateCode(const GenerationRequest& request) override;
        CompletionResponse RefactorCode(const RefactorRequest& request) override;
        std::string ExplainCode(const std::string& code, const std::string& language) override;
        std::string FindCodeIssues(const std::string& code, const std::string& language) override;

        std::string ReadFile(const std::string& filePath) override;
        bool WriteFile(const std::string& filePath, const std::string& content) override;

        void GetCodeCompletionAsync(const CompletionRequest& request, 
            std::function<void(const CompletionResponse&)> callback) override;

    private:
        std::unique_ptr<OpenAIClient> m_client;
        bool m_initialized;

        // 异步任务队列
        struct AsyncTask
        {
            CompletionRequest request;
            std::function<void(const CompletionResponse&)> callback;
        };

        std::queue<AsyncTask> m_taskQueue;
        std::mutex m_queueMutex;
        std::condition_variable m_queueCondition;
        std::thread m_workerThread;
        bool m_workerRunning;

        void WorkerThread();
        nlohmann::json BuildCompletionMessages(const CompletionRequest& request);
        nlohmann::json BuildGenerationMessages(const GenerationRequest& request);
    };
}
