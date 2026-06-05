#pragma once
#include "pch.h"
#include "ModelClient.h"

class PipeServer
{
public:
    PipeServer(const std::string& pipeName = "AICodeAssistant_Pipe");
    ~PipeServer();

    void Start();
    void Stop();
    void SetModelClient(std::unique_ptr<ModelClient> client)
    {
        m_modelClient = std::move(client);
    }

private:
    void ServerThread();
    void HandleClient(HANDLE hPipe);

    std::string m_pipeName;
    HANDLE m_hStopEvent;
    std::unique_ptr<std::thread> m_serverThread;
    std::unique_ptr<ModelClient> m_modelClient;
};