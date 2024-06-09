using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.EventLog
{
    internal class NullSink : ILogEventSink
    {
        public void Emit(LogEvent logEvent)
        {
            // Do nothing
        }
    }
}
