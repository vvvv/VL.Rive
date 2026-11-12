using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static VL.Rive.Interop.Methods;

namespace VL.Rive.Internal;

/// <summary>
/// Routes what Rive scripts print, and their Lua errors, to the vvvv log. Without this the native runtime writes console output to stdout and drops errors.
/// </summary>
internal static unsafe class ScriptLog
{
    static ILogger? logger;

    /// <summary>
    /// Makes <paramref name="logger"/> receive the output of scripts in files imported from now on. With several renderers the most recent one wins.
    /// </summary>
    public static void Attach(ILogger logger)
    {
        ScriptLog.logger = logger;
        rive_Scripting_SetLogCallback(&OnLog);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    static void OnLog(int level, sbyte* message, nuint length)
    {
        try
        {
            var text = Encoding.UTF8.GetString((byte*)message, (int)length);
            var logLevel = level switch
            {
                1 => LogLevel.Warning,
                2 => LogLevel.Error,
                _ => LogLevel.Information
            };
            logger?.Log(logLevel, "Rive script: {Message}", text);
        }
        catch
        {
            // Must not throw into native code
        }
    }
}
