using Kusto.Cloud.Platform.Utils;
using Kusto.Data.Common;
using Kusto.Ingest;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using System;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace OpenTelemetry.Exporter.Kusto
{
    /// <summary>
    /// Generates table with schema:
    /// Timestamp - DateTime
    /// Category - String
    /// EventId - Int
    /// EventName - String
    /// Level - String
    /// Message - String
    /// Additional Columns
    /// </summary>
    public class KustoLogExporter : BaseExporter<LogRecord>
    {
        #region Constants
        private const int c_bufferSize = 65360;
        private const string c_defaultDatabaseName = "Default";
        private const string c_defaultTableName = "Logs";
        private const IngestionReportLevel c_defaultReportLevel = IngestionReportLevel.FailuresOnly;
        private const IngestionReportMethod c_defaultReportMethod = IngestionReportMethod.Queue;
        #endregion

        #region Private Members
        private readonly KustoLogExporterOptions m_options;
        private readonly IKustoIngestClient m_ingestClient;
        private readonly KustoQueuedIngestionProperties m_ingestionOptions;
        

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

            m_options = options;
            if (string.IsNullOrEmpty(m_options.DatabaseName))
            {
                m_options.DatabaseName = c_defaultDatabaseName; // TODO: Proper name or should throw?
            }

            if (string.IsNullOrEmpty(m_options.TableName))
            {
                m_options.TableName = c_defaultTableName;
            }

            m_ingestClient = KustoIngestFactory.CreateQueuedIngestClient(m_options.ConnectionString);
            m_ingestionOptions = new KustoQueuedIngestionProperties(m_options.DatabaseName, m_options.TableName)
            {
                Format = DataSourceFormat.csv,
                ReportLevel = m_options.ReportLevel ?? c_defaultReportLevel,
                ReportMethod = m_options.ReportMethod ?? c_defaultReportMethod,
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
            if (force || m_bufferStream.Value.Position == c_bufferSize)
            {
                m_bufferStream.Value.Seek(0, SeekOrigin.Begin);
                m_ingestClient.IngestFromStreamAsync(m_bufferStream.Value, m_ingestionOptions, new StreamSourceOptions
                {
                    Size = m_bufferStream.Value.Position,
                    LeaveOpen = true
                }).ResultEx();

                m_bufferStream.Value.Seek(0, SeekOrigin.Begin);
            }
        }

        private bool WriteCsvRecord(CsvWriter csvWriter, LogRecord record)
        {            
            var result = csvWriter.WriteField(record.Timestamp.ToString("O"));
            result = result && csvWriter.WriteField(record.CategoryName);
            result = result && csvWriter.WriteField(record.EventId.Id.ToString());
            result = result && csvWriter.WriteField(record.EventId.Name);
            result = result && csvWriter.WriteField(FastToString(record.LogLevel));
            result = result && csvWriter.WriteField(record.FormattedMessage);
            result = result && csvWriter.WriteField(JsonSerializer.Serialize(record.State, s_serializerOptions));
            csvWriter.CompleteRecord();
            csvWriter.Flush();

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
            return new CsvWriter(new StreamWriter(m_bufferStream.Value, encoding: Encoding.UTF8, bufferSize: c_bufferSize, leaveOpen: true));
        }

        private string FastToString(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace: return "Trace";
                case LogLevel.Debug: return "Debug";
                case LogLevel.Information: return "Information";
                case LogLevel.Warning: return "Warning";
                case LogLevel.Error: return "Error";
                case LogLevel.Critical: return "Critical";
                case LogLevel.None:
                default: return "None";
            }
        }
        #endregion
    }
}
