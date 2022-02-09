using Kusto.Cloud.Platform.Utils;
using Kusto.Data.Common;
using Kusto.Data.Ingestion;
using Kusto.Ingest;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace OpenTelemetry.Exporter.Kusto
{
    /// <summary>
    /// Table schema should match OpenTelemetry Log spec:
    /// https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/logs/data-model.md#log-and-event-record-definition
    /// </summary>
    public class KustoLogExporter : BaseExporter<LogRecord>
    {
        #region Constants
        private const int c_bufferSize = 65360;
        private const string c_logTypeFullName = "Microsoft.Extensions.Logging.FormattedLogValues";
        private const string c_defaultDatabaseName = "Default";
        private const string c_defaultTableName = "Logs";
        private const IngestionReportLevel c_defaultReportLevel = IngestionReportLevel.FailuresOnly;
        private const IngestionReportMethod c_defaultReportMethod = IngestionReportMethod.Queue;
        #endregion

        #region Private Members
        private readonly string m_resource;
        private readonly KustoLogExporterOptions m_options;
        private readonly IKustoIngestClient m_ingestClient;
        private readonly KustoQueuedIngestionProperties m_ingestionOptions;

        private static readonly Dictionary<string, object> s_emptyAttributes = new Dictionary<string, object>(0);
        private static readonly ThreadLocal<byte[]> m_buffer = new ThreadLocal<byte[]>(() => null);
        private static readonly ThreadLocal<MemoryStream> m_bufferStream = new ThreadLocal<MemoryStream>(() => null);
        private static readonly JsonSerializerOptions s_serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            IncludeFields = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };
        #endregion

        #region Constructors
        public KustoLogExporter(KustoLogExporterOptions options)
        {
            Ensure.ArgIsNotNull(options, nameof(options));
            Ensure.ArgIsNotNull(options.ConnectionString, nameof(options.ConnectionString));
            Ensure.IsTrue(options.ConnectionString.IsValid(out var errorMessage), errorMessage);

            m_resource = ParentProvider.GetResource().ToKustoOTelString();

            // Prepare options
            m_options = options;
            if (string.IsNullOrEmpty(m_options.DatabaseName))
            {
                m_options.DatabaseName = c_defaultDatabaseName; // TODO: Proper name or should throw?
            }

            if (string.IsNullOrEmpty(m_options.TableName))
            {
                m_options.TableName = c_defaultTableName;
            }

            // Prepare ingestion client
            // Todo: provide an option for different client to support
            m_ingestClient = KustoIngestFactory.CreateQueuedIngestClient(m_options.ConnectionString);
            IngestionMapping mapping = null;
            if (string.IsNullOrEmpty(m_options.MappingReference))
            {
                mapping = new IngestionMapping
                {
                    IngestionMappingKind = IngestionMappingKind.Csv,
                    IngestionMappingReference = m_options.MappingReference
                };
            }

            m_ingestionOptions = new KustoQueuedIngestionProperties(m_options.DatabaseName, m_options.TableName)
            {
                Format = DataSourceFormat.csv,
                ReportLevel = m_options.ReportLevel ?? c_defaultReportLevel,
                ReportMethod = m_options.ReportMethod ?? c_defaultReportMethod,
                IngestionMapping = mapping
            };
        }
        #endregion

        #region Public Methods
        public override ExportResult Export(in Batch<LogRecord> batch)
        {
            long cursor = 0;
            var result = ExportResult.Success;
            var csvWriter = CreateCsvWriter();

            foreach (var logRecord in batch)
            {
                try
                {
                    if (!WriteCsvRecord(csvWriter, logRecord))
                    {
                        // Reposition the stream cursor to prewrite location
                        m_bufferStream.Value.Position = cursor;
                        result = ExportResult.Failure;
                    }

                    cursor = m_bufferStream.Value.Position;
                    FlushIfNeeded();
                }
                catch (Exception ex)
                {
                    // TODO: write event for exception
                    KustoOtelEventSource.Log.LogExporterError(ex.ToStringEx());
                    result = ExportResult.Failure;
                }
            }

            FlushIfNeeded(force: true);
            return result;
        }
        #endregion

        #region Private Methods
        private void FlushIfNeeded(bool force = false)
        {
            if ((force || m_bufferStream.Value.Position == c_bufferSize) && m_bufferStream.Value.Position > 0)
            {
                var size = m_bufferStream.Value.Position;
                var stream = new MemoryStream(m_buffer.Value, 0, (int)size  - 2, writable: false);
                m_ingestClient.IngestFromStreamAsync(stream, m_ingestionOptions, new StreamSourceOptions
                {
                    Size = size,
                    LeaveOpen = false
                }).ResultEx();

                m_bufferStream.Value.Seek(0, SeekOrigin.Begin);
            }
        }

        private bool WriteCsvRecord(CsvWriter csvWriter, LogRecord record)
        {
            var result = csvWriter.WriteField(record.Timestamp.ToKustoOTelString(m_options.UseKustoDateTime));             // TimeStamp  (datetime/uint64)
            result = result && csvWriter.WriteField(m_resource);                                                           // Resource   (dynamic - KeyValue)
            result = result && csvWriter.WriteField(record.CategoryName);                                                  // Category   (string)
            result = result && csvWriter.WriteField(record.EventId.Name);                                                  // EventName  (string)
            result = result && csvWriter.WriteField(record.EventId.Id.ToString());                                         // EventId    (int32)
            result = result && csvWriter.WriteField(record.LogLevel.ToSeverityString());                                   // Level      (string)
            result = result && csvWriter.WriteField(record.TraceId.ToString());                                            // TraceId    (Guid/string)
            result = result && csvWriter.WriteField(record.SpanId.ToString());                                             // SpanId     (Guid/string)
            result = result && csvWriter.WriteField(record.TraceFlags.ToString());                                         // TraceFlags (string)
            result = result && csvWriter.WriteField(JsonSerializer.Serialize(GetAttributes(record), s_serializerOptions)); // Attributes (dynamic - KeyValue)

            // Payload (string)
            if (record.State == null)
            {
                result = result && csvWriter.WriteField(string.Empty);
            }
            else if (string.Equals(record.State.GetType().FullName, c_logTypeFullName))
            {
                result = result && csvWriter.WriteField(record.State.ToString());
            }
            else
            {
                result = result && csvWriter.WriteField(JsonSerializer.Serialize(record.State, s_serializerOptions));
            }

            if (result)
            {
                csvWriter.CompleteRecord();
                csvWriter.Flush();
            }

            return result;
        }

        private CsvWriter CreateCsvWriter()
        {
            if (m_buffer.Value == null)
            {
                m_buffer.Value = new byte[c_bufferSize];
                m_bufferStream.Value = new MemoryStream(m_buffer.Value);
            }

            m_bufferStream.Value.Seek(0, SeekOrigin.Begin);
            return new CsvWriter(new StreamWriter(m_bufferStream.Value, bufferSize: c_bufferSize, leaveOpen: true));
        }

        private IReadOnlyDictionary<string, object> GetAttributes(LogRecord record)
        {
            Dictionary<string, object> attributes;
            if (record.Exception == null)
            {
                attributes = s_emptyAttributes;
            }
            else
            {
                // Todo: Reduce dictionary allocations
                attributes = new Dictionary<string, object>(3)
                {
                    ["exception.type"] = record.Exception.GetType().FullName,
                    ["exception.message"] = record.Exception.Message,
                    ["exception.stacktrace"] = record.Exception.ToString()
                };
            }

            return attributes;
        }
        #endregion
    }
}
