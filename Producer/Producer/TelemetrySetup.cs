using Acidaes.OpenTelemetry;
using OpenTelemetry.Exporter;

namespace Producer
{
    public static class TelemetrySetup
    {
        public static void InitializeTracing(IServiceCollection builderServices)
        {
            try
            {
                string[] TracesNames = { "Producer" };

                //TracingOptions options = new("Producer_Test")
                //{
                //    SourceNames = TracesNames,
                //    Instrumentation = new DefaultIntrumentation
                //    {
                //        TraceAspNet = true,
                //        //TraceGrpcClient = true
                //    },
                //    IsEnabled = true
                //};

                TracingOptions traceOptions = new TracingOptions("Producer_Test");
                traceOptions.Instrumentation = new DefaultIntrumentation();
                traceOptions.Instrumentation.TraceAspNet = true;
                traceOptions.Instrumentation.TraceHttpClient = true;
                traceOptions.Instrumentation.TraceGrpcClient = true;
                traceOptions.Instrumentation.TraceSqlClient = true;
                traceOptions.Instrumentation.TraceSqlClientOptions = new OpenTelemetry.Instrumentation.SqlClient.SqlClientInstrumentationOptions()
                {
                    RecordException = true,
                    SetDbStatementForStoredProcedure = true,
                    SetDbStatementForText = true,
                    EnableConnectionLevelAttributes = true,
                };
                traceOptions.SourceNames = TracesNames;
                traceOptions.IsEnabled = true;

                OtelOptions otel = new("http://127.0.0.1", OtlpExportProtocol.Grpc);
                OpenTelemetryExtensions.AddOpenTelemetryTracing(builderServices, traceOptions, otel);
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