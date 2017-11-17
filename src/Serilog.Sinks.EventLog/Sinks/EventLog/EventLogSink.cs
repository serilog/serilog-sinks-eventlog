// Copyright 2014 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Diagnostics;
using System.IO;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting;

namespace Serilog.Sinks.EventLog
{
    /// <summary>
    /// Writes log events as documents to the Windows event log.
    /// </summary>
    /// <remarks>Beware of changing the source/logname, see: http://stackoverflow.com/questions/804284/how-do-i-write-to-a-custom-windows-event-log?rq=1</remarks>
    public class EventLogSink : ILogEventSink
    {
        const string ApplicationLogName = "Application";
        const int MaximumPayloadLengthChars = 31839;
        const int MaximumSourceNameLengthChars = 212;
        const int SourceMovedEventId = 3;

        readonly IEventIdProvider _eventIdProvider;
        readonly ITextFormatter _textFormatter;
        readonly System.Diagnostics.EventLog _log;

        /// <summary>
        /// Construct a sink posting to the Windows event log, creating the specified <paramref name="source"/> if it does not exist.
        /// </summary>
        /// <param name="source">The source name by which the application is registered on the local computer. </param>
        /// <param name="logName">The name of the log the source's entries are written to. Possible values include Application, System, or a custom event log.</param>
        /// <param name="textFormatter">Supplies culture-specific formatting information, or null.</param>
        /// <param name="machineName">The name of the machine hosting the event log written to.</param>
        /// <param name="manageEventSource">If false does not check/create event source.  Defaults to true i.e. allow sink to manage event source creation</param>
        /// <param name="eventIdProvider">Supplies event ids for emitted log events.</param>
        public EventLogSink(string source, string logName, ITextFormatter textFormatter, string machineName, bool manageEventSource, IEventIdProvider eventIdProvider = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (textFormatter == null) throw new ArgumentNullException(nameof(textFormatter));

            // The source is limitted in length and allowed chars, see: https://msdn.microsoft.com/en-us/library/e29k5ebc%28v=vs.110%29.aspx
            if (source.Length > MaximumSourceNameLengthChars)
            {
                SelfLog.WriteLine("Trimming long event log source name to {0} characters", MaximumSourceNameLengthChars);
                source = source.Substring(0, MaximumSourceNameLengthChars);
            }

            source = source.Replace("<", "_");
            source = source.Replace(">", "_");

            _eventIdProvider = eventIdProvider ?? new EventIdHashProvider();
            _textFormatter = textFormatter;
            _log = new System.Diagnostics.EventLog(string.IsNullOrWhiteSpace(logName) ? ApplicationLogName : logName, machineName);

            if (manageEventSource)
            {
                ConfigureSource(_log, source);
            }
            else
            {
                _log.Source = source;
            }
        }

        static void ConfigureSource(System.Diagnostics.EventLog log, string source)
        {
            var sourceData = new EventSourceCreationData(source, log.Log) {MachineName = log.MachineName};
            string oldLogName = null;

            if (System.Diagnostics.EventLog.SourceExists(source, log.MachineName))
            {
                var existingLogWithSourceName = System.Diagnostics.EventLog.LogNameFromSourceName(source, log.MachineName);

                if (!string.IsNullOrWhiteSpace(existingLogWithSourceName) &&
                    !log.Log.Equals(existingLogWithSourceName, StringComparison.OrdinalIgnoreCase))
                {
                    // Remove the source from the previous log so we can associate it with the current log name
                    System.Diagnostics.EventLog.DeleteEventSource(source, log.MachineName);
                    oldLogName = existingLogWithSourceName;
                }
            }
            else
            {
                System.Diagnostics.EventLog.CreateEventSource(sourceData);
            }

            if (oldLogName != null)
            {
                var metaSource = $"serilog-{log.Log}";
                if (!System.Diagnostics.EventLog.SourceExists(metaSource, log.MachineName))
                    System.Diagnostics.EventLog.CreateEventSource(new EventSourceCreationData(metaSource, log.Log)
                    {
                        MachineName = log.MachineName
                    });

                log.Source = metaSource;
                log.WriteEntry(
                    $"Event source {source} was previously registered in log {oldLogName}. " +
                    $"The source has been registered with this log, {log.Log}, however a computer restart may be required " +
                    $"before event logs will appear in {log.Log} with source {source}. Until then, messages may be logged to {oldLogName}.",
                    EventLogEntryType.Warning,
                    SourceMovedEventId);
            }

            log.Source = source;
        }

        /// <summary>
        /// Emit the provided log event to the sink.
        /// </summary>
        /// <param name="logEvent">The log event to write.</param>
        /// <remarks>
        /// <see cref="LogEventLevel.Debug" />, <see cref="LogEventLevel.Information" /> and <see cref="LogEventLevel.Verbose" /> are registered as <see cref="EventLogEntryType.Information" />.
        /// <see cref="LogEventLevel.Error" />, <see cref="LogEventLevel.Fatal" /> are registered as <see cref="EventLogEntryType.Error" />.
        /// <see cref="LogEventLevel.Warning" /> are registered as <see cref="EventLogEntryType.Warning" />.
        /// The Event ID in the Windows log will be set to the integer value of the <paramref name="logEvent"/>'s <see cref="LogEvent.Level"/> property, so that the log can be filtered with more granularity.</remarks>
        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));

            var type = LevelToEventLogEntryType(logEvent.Level);

            var payloadWriter = new StringWriter();
            _textFormatter.Format(logEvent, payloadWriter);

            // The payload is limited in length and allowed chars, see: https://msdn.microsoft.com/en-us/library/e29k5ebc%28v=vs.110%29.aspx
            var payload = payloadWriter.ToString();
            if (payload.Length > MaximumPayloadLengthChars)
            {
                SelfLog.WriteLine("Trimming long event log entry payload to {0} characters", MaximumPayloadLengthChars);
                payload = payload.Substring(0, MaximumPayloadLengthChars);
            }

            _log.WriteEntry(payload, type, _eventIdProvider.ComputeEventId(logEvent));
        }

        static EventLogEntryType LevelToEventLogEntryType(LogEventLevel logEventLevel)
        {
            switch (logEventLevel)
            {
                case LogEventLevel.Debug:
                case LogEventLevel.Information:
                case LogEventLevel.Verbose:
                    return EventLogEntryType.Information;

                case LogEventLevel.Error:
                case LogEventLevel.Fatal:
                    return EventLogEntryType.Error;

                case LogEventLevel.Warning:
                    return EventLogEntryType.Warning;

                default:
                    SelfLog.WriteLine("Unexpected logging level {0}, writing to the event log as `Information`", logEventLevel);
                    return EventLogEntryType.Information;
            }
        }
    }
}
