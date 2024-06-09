using System;
using System.Diagnostics;
using System.Linq;
using Serilog.Formatting.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;
using Serilog.Events;
using Xunit;

#pragma warning disable Serilog004 // Allow non-constant message templates

namespace Serilog.Sinks.EventLog.Tests
{
    public class EventLogSinkTests
    {
        const string CustomLogName = "serilog-eventlog-sink";
        const string EventLogSource = "EventLogSinkTests";

        [Fact]
        public void EmittingJsonFormattedEventsWorks()
        {
            var log = new LoggerConfiguration()
                .WriteTo.EventLog(new JsonFormatter(), EventLogSource, manageEventSource: true)
                .CreateLogger();

            var message = $"This is a JSON message with a {Guid.NewGuid():D}";
            log.Information(message);
            var messageFromLogEvent = EventLogMessageWithSpecificBody(message);
            Assert.NotNull(messageFromLogEvent);
            AssertJsonCarriesMessageTemplate(messageFromLogEvent, message);
        }


        [Fact]
        public void EmittingJsonFormattedEventsFromAppSettingsWorks()
        {
            var log = new LoggerConfiguration()
                .ReadFrom.Configuration(
                    new ConfigurationBuilder()
                        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                        .AddXmlFile("appsettings.xml")
                        .Build())
                .CreateLogger();

            var message = $"This is a JSON message with a {Guid.NewGuid():D}";
            log.Information(message);
            var messageFromLogEvent = EventLogMessageWithSpecificBody(message);
            Assert.NotNull(messageFromLogEvent);
            AssertJsonCarriesMessageTemplate(messageFromLogEvent, message);

        }

        static void AssertJsonCarriesMessageTemplate(string json, string messageTemplate)
        {
            var jsonObject = JObject.Parse(json);
            Assert.Equal(messageTemplate, (string)jsonObject["MessageTemplate"]);
        }

        [Fact]
        public void EmittingNormalEventsWorks()
        {
            var log = new LoggerConfiguration()
                .WriteTo.EventLog(EventLogSource, manageEventSource: true)
                .CreateLogger();

            var guid = Guid.NewGuid().ToString("D");
            log.Information("This is a normal message with a {Guid}", guid);
            Assert.True(EventLogMessageWithSpecificBodyExists(guid),
                "The message was not found in the event log.");
        }

        [Fact]
        public void UsingAngleBracketsInSourceWorks()
        {
            var log = new LoggerConfiguration()
                .WriteTo.EventLog("EventLogSink<Tests>", manageEventSource: true)
                .CreateLogger();

            var guid = Guid.NewGuid().ToString("D");
            log.Information("This is a normal message with a {Guid}", guid);

            Assert.True(EventLogMessageWithSpecificBodyExists(guid),
                "The message was not found in the event log.");
        }

        [Fact]
        public void UsingSuperLongSourceNamesAreCorrectlyTrimmed()
        {
            for (var i = 199; i < 270; i+=10)
            {
                var log = new LoggerConfiguration()
                    .WriteTo.EventLog(EventLogSource + new string('x', i - EventLogSource.Length), manageEventSource: true)
                    .CreateLogger();

                var guid = Guid.NewGuid().ToString("D");
                log.Information("This is a message with a {Guid}, source had length {length}", guid, i);

                Assert.True(EventLogMessageWithSpecificBodyExists(guid), "The message was not found in the event log. SourceLength was " + i);
            }
        }

        [Fact]
        public void UsingSuperLongLogMessageWorks()
        {
            var charcounts = new[]
            {
                10*1000,
                20*1000,
                30*1000,
                40*1000,
                70*1000
            };
            foreach (var charcount in charcounts)
            {
                var log = new LoggerConfiguration()
                    .WriteTo.EventLog(EventLogSource, manageEventSource: true)
                    .CreateLogger();

                var guid = Guid.NewGuid().ToString("D");
                log.Information("This is a super long message which might be trimmed. Guid is {Guid}.The following text has {charcount} chars: {LongText}"
                    , guid
                    , charcount
                    , new string('x', charcount));

                Assert.True(EventLogMessageWithSpecificBodyExists(guid), "The message was not found in the event log. Char count was " + charcount);
            }
        }

        [Fact]
        public void UsingSpecialCharsWorks()
        {
            var log = new LoggerConfiguration()
                .WriteTo.EventLog(EventLogSource, manageEventSource: true)
                .CreateLogger();

            var guid = Guid.NewGuid().ToString("D");
            log.Information("This is a message with a {Guid} and a special char {char}", guid, "%1");

            Assert.True(EventLogMessageWithSpecificBodyExists(guid), "The message was not found in the event log.");
        }

