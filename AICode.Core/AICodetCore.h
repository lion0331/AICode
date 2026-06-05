#pragma once
#include "pch.h"
#include "PipeServer.h"

#ifdef AICODEASSISTANTCORE_EXPORTS
#define AICODEASSISTANTCORE_API __declspec(dllexport)
#else
#define AICODEASSISTANTCORE_API __declspec(dllimport)
#endif

extern "C"
{
    AICODEASSISTANTCORE_API BOOL InitializeCore(const char* apiKey, const char* model);
    AICODEASSISTANTCORE_API void ShutdownCore();
    AICODEASSISTANTCORE_API BOOL IsCoreInitialized();
}