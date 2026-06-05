#pragma once
#include "pch.h"

// 前向声明，解决循环依赖
class ModelClient;
class PipeServer;

// 导出宏定义（必须在项目属性中添加AICODECORE_EXPORTS）
#ifdef AICODECORE_EXPORTS
#define AICODE_API __declspec(dllexport)
#else
#define AICODE_API __declspec(dllimport)
#endif

extern "C" {
    AICODE_API BOOL __stdcall InitializeCore(const char* apiKey, const char* model);
    AICODE_API void __stdcall ShutdownCore();
    AICODE_API BOOL __stdcall IsCoreInitialized();
    AICODE_API void __stdcall GenerateCode(const char* prompt, void(*callback)(const char*));
}