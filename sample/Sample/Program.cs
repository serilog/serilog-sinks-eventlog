using Serilog;

namespace Sample
{
    class Program
    {
        static void Main()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.EventLog("Sample App", manageEventSource: true)
                .CreateLogger();

            Log.Information("Hello, Windows Event Log!");

            Log.CloseAndFlush();
        }
    }
}
