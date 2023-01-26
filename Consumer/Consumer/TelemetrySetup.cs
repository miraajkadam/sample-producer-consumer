using Acidaes.OpenTelemetry;
using OpenTelemetry.Exporter;

namespace Consumer
{
    public static class TelemetrySetup
    {
        public static void InitializeTracing(IServiceCollection builderServices)
        {
            try
            {
                string[] TracesNames = { "Consumer" };

                TracingOptions options = new("Consumer_Test")
                {
                    SourceNames = TracesNames,
                    Instrumentation = new DefaultIntrumentation
                    {
                        TraceAspNet = true,
                        //TraceGrpcClient = true
                    },
                    IsEnabled = true
                };

                OtelOptions otel = new("http://192.168.0.147:32348", OtlpExportProtocol.Grpc);
                OpenTelemetryExtensions.AddOpenTelemetryTracing(builderServices, options, otel);
            }
            catch (Exception ex) when (
                ex is NullReferenceException ||
                ex is TypeInitializationException
            )
            {
                Console.WriteLine("Error in tracing initialization... Check the OTel configurations. " + ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
    }
}