        [Fact]
        public void UsingCustomEventLogWorks()
        {
            var log = new LoggerConfiguration()
                .WriteTo.EventLog(
                    //can't use same source in different log
                    source: $"{EventLogSource}-{CustomLogName}",
                    logName: CustomLogName,
                    manageEventSource: true)
                .CreateLogger();

            var guid = Guid.NewGuid().ToString("D");
            log.Information("This is a normal message with a {Guid} in log {CUSTOM_LOG_NAME}", guid, CustomLogName);

            Assert.True(EventLogMessageWithSpecificBodyExists(guid, CustomLogName),
                "The message was not found in the event log.");
        }

        [Fact]
        public void UsingExistingSourceInCustomEventLogLogsRestartWarningAndLogsToApplicationLog()
        {
            var source = Guid.NewGuid().ToString("D");
            //create our source in the app log first
            System.Diagnostics.EventLog.CreateEventSource(new EventSourceCreationData(source, "Application"));

            //then try to use it in our custom log
            var log = new LoggerConfiguration()
                .WriteTo.EventLog(source: source, logName: CustomLogName, manageEventSource: true)
                .CreateLogger();

            var guid = Guid.NewGuid().ToString("D");
            log.Information("This is a normal message with a {Guid} in log {customLogName}", guid, CustomLogName);

            if (!EventLogMessageWithSpecificBodyExists(guid, "Application"))
                Assert.True(EventLogMessageWithSpecificBodyExists(guid, CustomLogName), "The message was not found in either the original or new event log.");


            Assert.True(EventLogMessageWithSpecificBodyExists(source, CustomLogName),
                "The message was not found in target event log.");

            System.Diagnostics.EventLog.DeleteEventSource(source);
        }

        [Fact]
        public void UsingCustomEventIdProviderLogsMessagesWithSuppliedEventId()
        {
            var log = new LoggerConfiguration()
                .WriteTo.EventLog(EventLogSource, manageEventSource: true, eventIdProvider: new CustomEventIdProvider())
                .CreateLogger();

            Assert.NotEqual(CustomEventIdProvider.MessageWithKnownIdEventId, CustomEventIdProvider.UnknownEventId);

            var knownIdGuid = Guid.NewGuid().ToString("D");
            log.Information(CustomEventIdProvider.MessageWithKnownId, knownIdGuid);

            Assert.True(EventLogMessageWithSpecificBodyAndEventIdExists(knownIdGuid, CustomEventIdProvider.MessageWithKnownIdEventId),
                "The message was with known event id not found in target event log.");

            var unknownIdGuid = Guid.NewGuid().ToString("D");
            log.Information("unknown message {Guid}", unknownIdGuid);

            Assert.True(EventLogMessageWithSpecificBodyAndEventIdExists(unknownIdGuid, CustomEventIdProvider.UnknownEventId),
                "The message was with unknown event id not found in target event log.");
        }

        static bool EventLogMessageWithSpecificBodyAndEventIdExists(string partOfBody, int eventId)
        {
            return ApplicationLog
                .Entries
                .Cast<EventLogEntry>()
                .Any(entry => entry.InstanceId == eventId
                              && entry.Message.Contains(partOfBody));
        }

        static bool EventLogMessageWithSpecificBodyExists(string partOfBody, string logName = "")
        {
            var log = string.IsNullOrWhiteSpace(logName) ? ApplicationLog : GetLog(logName);
            return log.Entries.Cast<EventLogEntry>().Any(entry => entry.Message.Contains(partOfBody));
        }

        static string EventLogMessageWithSpecificBody(string partOfBody)
        {
            return ApplicationLog.Entries.Cast<EventLogEntry>().FirstOrDefault(entry => entry.Message.Contains(partOfBody))?.Message;
        }

        static System.Diagnostics.EventLog ApplicationLog => GetLog("Application");

        static System.Diagnostics.EventLog GetLog(string logName)
        {
            var eventLog = System.Diagnostics.EventLog.GetEventLogs().FirstOrDefault(log => log.Log == logName);
            if (eventLog == null)
                throw new Exception($"Cannot find log \"{logName}\"");
            return eventLog;
        }

        sealed class CustomEventIdProvider : IEventIdProvider
        {
            public const ushort UnknownEventId = 1;

            public const ushort MessageWithKnownIdEventId = 12;
            public const string MessageWithKnownId = "Event {Guid} - this message has a known id";

            public ushort ComputeEventId(LogEvent logEvent)
            {
                return string.Equals(logEvent.MessageTemplate.Text, MessageWithKnownId) ? MessageWithKnownIdEventId : UnknownEventId;
            }
        }
    }
}
