using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.EventLog("Sample App", manageEventSource: true)
    .CreateLogger();

Log.Information("Hello, Windows Event Log!");

Log.CloseAndFlush();
