using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;

namespace Serilog.Sinks.EventLog.Tests
{
    [TestFixture]
    public class EventLogSinkTests
    {
        private readonly string EVENT_LOG_SOURCE = "EventLogSinkTests";

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
            var customLogName = "serilog-eventlog-sink";
            var log = new LoggerConfiguration()
                .WriteTo.EventLog(
                    //can't use same source in different log
                    source: $"{EVENT_LOG_SOURCE}-{customLogName}", 
                    logName: customLogName)
                .CreateLogger();

            var guid = Guid.NewGuid().ToString("D");
            log.Information("This is a normal mesage with a {Guid} in log {customLogName}", guid, customLogName);

            Assert.IsTrue(EventLogMessageWithSpecificBodyExists(guid, customLogName),
                "The message was not found in the eventlog.");
        }

        [Test]
        public void UsingExistingSourceInCustomEventLogLogsRestartWarningAndLogsToApplicationLog()
        {
            var customLogName = "serilog-eventlog-sink";
            var source = Guid.NewGuid().ToString("D");
            //create our source in the app log first
            System.Diagnostics.EventLog.CreateEventSource(new EventSourceCreationData(source, "Application"));

            //then try to use it in our custom log
            var log = new LoggerConfiguration()
                .WriteTo.EventLog(source: source, logName: customLogName)
                .CreateLogger();

            var guid = Guid.NewGuid().ToString("D");
            log.Information("This is a normal mesage with a {Guid} in log {customLogName}", guid, customLogName);

            Assert.IsTrue(EventLogMessageWithSpecificBodyExists(guid, "Application"),
                "The message was not found in the eventlog.");
            Assert.IsTrue(EventLogMessageWithSpecificBodyExists(source, customLogName),
                "The message was not found in the eventlog.");

            System.Diagnostics.EventLog.DeleteEventSource(source);
        }

        private bool EventLogMessageWithSpecificBodyExists(string partOfBody, string logName = "")
        {
            var log = string.IsNullOrWhiteSpace(logName) ? ApplicationLog : GetLog(logName);
            return log.Entries.Cast<EventLogEntry>().Any(entry => entry.Message.Contains(partOfBody));
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
