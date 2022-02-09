using Kusto.Data;
using Kusto.Ingest;

namespace OpenTelemetry.Exporter.Kusto
{
    public class KustoLogExporterOptions
    {
        public KustoConnectionStringBuilder ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public string TableName { get; set; }
        public IngestionReportLevel? ReportLevel { get; set; }
        public IngestionReportMethod? ReportMethod { get; set; }
        public string MappingReference { get; set; }

        /// <summary>
        /// If set to true Exporter will send the DateTime values as Kusto's datetime values and not uint64
        /// As described in OpenTelemetry spec all Timestamp data should be sent as "uint64 nanoseconds since Unix epoch"
        /// This is not the recommended format to make readable telemetry on Kusto
        /// </summary>
        public bool UseKustoDateTime { get; set; } = true;
    }
}
