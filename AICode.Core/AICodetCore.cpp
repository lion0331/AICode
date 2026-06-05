#include "AICodeCore.h"

std::unique_ptr<PipeServer> g_pipeServer;
std::unique_ptr<ModelClient> g_modelClient;

AICODEASSISTANTCORE_API BOOL InitializeCore(const char* apiKey, const char* model)
{
    if (g_pipeServer) return TRUE;

    try
    {
        g_modelClient = std::make_unique<ModelClient>(apiKey, model);
        g_pipeServer = std::make_unique<PipeServer>();
        g_pipeServer->SetModelClient(std::move(g_modelClient));
        g_pipeServer->Start();
        return TRUE;
    }
    catch (...)
    {
        return FALSE;
    }
}

AICODEASSISTANTCORE_API void ShutdownCore()
{
    if (g_pipeServer)
    {
        g_pipeServer->Stop();
        g_pipeServer.reset();
    }
    g_modelClient.reset();
}

AICODEASSISTANTCORE_API BOOL IsCoreInitialized()
{
    return g_pipeServer != nullptr;
}