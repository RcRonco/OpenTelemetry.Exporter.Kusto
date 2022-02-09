using Kusto.Data;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter.Kusto;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using System;
using System.Collections.Generic;

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
                    var resourceBuilder = ResourceBuilder.CreateDefault()
                       .AddService("ServiceName", "ServiceNamespace", "1.0.0.1-rc")
                       .AddAttributes(new Dictionary<string, object>(1)
                       {
                           ["additionalLabel"] = "labelValue"
                       });

                    options.SetResourceBuilder(resourceBuilder)
                           .AddKustoLogExporter(kustoOptions =>
                            {
                                kustoOptions.ConnectionString = new KustoConnectionStringBuilder("https://ingest-relogseus.eastus.kusto.windows.net")
                                     .WithAadApplicationKeyAuthentication("d443fbc0-3f4f-4f9f-ad3b-5766e13b15d1", "u6G7Q~SN6eB2RSUcDPDUDVAqVtQpYQyFmwqwr", "4523672d-a7c0-45ec-a446-57f746ff3684");
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
            
            Console.WriteLine("Done writing logs, waiting for exporter");
            Console.ReadLine();
        }
    }
}
