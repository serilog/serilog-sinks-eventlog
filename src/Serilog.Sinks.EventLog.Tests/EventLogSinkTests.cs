using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Serilog.Formatting.Json;
using Newtonsoft.Json.Linq;

namespace Serilog.Sinks.EventLog.Tests
{
    [TestFixture]
    public class EventLogSinkTests
    {
        private const string CUSTOM_LOG_NAME = "serilog-eventlog-sink";
        private readonly string EVENT_LOG_SOURCE = "EventLogSinkTests";

        [Test]
        public void EmittingJsonFormattedEventsWorks()
        {
            var log = new LoggerConfiguration()
                .WriteTo.EventLog(new JsonFormatter(), EVENT_LOG_SOURCE)
                .CreateLogger();

            var message = $"This is a JSON message with a {Guid.NewGuid().ToString("D")}";
            log.Information(message);
            var messageFromLogEvent = EventLogMessageWithSpecificBody(message);
            Assert.IsNotNull(messageFromLogEvent, "The message was not found in the eventlog.");
            AssertJsonIsCorrect(messageFromLogEvent, message);
        }


        [Test]
        public void EmittingJsonFormattedEventsFromAppSettingsWorks()
        {
            var log = new LoggerConfiguration()
                .ReadFrom.AppSettings()
                .CreateLogger();

            var message = $"This is a JSON message with a {Guid.NewGuid().ToString("D")}";
            log.Information(message);
            var messageFromLogEvent = EventLogMessageWithSpecificBody(message);
            Assert.IsNotNull(messageFromLogEvent, "The message was not found in the eventlog.");
            AssertJsonIsCorrect(messageFromLogEvent, message);

        }

        private void AssertJsonIsCorrect(string json, string message)
        {
            var jsonObject = JObject.Parse(json);
            Assert.IsTrue((string)jsonObject["MessageTemplate"] == message);
        }

        [Test]
        public void EmittingNormalEventsWorks()
        {
            var log = new LoggerConfiguration()
                .WriteTo.EventLog(EVENT_LOG_SOURCE)
                .CreateLogger();

            var guid = Guid.NewGuid().ToString("D");
            log.Information("This is a normal mesage with a {Guid}", guid);
            Assert.IsTrue(EventLogMessageWithSpecificBodyExists(guid),
                "The message was not found in the eventlog.");
        }

        [Test]
        public void UsingAngleBracketsInSourceWorks()
        {
            var log = new LoggerConfiguration()
                .WriteTo.EventLog("EventLogSink<Tests>")
                .CreateLogger();

            var guid = Guid.NewGuid().ToString("D");
            log.Information("This is a normal mesage with a {Guid}", guid);

            Assert.IsTrue(EventLogMessageWithSpecificBodyExists(guid),
                "The message was not found in the eventlog.");
        }

        [Test]
        public void UsingSuperLongSourceNamesAreCorrectlyTrimmed()
        {
            for (var i = 199; i < 270; i+=10)
            {
                var log = new LoggerConfiguration()
                    .WriteTo.EventLog(EVENT_LOG_SOURCE + new string('x', i - EVENT_LOG_SOURCE.Length))
                    .CreateLogger();

                var guid = Guid.NewGuid().ToString("D");
                log.Information("This is a mesage with a {Guid}, source had length {length}", guid, i);

                Assert.IsTrue(EventLogMessageWithSpecificBodyExists(guid), "The message was not found in the eventlog. SourceLength was " + i);
            }
        }

        [Test]
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
                    .WriteTo.EventLog(EVENT_LOG_SOURCE)
                    .CreateLogger();

                var guid = Guid.NewGuid().ToString("D");
                log.Information("This is a super long message which might be trimmed. Guid is {Guid}.The following text has {charcount} chars: {LongText}"
                    , guid
                    , charcount
                    , new string('x', charcount));

                Assert.IsTrue(EventLogMessageWithSpecificBodyExists(guid), "The message was not found in the eventlog. Charcount was " + charcount);
            }
        }

        [Test]
        public void UsingSpecialCharsWorks()
        {
            var log = new LoggerConfiguration()
                .WriteTo.EventLog(EVENT_LOG_SOURCE)
                .CreateLogger();

            var guid = Guid.NewGuid().ToString("D");
            log.Information("This is a mesage with a {Guid} and a special char {char}", guid, "%1");

            Assert.IsTrue(EventLogMessageWithSpecificBodyExists(guid), "The message was not found in the eventlog.");
        }

        [Test]
        public void UsingCustomEventLogWorks()
        {
            var log = new LoggerConfiguration()
                .WriteTo.EventLog(
                    //can't use same source in different log
                    source: $"{EVENT_LOG_SOURCE}-{CUSTOM_LOG_NAME}",
                    logName: CUSTOM_LOG_NAME)
                .CreateLogger();

            var guid = Guid.NewGuid().ToString("D");
            log.Information("This is a normal mesage with a {Guid} in log {CUSTOM_LOG_NAME}", guid, CUSTOM_LOG_NAME);

            Assert.IsTrue(EventLogMessageWithSpecificBodyExists(guid, CUSTOM_LOG_NAME),
                "The message was not found in the eventlog.");
        }

        [Test]
        public void UsingExistingSourceInCustomEventLogLogsRestartWarningAndLogsToApplicationLog()
        {
            var source = Guid.NewGuid().ToString("D");
            //create our source in the app log first
            System.Diagnostics.EventLog.CreateEventSource(new EventSourceCreationData(source, "Application"));

            //then try to use it in our custom log
            var log = new LoggerConfiguration()
                .WriteTo.EventLog(source: source, logName: CUSTOM_LOG_NAME)
                .CreateLogger();

            var guid = Guid.NewGuid().ToString("D");
            log.Information("This is a normal mesage with a {Guid} in log {customLogName}", guid, CUSTOM_LOG_NAME);

            if (!EventLogMessageWithSpecificBodyExists(guid, "Application"))
                Assert.IsTrue(EventLogMessageWithSpecificBodyExists(guid, CUSTOM_LOG_NAME), "The message was not found in either the original or new eventlog.");


            Assert.IsTrue(EventLogMessageWithSpecificBodyExists(source, CUSTOM_LOG_NAME),
                "The message was not found in target eventlog.");

            System.Diagnostics.EventLog.DeleteEventSource(source);
        }

        private bool EventLogMessageWithSpecificBodyExists(string partOfBody, string logName = "")
        {
            var log = string.IsNullOrWhiteSpace(logName) ? ApplicationLog : GetLog(logName);
            return log.Entries.Cast<EventLogEntry>().Any(entry => entry.Message.Contains(partOfBody));
        }

        private string EventLogMessageWithSpecificBody(string partOfBody)
        {
            return ApplicationLog.Entries.Cast<EventLogEntry>().FirstOrDefault(entry => entry.Message.Contains(partOfBody))?.Message;
        }

        private static System.Diagnostics.EventLog ApplicationLog
        {
            get { return GetLog("Application"); }
        }

        private static System.Diagnostics.EventLog GetLog(string logName)
        {
            var evemtlog = System.Diagnostics.EventLog.GetEventLogs().FirstOrDefault(log => log.Log == logName);
            if (evemtlog == null)
                throw new Exception($"Cannot find log \"{logName}\"");
            return evemtlog;
        }
    }
}
