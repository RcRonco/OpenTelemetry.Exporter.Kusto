using Kusto.Data.Common;
using Kusto.Data.Ingestion;
using Kusto.Ingest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTelemetry.Exporter.Kusto
{
    internal class KustoHelper
    {
        #region Consts
        private const IngestionReportLevel c_defaultReportLevel = IngestionReportLevel.FailuresOnly;
        private const IngestionReportMethod c_defaultReportMethod = IngestionReportMethod.Queue;
        #endregion

        public static (IKustoIngestClient Client, KustoIngestionProperties IngestionProperties) CreateIngestClient(KustoLogExporterOptions options)
        {
            IKustoIngestClient client;
            KustoIngestionProperties properties;
            IngestionMapping mapping = null;
            
            if (string.IsNullOrEmpty(options.MappingReference))
            {
                mapping = new IngestionMapping
                {
                    IngestionMappingKind = IngestionMappingKind.Csv,
                    IngestionMappingReference = options.MappingReference
                };
            }

            if (options.EnableStreamIngestion)
            {
                client = KustoIngestFactory.CreateStreamingIngestClient(options.ConnectionString);
                properties = new KustoIngestionProperties(options.DatabaseName, options.TableName)
                {
                    Format = DataSourceFormat.csv,
                    IngestionMapping = mapping
                };
            }
            else
            {
                client = KustoIngestFactory.CreateQueuedIngestClient(options.ConnectionString);
                properties = new KustoQueuedIngestionProperties(options.DatabaseName, options.TableName)
                {
                    Format = DataSourceFormat.csv,
                    ReportLevel = options.ReportLevel ?? c_defaultReportLevel,
                    ReportMethod = options.ReportMethod ?? c_defaultReportMethod,
                    IngestionMapping = mapping
                };
            }

            return (client, properties);
        }
    }
}
