using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTelemetry.Exporter.Kusto
{
    // Adding Rc for now to prevent naming collisions with official event sources in the future, (Rc == Ron Cohen :))
    [EventSource(Name = "OpenTelemetry-Exporter-Kusto-Rc")]
    internal class KustoOtelEventSource : EventSource
    {
        public static readonly KustoOtelEventSource Log = new KustoOtelEventSource();

        #region Events
        [Event(1, Level = EventLevel.Error)]
        public void LogExporterError(string errorMessage)
        {
            WriteEvent(1, errorMessage);
        }
        #endregion
    }
}
