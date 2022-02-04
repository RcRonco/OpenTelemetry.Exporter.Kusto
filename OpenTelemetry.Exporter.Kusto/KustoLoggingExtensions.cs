using Kusto.Cloud.Platform.Utils;
using OpenTelemetry.Logs;
using System;

namespace OpenTelemetry.Exporter.Kusto
{
    public static class KustoLoggingExtensions
    {
        public static OpenTelemetryLoggerOptions AddKustoLogExporter(this OpenTelemetryLoggerOptions options, Action<KustoLogExporterOptions> configure)
        {
            Ensure.ArgIsNotNull(options, nameof(options));

            var kustoOptions = new KustoLogExporterOptions();
            configure?.Invoke(kustoOptions);
            var exporter = new KustoLogExporter(kustoOptions);
            
            return options.AddProcessor(new BatchLogRecordExportProcessor(exporter));
        }
    }
}
