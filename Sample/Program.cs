using Kusto.Data;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter.Kusto;
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
                    });
                });
            });

            var logger = loggerFactory.CreateLogger<Program>();
            logger.Log(LogLevel.Error, new EventId(1, "SampleEvent"), new
            {
                FieldA = "Some text",
                FieldB = Guid.NewGuid(),
                FieldC = 123
            }, exception: null, (state, ex) => string.Empty);
            Console.ReadLine();
        }
    }
}
