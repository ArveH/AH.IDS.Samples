using System.Text;
using ILogger = Serilog.ILogger;

namespace Api.Net7.Helpers;

public static class LoggerExtensions
{
    public static void LogHeaders(this ILogger logger, string heading, IHeaderDictionary headers)
    {
        var sb = new StringBuilder();
        sb.AppendLine(heading);
        foreach (var header in headers)
        {
            sb.AppendLine($"{header.Key}: {header.Value}");
        }

        logger.Debug(sb.ToString());
    }
}