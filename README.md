# Serilog.Sinks.EventLog

[![Build status](https://ci.appveyor.com/api/projects/status/j1iodeatf9ykrluf/branch/master?svg=true)](https://ci.appveyor.com/project/serilog/serilog-sinks-eventlog/branch/master) [![NuGet Version](http://img.shields.io/nuget/v/Serilog.Sinks.EventLog.svg?style=flat)](https://www.nuget.org/packages/Serilog.Sinks.EventLog/)

A Serilog sink that writes events to the Windows Event Log.

**Important:** version 3.0 of this sink changed the default value of `manageEventSource` from `true` to `false`. Applications that run with administrative priviliges, and that can therefore create event sources on-the-fly, can opt-in by providing `manageEventSource: true` as a configuration option.

### Getting started

First, install the package from NuGet:

```
Install-Package Serilog.Sinks.EventLog
```

The sink is configured by calling `WriteTo.EventLog()` on the `LoggerConfiguration`:

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.EventLog("Sample App", manageEventSource: true)
    .CreateLogger();

Log.Information("Hello, Windows Event Log!");

Log.CloseAndFlush();
```

Events will appear under the Application log with the specified source name:

![Screenshot](https://raw.githubusercontent.com/serilog/serilog-sinks-eventlog/dev/assets/Screenshot.png)



