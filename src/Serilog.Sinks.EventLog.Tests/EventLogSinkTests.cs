using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;

namespace Serilog.Sinks.EventLog.Tests
{
    [TestFixture]
    public class EventLogSinkTests
    {
        [Test]
        public void EmittingNormalEventsWorks()
        {
            var log = new LoggerConfiguration()
                .WriteTo.EventLog("EventLogSinkTests")
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
                    .WriteTo.EventLog("EventLogSinkTests" + new string('x', i - "EventLogSinkTests".Length))
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
                    .WriteTo.EventLog("EventLogSinkTests")
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
                .WriteTo.EventLog("EventLogSinkTests")
                .CreateLogger();

            var guid = Guid.NewGuid().ToString("D");
            log.Information("This is a mesage with a {Guid} and a special char {char}", guid, "%1");

            Assert.IsTrue(EventLogMessageWithSpecificBodyExists(guid), "The message was not found in the eventlog.");
        }

        private bool EventLogMessageWithSpecificBodyExists(string partOfBody)
        {
            return ApplicationLog.Entries.Cast<EventLogEntry>().Any(entry => entry.Message.Contains(partOfBody));
        }

        private static System.Diagnostics.EventLog ApplicationLog
        {
            get { return System.Diagnostics.EventLog.GetEventLogs().First(log => log.Log == "Application"); }
        }
    }
}
