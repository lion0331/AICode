#include "PipeServer.h"

PipeServer::PipeServer(const std::string& pipeName)
    : m_pipeName("\\\\.\\pipe\\" + pipeName)
{
    m_hStopEvent = CreateEventA(NULL, TRUE, FALSE, NULL);
}

PipeServer::~PipeServer()
{
    Stop();
    CloseHandle(m_hStopEvent);
}

void PipeServer::Start()
{
    m_serverThread = std::make_unique<std::thread>(&PipeServer::ServerThread, this);
}

void PipeServer::Stop()
{
    SetEvent(m_hStopEvent);
    if (m_serverThread && m_serverThread->joinable())
    {
        m_serverThread->join();
    }
}

void PipeServer::ServerThread()
{
    while (WaitForSingleObject(m_hStopEvent, 0) != WAIT_OBJECT_0)
    {
        HANDLE hPipe = CreateNamedPipeA(
            m_pipeName.c_str(),
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

        if (ConnectNamedPipe(hPipe, &overlapped) == FALSE && GetLastError() != ERROR_IO_PENDING)
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
    }
}

void PipeServer::HandleClient(HANDLE hPipe)
{
    char buffer[65536];
    DWORD bytesRead;

    if (!ReadFile(hPipe, buffer, sizeof(buffer) - 1, &bytesRead, NULL))
    {
        CloseHandle(hPipe);
        return;
    }

    buffer[bytesRead] = '\0';
    std::string request = buffer;

    try
    {
        json j = json::parse(request);
        std::string type = j["type"];

        if (type == "generate_stream" && m_modelClient)
        {
            std::string prompt = j["prompt"];

            m_modelClient->GenerateStreamAsync(prompt, [hPipe](const std::string& chunk)
                {
                    DWORD bytesWritten;
                    WriteFile(hPipe, chunk.c_str(), chunk.length(), &bytesWritten, NULL);
                }).wait();

            const char* endMarker = "[END]";
            WriteFile(hPipe, endMarker, strlen(endMarker), &bytesRead, NULL);
        }
        else if (type == "generate" && m_modelClient)
        {
            std::string prompt = j["prompt"];
            std::string result = m_modelClient->GenerateAsync(prompt).get();

            DWORD bytesWritten;
            WriteFile(hPipe, result.c_str(), result.length(), &bytesWritten, NULL);
        }
    }
    catch (...)
    {
        const char* error = "错误：请求处理失败";
        WriteFile(hPipe, error, strlen(error), &bytesRead, NULL);
    }

    FlushFileBuffers(hPipe);
    DisconnectNamedPipe(hPipe);
    CloseHandle(hPipe);
}