using System;
using System.Runtime.InteropServices;

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

    public struct CompletionRequest
    {
        public string FilePath;
        public string Language;
        public string CodeBeforeCursor;
        public string CodeAfterCursor;
        public int LineNumber;
        public int ColumnNumber;
        public int MaxTokens;
        public float Temperature;
    }

    public struct CompletionResponse
    {
        public string CompletionText;
        public string ModelUsed;
        public int TokensUsed;
        public bool Success;
        public string ErrorMessage;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeCompletionResponse
    {
        public IntPtr CompletionText;
        public IntPtr ModelUsed;
        public int TokensUsed;

        [MarshalAs(UnmanagedType.I1)]
        public bool Success;

        public IntPtr ErrorMessage;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void NativeCompletionCallback(NativeCompletionResponse response, IntPtr userData);

    internal static class AICodeEngineInterop
    {
        private const string DllName = "AICode.Core.dll";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern IntPtr CreateAICodeEngine();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void ReleaseAICodeEngine(IntPtr engine);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true, EntryPoint = "InitializeAICodeEngine")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool Initialize(
            IntPtr engine,
            ModelType modelType,
            [MarshalAs(UnmanagedType.LPWStr)] string apiKey,
            [MarshalAs(UnmanagedType.LPWStr)] string apiBaseUrl);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern NativeCompletionResponse GetCodeCompletion(
            IntPtr engine,
            [MarshalAs(UnmanagedType.LPWStr)] string filePath,
            [MarshalAs(UnmanagedType.LPWStr)] string language,
            [MarshalAs(UnmanagedType.LPWStr)] string codeBeforeCursor,
            [MarshalAs(UnmanagedType.LPWStr)] string codeAfterCursor,
            int lineNumber,
            int columnNumber,
            int maxTokens,
            float temperature);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void GetCodeCompletionAsync(
            IntPtr engine,
            [MarshalAs(UnmanagedType.LPWStr)] string filePath,
            [MarshalAs(UnmanagedType.LPWStr)] string language,
            [MarshalAs(UnmanagedType.LPWStr)] string codeBeforeCursor,
            [MarshalAs(UnmanagedType.LPWStr)] string codeAfterCursor,
            int lineNumber,
            int columnNumber,
            int maxTokens,
            float temperature,
            NativeCompletionCallback callback,
            IntPtr userData);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true, EntryPoint = "GenerateCode")]
        public static extern NativeCompletionResponse GenerateCode(
            IntPtr engine,
            [MarshalAs(UnmanagedType.LPWStr)] string prompt,
            [MarshalAs(UnmanagedType.LPWStr)] string language,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)] string[] contextFiles,
            int contextFilesCount,
            int maxTokens,
            float temperature);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true, EntryPoint = "RefactorCode")]
        public static extern NativeCompletionResponse RefactorCode(
            IntPtr engine,
            [MarshalAs(UnmanagedType.LPWStr)] string filePath,
            [MarshalAs(UnmanagedType.LPWStr)] string originalCode,
            [MarshalAs(UnmanagedType.LPWStr)] string refactorInstruction,
            int startLine,
            int endLine);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true, EntryPoint = "ExplainCode")]
        public static extern IntPtr ExplainCode(
            IntPtr engine,
            [MarshalAs(UnmanagedType.LPWStr)] string code,
            [MarshalAs(UnmanagedType.LPWStr)] string language);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true, EntryPoint = "FindCodeIssues")]
        public static extern IntPtr FindCodeIssues(
            IntPtr engine,
            [MarshalAs(UnmanagedType.LPWStr)] string code,
            [MarshalAs(UnmanagedType.LPWStr)] string language);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true, EntryPoint = "ReadAICodeFile")]
        public static extern IntPtr ReadFile(IntPtr engine, [MarshalAs(UnmanagedType.LPWStr)] string filePath);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true, EntryPoint = "WriteAICodeFile")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool WriteFile(
            IntPtr engine,
            [MarshalAs(UnmanagedType.LPWStr)] string filePath,
            [MarshalAs(UnmanagedType.LPWStr)] string content);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void FreeAICodeString(IntPtr ptr);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void FreeCompletionResponse(ref NativeCompletionResponse response);

        public static CompletionResponse ToManagedResponse(NativeCompletionResponse nativeResponse)
        {
            try
            {
                return new CompletionResponse
                {
                    CompletionText = PtrToStringAndFree(nativeResponse.CompletionText),
                    ModelUsed = PtrToStringAndFree(nativeResponse.ModelUsed),
                    TokensUsed = nativeResponse.TokensUsed,
                    Success = nativeResponse.Success,
                    ErrorMessage = PtrToStringAndFree(nativeResponse.ErrorMessage)
                };
            }
            finally
            {
                nativeResponse.CompletionText = IntPtr.Zero;
                nativeResponse.ModelUsed = IntPtr.Zero;
                nativeResponse.ErrorMessage = IntPtr.Zero;
            }
        }

        public static string PtrToStringAndFree(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return string.Empty;
            }

            try
            {
                return Marshal.PtrToStringUni(ptr) ?? string.Empty;
            }
            finally
            {
                FreeAICodeString(ptr);
            }
        }
    }

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
            ThrowIfDisposed();
            return AICodeEngineInterop.Initialize(_engine, modelType, apiKey, apiBaseUrl);
        }

        public CompletionResponse GetCodeCompletion(CompletionRequest request)
        {
            ThrowIfDisposed();
            var nativeResponse = AICodeEngineInterop.GetCodeCompletion(
                _engine,
                request.FilePath ?? string.Empty,
                request.Language ?? string.Empty,
                request.CodeBeforeCursor ?? string.Empty,
                request.CodeAfterCursor ?? string.Empty,
                request.LineNumber,
                request.ColumnNumber,
                request.MaxTokens,
                request.Temperature);

            return AICodeEngineInterop.ToManagedResponse(nativeResponse);
        }

        public void GetCodeCompletionAsync(CompletionRequest request, Action<CompletionResponse> callback)
        {
            ThrowIfDisposed();
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            var callbackState = new CompletionCallbackState(callback);
            var handle = GCHandle.Alloc(callbackState);
            callbackState.Handle = handle;

            try
            {
                AICodeEngineInterop.GetCodeCompletionAsync(
                    _engine,
                    request.FilePath ?? string.Empty,
                    request.Language ?? string.Empty,
                    request.CodeBeforeCursor ?? string.Empty,
                    request.CodeAfterCursor ?? string.Empty,
                    request.LineNumber,
                    request.ColumnNumber,
                    request.MaxTokens,
                    request.Temperature,
                    CompletionCallbackState.NativeCallback,
                    GCHandle.ToIntPtr(handle));
            }
            catch
            {
                handle.Free();
                throw;
            }
        }

        public CompletionResponse GenerateCode(string prompt, string language, string[] contextFiles = null,
            int maxTokens = 4096, float temperature = 0.7f)
        {
            ThrowIfDisposed();
            contextFiles ??= Array.Empty<string>();
            var nativeResponse = AICodeEngineInterop.GenerateCode(
                _engine,
                prompt ?? string.Empty,
                language ?? string.Empty,
                contextFiles,
                contextFiles.Length,
                maxTokens,
                temperature);

            return AICodeEngineInterop.ToManagedResponse(nativeResponse);
        }

        public CompletionResponse RefactorCode(string filePath, string originalCode, string refactorInstruction,
            int startLine, int endLine)
        {
            ThrowIfDisposed();
            var nativeResponse = AICodeEngineInterop.RefactorCode(
                _engine,
                filePath ?? string.Empty,
                originalCode ?? string.Empty,
                refactorInstruction ?? string.Empty,
                startLine,
                endLine);

            return AICodeEngineInterop.ToManagedResponse(nativeResponse);
        }

        public string ExplainCode(string code, string language)
        {
            ThrowIfDisposed();
            IntPtr ptr = AICodeEngineInterop.ExplainCode(_engine, code ?? string.Empty, language ?? string.Empty);
            return AICodeEngineInterop.PtrToStringAndFree(ptr);
        }

        public string FindCodeIssues(string code, string language)
        {
            ThrowIfDisposed();
            IntPtr ptr = AICodeEngineInterop.FindCodeIssues(_engine, code ?? string.Empty, language ?? string.Empty);
            return AICodeEngineInterop.PtrToStringAndFree(ptr);
        }

        public string ReadFile(string filePath)
        {
            ThrowIfDisposed();
            IntPtr ptr = AICodeEngineInterop.ReadFile(_engine, filePath ?? string.Empty);
            return AICodeEngineInterop.PtrToStringAndFree(ptr);
        }

        public bool WriteFile(string filePath, string content)
        {
            ThrowIfDisposed();
            return AICodeEngineInterop.WriteFile(_engine, filePath ?? string.Empty, content ?? string.Empty);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

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

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AICodeEngine));
            }
        }

        private sealed class CompletionCallbackState
        {
            internal static readonly NativeCompletionCallback NativeCallback = OnCompletion;

            public CompletionCallbackState(Action<CompletionResponse> callback)
            {
                Callback = callback;
            }

            public Action<CompletionResponse> Callback { get; }

            public GCHandle Handle { get; set; }

            private static void OnCompletion(NativeCompletionResponse nativeResponse, IntPtr userData)
            {
                var handle = GCHandle.FromIntPtr(userData);
                var state = (CompletionCallbackState)handle.Target;

                try
                {
                    state.Callback(AICodeEngineInterop.ToManagedResponse(nativeResponse));
                }
                finally
                {
                    handle.Free();
                }
            }
        }
    }
}
