using System;
using System.Runtime.InteropServices;
using System.Text;

namespace AICode.Vsix
{
    public enum ModelType
    {
        GPT4o,
        GPT4Turbo,
        Claude35Sonnet,
        Claude3Opus,
        GeminiPro
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct CompletionRequest
    {
        [MarshalAs(UnmanagedType.LPStr)] public string FilePath;
        [MarshalAs(UnmanagedType.LPStr)] public string Language;
        [MarshalAs(UnmanagedType.LPStr)] public string CodeBeforeCursor;
        [MarshalAs(UnmanagedType.LPStr)] public string CodeAfterCursor;
        public int LineNumber;
        public int ColumnNumber;
        public int MaxTokens;
        public float Temperature;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct CompletionResponse
    {
        [MarshalAs(UnmanagedType.LPStr)] public string CompletionText;
        [MarshalAs(UnmanagedType.LPStr)] public string ModelUsed;
        public int TokensUsed;
        [MarshalAs(UnmanagedType.Bool)] public bool Success;
        [MarshalAs(UnmanagedType.LPStr)] public string ErrorMessage;
    }

    public delegate void CompletionCallback(in CompletionResponse response);

    internal static class AICodeEngineInterop
    {
        private const string DllName = "AICode.Core.dll";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateAICodeEngine();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ReleaseAICodeEngine(IntPtr engine);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool Initialize(IntPtr engine, ModelType modelType, 
            [MarshalAs(UnmanagedType.LPStr)] string apiKey, 
            [MarshalAs(UnmanagedType.LPStr)] string apiBaseUrl);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern CompletionResponse GetCodeCompletion(IntPtr engine, in CompletionRequest request);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void GetCodeCompletionAsync(IntPtr engine, in CompletionRequest request, 
            CompletionCallback callback);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern CompletionResponse GenerateCode(IntPtr engine, 
            [MarshalAs(UnmanagedType.LPStr)] string prompt,
            [MarshalAs(UnmanagedType.LPStr)] string language,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr, SizeParamIndex = 4)] string[] contextFiles,
            int contextFilesCount,
            int maxTokens,
            float temperature);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern CompletionResponse RefactorCode(IntPtr engine,
            [MarshalAs(UnmanagedType.LPStr)] string filePath,
            [MarshalAs(UnmanagedType.LPStr)] string originalCode,
            [MarshalAs(UnmanagedType.LPStr)] string refactorInstruction,
            int startLine,
            int endLine);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ExplainCode(IntPtr engine,
            [MarshalAs(UnmanagedType.LPStr)] string code,
            [MarshalAs(UnmanagedType.LPStr)] string language);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr FindCodeIssues(IntPtr engine,
            [MarshalAs(UnmanagedType.LPStr)] string code,
            [MarshalAs(UnmanagedType.LPStr)] string language);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ReadFile(IntPtr engine, [MarshalAs(UnmanagedType.LPStr)] string filePath);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WriteFile(IntPtr engine, [MarshalAs(UnmanagedType.LPStr)] string filePath,
            [MarshalAs(UnmanagedType.LPStr)] string content);

        // 辅助方法：释放字符串指针
        public static string PtrToStringAndFree(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                return string.Empty;

            string result = Marshal.PtrToStringAnsi(ptr);
            Marshal.FreeCoTaskMem(ptr);
            return result;
        }
    }

    // 托管包装类
    public class AICodeEngine : IDisposable
    {
        private IntPtr _engine;
        private bool _disposed;

        public AICodeEngine()
        {
            _engine = AICodeEngineInterop.CreateAICodeEngine();
            if (_engine == IntPtr.Zero)
            {
                throw new InvalidOperationException("无法创建AICode引擎实例");
            }
        }

        public bool Initialize(ModelType modelType, string apiKey, string apiBaseUrl = "")
        {
            return AICodeEngineInterop.Initialize(_engine, modelType, apiKey, apiBaseUrl);
        }

        public CompletionResponse GetCodeCompletion(CompletionRequest request)
        {
            return AICodeEngineInterop.GetCodeCompletion(_engine, request);
        }

        public void GetCodeCompletionAsync(CompletionRequest request, Action<CompletionResponse> callback)
        {
            AICodeEngineInterop.GetCodeCompletionAsync(_engine, request, response => callback(response));
        }

        public CompletionResponse GenerateCode(string prompt, string language, string[] contextFiles = null, 
            int maxTokens = 4096, float temperature = 0.7f)
        {
            contextFiles ??= Array.Empty<string>();
            return AICodeEngineInterop.GenerateCode(_engine, prompt, language, contextFiles, 
                contextFiles.Length, maxTokens, temperature);
        }

        public CompletionResponse RefactorCode(string filePath, string originalCode, string refactorInstruction, 
            int startLine, int endLine)
        {
            return AICodeEngineInterop.RefactorCode(_engine, filePath, originalCode, refactorInstruction, 
                startLine, endLine);
        }

        public string ExplainCode(string code, string language)
        {
            IntPtr ptr = AICodeEngineInterop.ExplainCode(_engine, code, language);
            return AICodeEngineInterop.PtrToStringAndFree(ptr);
        }

        public string FindCodeIssues(string code, string language)
        {
            IntPtr ptr = AICodeEngineInterop.FindCodeIssues(_engine, code, language);
            return AICodeEngineInterop.PtrToStringAndFree(ptr);
        }

        public string ReadFile(string filePath)
        {
            IntPtr ptr = AICodeEngineInterop.ReadFile(_engine, filePath);
            return AICodeEngineInterop.PtrToStringAndFree(ptr);
        }

        public bool WriteFile(string filePath, string content)
        {
            return AICodeEngineInterop.WriteFile(_engine, filePath, content);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (_engine != IntPtr.Zero)
            {
                AICodeEngineInterop.ReleaseAICodeEngine(_engine);
                _engine = IntPtr.Zero;
            }

            _disposed = true;
        }

        ~AICodeEngine()
        {
            Dispose(false);
        }
    }
}
