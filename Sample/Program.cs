using Kusto.Data;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter.Kusto;
using OpenTelemetry.Logs;
using System;

namespace Sample
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddOpenTelemetry(options =>
                {
                    options.AddKustoLogExporter(kustoOptions =>
                    {
                        kustoOptions.ConnectionString = new KustoConnectionStringBuilder("https://ingest-oteltest.eastus.kusto.windows.net/")
                             .WithAadUserPromptAuthentication();
                        kustoOptions.DatabaseName = "OTel";
                        kustoOptions.TableName = "OTelLogs";
                        kustoOptions.MappingReference = "OTelLogsMapping";
                    });
                });
            });

            var logger = loggerFactory.CreateLogger<Program>();
            logger.Log(LogLevel.Error, new EventId(1, "SampleEvent"), new
            {
                FieldA = "Some text",
                FieldB = Guid.NewGuid(),
                FieldC = 123
            }, exception: null, (state, ex) => "SomeText");
            logger.LogInformation("Some info message with param {0}", 123);
            logger.LogWarning("Some warning message with param {0}", 1.23);
            logger.LogCritical("Some critical message with param {0}", Guid.NewGuid());
            logger.LogCritical(new EventId(3, "CriticalSampleEvent"), new InvalidOperationException("my exception message"), "Some critical message with param {0}", 2);
            Console.ReadLine();
        }
    }
}
