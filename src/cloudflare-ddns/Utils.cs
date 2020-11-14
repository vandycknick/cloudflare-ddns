using System;
using Serilog.Events;

namespace CloudflareDDNS
{
    public static class Utils
    {
        public static LogEventLevel MapToLogEventLevel(string logLevel) =>
            logLevel switch
            {
                "verbose" => LogEventLevel.Verbose,
                "info" => LogEventLevel.Information,
                "warning" => LogEventLevel.Warning,
                "error" => LogEventLevel.Error,
                _ => throw new ArgumentException($"Invalid log level provided ({logLevel}), only verbose, info, warning, error are supported!")
            };
    }
}
