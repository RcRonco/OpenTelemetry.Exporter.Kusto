using Kusto.Cloud.Platform.Utils;
using Kusto.Data;
using Kusto.Ingest;

namespace OpenTelemetry.Exporter.Kusto
{
    public class KustoLogExporterOptions
    {
        #region Constants
        public const string DefaultTableName = "Logs";
        public const IngestionReportLevel DefaultReportLevel = IngestionReportLevel.FailuresOnly;
        public const IngestionReportMethod DefaultReportMethod = IngestionReportMethod.Queue;
        #endregion

        #region Properties
        public KustoConnectionStringBuilder ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public string TableName { get; set; }
        public IngestionReportLevel? ReportLevel { get; set; }
        public IngestionReportMethod? ReportMethod { get; set; }
        public string MappingReference { get; set; }
        public bool EnableStreamIngestion { get; set; } = false;

        /// <summary>
        /// If set to true Exporter will send the DateTime values as Kusto's datetime values and not uint64
        /// As described in OpenTelemetry spec all Timestamp data should be sent as "uint64 nanoseconds since Unix epoch"
        /// This is not the recommended format to make readable telemetry on Kusto
        /// </summary>
        public bool UseKustoDateTime { get; set; } = true;
        #endregion

        #region Public Methods
        public void ValidateOrThrow()
        {
            Ensure.ArgIsNotNull(ConnectionString, nameof(ConnectionString));
            Ensure.ArgSatisfiesCondition(ConnectionString.IsValid(out var errorMessage), nameof(ConnectionString), errorMessage);
            Ensure.ArgIsNotNullOrEmpty(DatabaseName, nameof(DatabaseName));
            Ensure.ArgIsNotNullOrEmpty(MappingReference, nameof(MappingReference));

            if (string.IsNullOrEmpty(TableName))
            {
                TableName = DefaultTableName;
            }

            ReportLevel ??= DefaultReportLevel;
            ReportMethod ??= DefaultReportMethod;
        }
        #endregion
    }
}
