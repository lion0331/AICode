#pragma once
#include <windows.h>
#include <string>
#include <vector>
#include <memory>
#include <functional>
#include <thread>
#include <sstream>
#include <cpprest/http_client.h>
#include <cpprest/json.h>
#include <nlohmann/json.hpp>

using json = nlohmann::json;
using namespace web;
using namespace web::http;
using namespace web::http::client;