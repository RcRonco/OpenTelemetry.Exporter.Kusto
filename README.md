# OpenTelemetry.Exporter.Kusto (WIP)
OpenTelemtry exporter for Kusto (Azure Data Explorer)

## Setup a table
```csl
.create table Logs (
    Timestamp: datetime, 
    Category: string, 
    EventId: int, 
    EventName: string, 
    Severity: string, 
    TraceId: string,
    SpanId: string,
    Message: string,
    Exception: string
) 
```
