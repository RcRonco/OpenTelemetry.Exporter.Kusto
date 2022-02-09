using Kusto.Cloud.Platform.Utils;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OpenTelemetry.Exporter.Kusto
{
    internal static class OpenTelemetryExtensions
    {
        #region Private Members
        private static readonly JsonSerializerOptions s_serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            IncludeFields = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };
        #endregion

        /// <summary>
        /// Converts <see cref="LogLevel"/> to Severity number
        /// https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/logs/data-model.md#field-severitynumber
        /// </summary>
        /// <param name="level">Log level</param>
        /// <returns>Log severity number</returns>
        public static string ToSeverityNumberValue(this LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace: return "1";
                case LogLevel.Debug: return "5";
                case LogLevel.Information: return "9";
                case LogLevel.Warning: return "13";
                case LogLevel.Error: return "17";
                case LogLevel.Critical: return "21";
                case LogLevel.None:
                default: return "0";
            }
        }

        /// <summary>
        /// Converts <see cref="LogLevel"/> to OpenTelemetry Severity text
        /// https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/logs/data-model.md#field-severitytext
        /// </summary>
        /// <param name="level">Log level</param>
        /// <returns>Log severity number</returns>
        public static string ToSeverityString(this LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace: return "TRACE";
                case LogLevel.Debug: return "DEBUG";
                case LogLevel.Information: return "INFO";
                case LogLevel.Warning: return "WARN";
                case LogLevel.Error: return "ERROR";
                case LogLevel.Critical: return "FATAL";
                case LogLevel.None:
                default: return "NONE";
            }
        }

        public static string ToKustoOTelString(this Resource resource)
        {
            return JsonSerializer.Serialize(new Dictionary<string, object>(resource.Attributes), s_serializerOptions);
        }

        public static string ToKustoOTelString(this DateTime dateTime, bool useKustoDateTime)
        {
            if (useKustoDateTime)
            {
                return dateTime.FastToString();
            }
            else
            {
                // TODO: Handle overflow of errors
                // Convert to unix milliseconds sinse 
                return checked(dateTime.Subtract(DateTime.UnixEpoch).TotalMilliseconds * 1000).ToString();
            }
        }
    }
}
