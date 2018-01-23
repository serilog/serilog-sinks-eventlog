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
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Sinks.EventLog;
using Serilog.Formatting;
using System.Runtime.InteropServices;

namespace Serilog
{
    /// <summary>
    /// Adds the WriteTo.EventLog() extension method to <see cref="LoggerConfiguration"/>.
    /// </summary>
    public static class LoggerConfigurationEventLogExtensions
    {
        const string DefaultOutputTemplate = "{Message}{NewLine}{Exception}";

        /// <summary>
        /// Adds a sink that writes log events to the Windows event log.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="source">The source name by which the application is registered on the local computer. </param>
        /// <param name="logName">The name of the log the source's entries are written to. Possible values include Application, System, or a custom event log. </param>
        /// <param name="machineName">The name of the machine hosting the event log written to.  The local machine by default.</param>
        /// <param name="manageEventSource">If true, check/create event source as required.  Defaults to false i.e. do not allow sink to manage event source creation.</param>
        /// <param name="outputTemplate">A message template describing the format used to write to the sink.  The default is "{Timestamp} [{Level}] {Message}{NewLine}{Exception}".</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="eventIdProvider">Supplies event ids for emitted log events.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration EventLog(
            this LoggerSinkConfiguration loggerConfiguration,
            string source,
            string logName = null,
            string machineName = ".",
            bool manageEventSource = false,
            string outputTemplate = DefaultOutputTemplate,
            IFormatProvider formatProvider = null,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            IEventIdProvider eventIdProvider = null)
        {
            if (loggerConfiguration == null)
            {
                throw new ArgumentNullException(nameof(loggerConfiguration));
            }

            // Verify the code is running on Windows.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new PlatformNotSupportedException(RuntimeInformation.OSDescription);
            }

            var formatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);

            if (eventIdProvider == null)
            {
                return loggerConfiguration.Sink(new EventLogSink(source, logName, formatter, machineName, manageEventSource), restrictedToMinimumLevel);
            }

            return loggerConfiguration.Sink(new EventLogSink(source, logName, formatter, machineName, manageEventSource, eventIdProvider), restrictedToMinimumLevel);
        }

        /// <summary>
        /// Adds a sink that writes log events to the Windows event log.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="source">The source name by which the application is registered on the local computer.</param>
        /// <param name="logName">The name of the log the source's entries are written to. Possible values include Application, System, or a custom event log.</param>
        /// <param name="machineName">The name of the machine hosting the event log written to.  The local machine by default.</param>
        /// <param name="manageEventSource">If false does not check/create event source.  Defaults to true i.e. allow sink to manage event source creation</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <param name="formatter">Formatter to control how events are rendered into the file. To control
        /// plain text formatting, use the overload that accepts an output template instead.</param>
        /// <param name="eventIdProvider">Supplies event ids for emitted log events.</param>
        /// <returns>
        /// Logger configuration, allowing configuration to continue.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">loggerConfiguration</exception>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration EventLog(
            this LoggerSinkConfiguration loggerConfiguration,
            ITextFormatter formatter,
            string source,
            string logName = null,
            string machineName = ".",
            bool manageEventSource = false,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            IEventIdProvider eventIdProvider = null)
        {
            if (loggerConfiguration == null) throw new ArgumentNullException(nameof(loggerConfiguration));
            if (formatter == null) throw new ArgumentNullException(nameof(formatter));

            if (eventIdProvider == null)
            {
                return loggerConfiguration.Sink(new EventLogSink(source, logName, formatter, machineName, manageEventSource), restrictedToMinimumLevel);
            }

            return loggerConfiguration.Sink(new EventLogSink(source, logName, formatter, machineName, manageEventSource, eventIdProvider), restrictedToMinimumLevel);
        }
    }
}
