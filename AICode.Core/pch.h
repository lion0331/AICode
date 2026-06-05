#pragma once

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <wininet.h>

// 标准库
#include <string>
#include <vector>
#include <memory>
#include <functional>
#include <thread>
#include <mutex>
#include <sstream>
#include <stdexcept>
#include <iostream>

// 第三方库
#include <nlohmann/json.hpp>
using nlohmann_json = nlohmann::json;

#pragma comment(lib, "wininet.lib")