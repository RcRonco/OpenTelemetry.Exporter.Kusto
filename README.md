# OpenTelemetry.Exporter.Kusto (WIP)

:warning: **THIS IS A PRESONAL PROJECT**


OpenTelemtry exporter for Kusto (Azure Data Explorer)

## Settings up Kusto Cluster
### Create table
```kql
.create table OTelLogs (
  TimeStamp  :datetime/long,
  Resource   :dynamic,
  Category   :string,
  EventName  :string,
  EventId    :int,
  Level      :string,
  TraceId    :string,
  SpanId     :string,
  TraceFlags :string,
  Attributes :dynamic,
  Payload    :string
) 
```

##### Make sure to choose one of the types in TimeStamp

### Create ingestion mapping
```kql
.create table OTelLogs ingestion csv mapping "OTelLogsMapping" ```
[
  { "column": "TimeStamp",  "DataType": "datetime",      "Properties":{"Ordinal":"0"}},
  { "column": "Resource",   "DataType": "dynamic",       "Properties":{"Ordinal":"1"}},
  { "column": "Category",   "DataType": "string",        "Properties":{"Ordinal":"2"}},
  { "column": "EventName",  "DataType": "string",        "Properties":{"Ordinal":"3"}},
  { "column": "EventId",    "DataType": "int",           "Properties":{"Ordinal":"4"}},
  { "column": "Level",      "DataType": "string",        "Properties":{"Ordinal":"5"}},
  { "column": "TraceId",    "DataType": "string",        "Properties":{"Ordinal":"6"}},
  { "column": "SpanId",     "DataType": "string",        "Properties":{"Ordinal":"7"}},
  { "column": "TraceFlags", "DataType": "string",        "Properties":{"Ordinal":"8"}},
  { "column": "Attributes", "DataType": "dynamic",       "Properties":{"Ordinal":"9"}},
  { "column": "Payload",    "DataType": "string",        "Properties":{"Ordinal":"10"}}
]```
```


## Getting started with .NET
```csharp
public void Sample()
{
    using var loggerFactory = LoggerFactory.Create(builder =>
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
		kustoOptions.ConnectionString = new KustoConnectionStringBuilder("https://ingest-{clustername}.{region}.kusto.windows.net/").WithAadUserPromptAuthentication();
		kustoOptions.DatabaseName = "OTel";
		kustoOptions.TableName = "OTelLogs";
		kustoOptions.MappingReference = "OTelLogsMapping";
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

```

#### Execute Query:
```kql
OTelLogs
| evaluate bag_unpack(Resource, 'Resource.')
```

![Query result](https://raw.githubusercontent.com/RcRonco/OpenTelemetry.Exporter.Kusto/master/Docs/images/LogsQuerySample.jpg)
