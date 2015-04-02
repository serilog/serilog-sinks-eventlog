# Serilog.Sinks.EventLog

[![Build status](https://ci.appveyor.com/api/projects/status/j1iodeatf9ykrluf/branch/master?svg=true)](https://ci.appveyor.com/project/serilog/serilog-sinks-eventlog/branch/master)

A Serilog sink that writes events to the Windows Event Log.

**Package** - [Serilog.Sinks.EventLog](http://nuget.org/packages/serilog.sinks.eventlog)
| **Platforms** - .NET 4.5

```csharp
var log = new LoggerConfiguration()
    .WriteTo.EventLog("My App")
    .CreateLogger();
```
