#include "pch.h"
#include "PipeServer.h"

PipeServer::PipeServer(const std::string& pipeName)
    : m_pipePath("\\\\.\\pipe\\" + pipeName)
{
    m_hStopEvent = CreateEventA(NULL, TRUE, FALSE, NULL);
    if (m_hStopEvent == NULL)
    {
        throw std::runtime_error("创建停止事件失败");
    }
}

PipeServer::~PipeServer()
{
    Stop();
    if (m_hStopEvent != NULL)
    {
        CloseHandle(m_hStopEvent);
        m_hStopEvent = NULL;
    }
}

void PipeServer::SetModelClient(std::unique_ptr<ModelClient> client)
{
    m_modelClient = std::move(client);
}

void PipeServer::Start()
{
    if (m_serverThread && m_serverThread->joinable())
        return;

    m_serverThread = std::make_unique<std::thread>(&PipeServer::ServerThread, this);
}

void PipeServer::Stop()
{
    if (m_hStopEvent != NULL)
    {
        SetEvent(m_hStopEvent);
    }

    if (m_serverThread && m_serverThread->joinable())
    {
        m_serverThread->join();
        m_serverThread.reset();
    }
}

void PipeServer::ServerThread()
{
    while (WaitForSingleObject(m_hStopEvent, 0) != WAIT_OBJECT_0)
    {
        HANDLE hPipe = CreateNamedPipeA(
            m_pipePath.c_str(),
            PIPE_ACCESS_DUPLEX | FILE_FLAG_OVERLAPPED,
            PIPE_TYPE_MESSAGE | PIPE_READMODE_MESSAGE | PIPE_WAIT,
            PIPE_UNLIMITED_INSTANCES,
            65536,
            65536,
            0,
            NULL
        );

        if (hPipe == INVALID_HANDLE_VALUE)
        {
            Sleep(100);
            continue;
        }

        OVERLAPPED overlapped = { 0 };
        overlapped.hEvent = CreateEventA(NULL, TRUE, FALSE, NULL);

        BOOL connectResult = ConnectNamedPipe(hPipe, &overlapped);
        DWORD lastError = GetLastError();

        if (!connectResult && lastError != ERROR_IO_PENDING)
        {
            CloseHandle(hPipe);
            CloseHandle(overlapped.hEvent);
            continue;
        }

        HANDLE handles[] = { m_hStopEvent, overlapped.hEvent };
        DWORD waitResult = WaitForMultipleObjects(2, handles, FALSE, INFINITE);

        if (waitResult == WAIT_OBJECT_0)
        {
            CancelIo(hPipe);
            CloseHandle(hPipe);
            CloseHandle(overlapped.hEvent);
            break;
        }
        else if (waitResult == WAIT_OBJECT_0 + 1)
        {
            CloseHandle(overlapped.hEvent);
            std::thread clientThread(&PipeServer::HandleClient, this, hPipe);
            clientThread.detach();
        }
        else
        {
            CloseHandle(hPipe);
            CloseHandle(overlapped.hEvent);
        }
    }
}

void PipeServer::HandleClient(HANDLE hPipe)
{
    if (hPipe == INVALID_HANDLE_VALUE)
        return;

    char buffer[65536] = { 0 };
    DWORD bytesRead = 0;

    if (!ReadFile(hPipe, buffer, sizeof(buffer) - 1, &bytesRead, NULL))
    {
        CloseHandle(hPipe);
        return;
    }

    buffer[bytesRead] = '\0';
    std::string request = buffer;

    try
    {
        nlohmann_json j = nlohmann_json::parse(request);
        std::string type = j["type"];

        if (type == "generate_stream" && m_modelClient)
        {
            std::string prompt = j["prompt"];

            m_modelClient->GenerateStreamAsync(prompt, [hPipe](const std::string& chunk)
                {
                    if (chunk.empty()) return;

                    DWORD bytesWritten = 0;
                    WriteFile(hPipe, chunk.c_str(), chunk.length(), &bytesWritten, NULL);
                });
        }
        else if (type == "generate" && m_modelClient)
        {
            std::string prompt = j["prompt"];
            std::string result = m_modelClient->GenerateSync(prompt);

            DWORD bytesWritten = 0;
            WriteFile(hPipe, result.c_str(), result.length(), &bytesWritten, NULL);
        }
    }
    catch (const std::exception& e)
    {
        std::string error = "错误: " + std::string(e.what());
        DWORD bytesWritten = 0;
        WriteFile(hPipe, error.c_str(), error.length(), &bytesWritten, NULL);
    }

    FlushFileBuffers(hPipe);
    DisconnectNamedPipe(hPipe);
    CloseHandle(hPipe);
}