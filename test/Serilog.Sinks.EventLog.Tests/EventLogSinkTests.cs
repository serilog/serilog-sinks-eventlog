using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Serilog.Formatting.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;
using System.IO;

#pragma warning disable Serilog004 // Allow non-constant message templates

namespace Serilog.Sinks.EventLog.Tests
{
    [TestFixture]
    public class EventLogSinkTests
    {
        const string CustomLogName = "serilog-eventlog-sink";
        const string EventLogSource = "EventLogSinkTests";

        [Test]
        public void EmittingJsonFormattedEventsWorks()
        {
            var log = new LoggerConfiguration()
                .WriteTo.EventLog(new JsonFormatter(), EventLogSource, manageEventSource: true)
                .CreateLogger();

            var message = $"This is a JSON message with a {Guid.NewGuid():D}";
            log.Information(message);
            var messageFromLogEvent = EventLogMessageWithSpecificBody(message);
            Assert.IsNotNull(messageFromLogEvent, "The message was not found in the eventlog.");
            AssertJsonCarriesMessageTemplate(messageFromLogEvent, message);
        }


        [Test]
        public void EmittingJsonFormattedEventsFromAppSettingsWorks()
        {
            var log = new LoggerConfiguration()
                .ReadFrom.Configuration(
                    new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddXmlFile("appsettings.xml")
                        .Build())
                .CreateLogger();

            var message = $"This is a JSON message with a {Guid.NewGuid():D}";
            log.Information(message);
            var messageFromLogEvent = EventLogMessageWithSpecificBody(message);
            Assert.IsNotNull(messageFromLogEvent, "The message was not found in the eventlog.");
            AssertJsonCarriesMessageTemplate(messageFromLogEvent, message);

        }

        void AssertJsonCarriesMessageTemplate(string json, string messageTemplate)
        {
            var jsonObject = JObject.Parse(json);
            Assert.AreEqual(messageTemplate, (string)jsonObject["MessageTemplate"]);
        }

        [Test]
        public void EmittingNormalEventsWorks()
        {
            var log = new LoggerConfiguration()
                .WriteTo.EventLog(EventLogSource, manageEventSource: true)
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
                .WriteTo.EventLog("EventLogSink<Tests>", manageEventSource: true)
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
                    .WriteTo.EventLog(EventLogSource + new string('x', i - EventLogSource.Length), manageEventSource: true)
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
                    .WriteTo.EventLog(EventLogSource, manageEventSource: true)
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
                .WriteTo.EventLog(EventLogSource, manageEventSource: true)
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
                    source: $"{EventLogSource}-{CustomLogName}",
                    logName: CustomLogName, 
                    manageEventSource: true)
                .CreateLogger();

            var guid = Guid.NewGuid().ToString("D");
            log.Information("This is a normal mesage with a {Guid} in log {CUSTOM_LOG_NAME}", guid, CustomLogName);

            Assert.IsTrue(EventLogMessageWithSpecificBodyExists(guid, CustomLogName),
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
                .WriteTo.EventLog(source: source, logName: CustomLogName, manageEventSource: true)
                .CreateLogger();

            var guid = Guid.NewGuid().ToString("D");
            log.Information("This is a normal mesage with a {Guid} in log {customLogName}", guid, CustomLogName);

            if (!EventLogMessageWithSpecificBodyExists(guid, "Application"))
                Assert.IsTrue(EventLogMessageWithSpecificBodyExists(guid, CustomLogName), "The message was not found in either the original or new eventlog.");


            Assert.IsTrue(EventLogMessageWithSpecificBodyExists(source, CustomLogName),
                "The message was not found in target eventlog.");

            System.Diagnostics.EventLog.DeleteEventSource(source);
        }

        bool EventLogMessageWithSpecificBodyExists(string partOfBody, string logName = "")
        {
            var log = string.IsNullOrWhiteSpace(logName) ? ApplicationLog : GetLog(logName);
            return log.Entries.Cast<EventLogEntry>().Any(entry => entry.Message.Contains(partOfBody));
        }

        string EventLogMessageWithSpecificBody(string partOfBody)
        {
            return ApplicationLog.Entries.Cast<EventLogEntry>().FirstOrDefault(entry => entry.Message.Contains(partOfBody))?.Message;
        }

        static System.Diagnostics.EventLog ApplicationLog => GetLog("Application");

        static System.Diagnostics.EventLog GetLog(string logName)
        {
            var evemtlog = System.Diagnostics.EventLog.GetEventLogs().FirstOrDefault(log => log.Log == logName);
            if (evemtlog == null)
                throw new Exception($"Cannot find log \"{logName}\"");
            return evemtlog;
        }
    }
}
