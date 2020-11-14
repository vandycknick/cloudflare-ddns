using System;
using Serilog.Events;
using Xunit;

namespace CloudflareDDNS.Tests
{
    public class UtilsTests
    {
        [Fact]
        public void Utils_MapToLogEventLevel_ThrowsAnArgumentExceptionForAnUnsupportedLogLevel()
        {
            // Given
            var logLevel = "unsupported";

            // When
            var exception = Assert.Throws<ArgumentException>(() => Utils.MapToLogEventLevel(logLevel));

            // Then
            Assert.Equal($"Invalid log level provided ({logLevel}), only verbose, info, warning, error are supported!", exception.Message);
        }

        [Theory]
        [InlineData("verbose", LogEventLevel.Verbose)]
        [InlineData("info", LogEventLevel.Information)]
        [InlineData("warning", LogEventLevel.Warning)]
        [InlineData("error", LogEventLevel.Error)]
        public void Utils_MapToLogEventLevel_MapsAStringToALogEventLevel(string logLevel, LogEventLevel expected)
        {
            // Given, When
            var result = Utils.MapToLogEventLevel(logLevel);

            //Then
            Assert.Equal(expected, result);
        }
    }
}
