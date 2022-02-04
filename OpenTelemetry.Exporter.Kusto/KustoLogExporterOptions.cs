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
    }
}
