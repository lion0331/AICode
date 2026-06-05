#include "pch.h"
#include "AICodeCore.h"
#include "PipeServer.h"
#include "ModelClient.h"

// 全局单例 + 线程安全锁
static std::unique_ptr<ModelClient> g_modelClient;
static std::unique_ptr<PipeServer> g_pipeServer;
static std::mutex g_coreMutex;

extern "C" {
    AICODE_API BOOL __stdcall InitializeCore(const char* apiKey, const char* model)
    {
        std::lock_guard<std::mutex> lock(g_coreMutex);

        if (g_pipeServer != nullptr)
            return TRUE;

        if (apiKey == nullptr || model == nullptr || *apiKey == '\0' || *model == '\0')
            return FALSE;

        try
        {
            g_modelClient = std::make_unique<ModelClient>(apiKey, model);
            g_pipeServer = std::make_unique<PipeServer>();
            g_pipeServer->SetModelClient(std::move(g_modelClient));
            g_pipeServer->Start();
            return TRUE;
        }
        catch (const std::exception& e)
        {
            std::cerr << "初始化失败: " << e.what() << std::endl;
            g_pipeServer.reset();
            g_modelClient.reset();
            return FALSE;
        }
    }

    AICODE_API void __stdcall ShutdownCore()
    {
        std::lock_guard<std::mutex> lock(g_coreMutex);

        if (g_pipeServer)
        {
            g_pipeServer->Stop();
            g_pipeServer.reset();
        }
        g_modelClient.reset();
    }

    AICODE_API BOOL __stdcall IsCoreInitialized()
    {
        std::lock_guard<std::mutex> lock(g_coreMutex);
        return (g_pipeServer != nullptr);
    }

    AICODE_API void __stdcall GenerateCode(const char* prompt, void(*callback)(const char*))
    {
        if (g_modelClient && callback)
        {
            // 修复：将C风格函数指针包装为std::function
            g_modelClient->GenerateStreamAsync(prompt, [callback](const std::string& chunk)
                {
                    callback(chunk.c_str());
                });
        }
    }
}