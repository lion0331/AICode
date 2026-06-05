using System.Runtime.InteropServices;

namespace AICode;

internal static class NativeMethods
{
    private const string DllName = "AICode.Core.dll";

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool InitializeCore(string apiKey, string model);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ShutdownCore();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsCoreInitialized();
}