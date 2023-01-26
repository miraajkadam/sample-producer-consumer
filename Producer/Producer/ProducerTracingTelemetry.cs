using System.Diagnostics;
using System.Reflection;

namespace Producer
{
    public class ProducerTracingTelemetry
    {
        //only once in assembly.
        public const string ServiceName = "Producer";
        public static readonly string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        // only one traces should be defined in this class 
        public static readonly ActivitySource Traces = new(ServiceName, Version);
    }
}
