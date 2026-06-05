#pragma once
#include "pch.h"
#include "ModelClient.h"

class PipeServer
{
public:
    explicit PipeServer(const std::string& pipeName = "AICodeAssistant_Pipe");
    ~PipeServer();

    void Start();
    void Stop();
    void SetModelClient(std::unique_ptr<ModelClient> client);

private:
    void ServerThread();
    void HandleClient(HANDLE hPipe);

    std::string m_pipePath;
    HANDLE m_hStopEvent;
    std::unique_ptr<std::thread> m_serverThread;
    std::unique_ptr<ModelClient> m_modelClient;
};