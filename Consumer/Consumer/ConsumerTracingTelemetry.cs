using System.Diagnostics;
using System.Reflection;

namespace Consumer
{
    public class ConsumerTracingTelemetry
    {
        //only once in assembly.
        public const string ServiceName = "Consumer";
        public static readonly string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        // only one traces should be defined in this class 
        public static readonly ActivitySource Traces = new(ServiceName, Version);
    }
}
