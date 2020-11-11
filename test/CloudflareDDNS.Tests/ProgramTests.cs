using System;
using Serilog.Events;
using Xunit;

namespace CloudflareDDNS.Tests
{
    public class ProgramTests
    {
        [Fact]
        public void Program_MapToLogEventLevel_ThrowsAnArgumentExceptionForAnUnsupportedLogLevel()
        {
            // Given
            var logLevel = "unsupported";

            // When
            var exception = Assert.Throws<ArgumentException>(() => Program.MapToLogEventLevel(logLevel));

            // Then
            Assert.Equal($"Invalid log level provided ({logLevel}), only verbose, info, warning, error are supported!", exception.Message);
        }

        [Theory]
        [InlineData("verbose", LogEventLevel.Verbose)]
        [InlineData("info", LogEventLevel.Information)]
        [InlineData("warning", LogEventLevel.Warning)]
        [InlineData("error", LogEventLevel.Error)]
        public void Program_MapToLogEventLevel_MapsAStringToALogEventLevel(string logLevel, LogEventLevel expected)
        {
            // Given, When
            var result = Program.MapToLogEventLevel(logLevel);

            //Then
            Assert.Equal(expected, result);
        }
    }
}
