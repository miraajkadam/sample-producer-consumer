using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using System.Diagnostics;
using System.Text;
using static Confluent.Kafka.ConfigPropertyNames;

namespace Consumer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            using Activity consumerActivity = ConsumerTracingTelemetry.Traces.StartActivity("consumer.parent", ActivityKind.Internal);
            
            #region Consumer Code
            var config = new ConsumerConfig
            {
                BootstrapServers = "192.168.0.124:31234",
                GroupId = "simple-dotnet-consumer",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                //EnablePartitionEof = true
            };
            using var consumer = new ConsumerBuilder<string, string>(config)
                .SetErrorHandler((_, e) => Console.WriteLine($"Error: {e.Reason}"))
                .Build();

            consumer.Subscribe(new List<string>() { "OTelTest_1234" });
            var start = DateTime.Now;
            long messageCounter = 0;

            var cancelTokenSource = new CancellationTokenSource();
            var token = cancelTokenSource.Token;


            try
            {
                //Task.Run(() => ConsumeMessages(consumer, messageCounter, token), token);
                ConsumeMessages(consumer, messageCounter);
            }
            catch (OperationCanceledException) { }

            var elapsed = DateTime.Now - start;
            Console.WriteLine("average throughput: {0:N3} msg/sec, {1} messages over {2:N3} sec", messageCounter / elapsed.TotalSeconds, messageCounter, elapsed.TotalSeconds);
            #endregion

            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        void ConsumeMessages(IConsumer<string, string> consumer, long messageCounter)
        {
            var result = new ConsumeResult<string, string>();

            while (!(result == null))
            {
                result = consumer.Consume(5000);

                if (result == null) { continue; }
                if (result.IsPartitionEOF) { break; }

                var parentContext = Propagator.Extract(default, result.Message, ExtractTraceContext);
                Baggage.Current = parentContext.Baggage;

                //var activityLink = new List<ActivityLink>
                //{
                //    new ActivityLink(parentContext.ActivityContext),
                //    new ActivityLink(consumerActivity.Context),
                //};

                using var childActivity = ConsumerTracingTelemetry.Traces.StartActivity("consumer.child", ActivityKind.Internal, parentContext.ActivityContext);
                using var childActivity1 = ConsumerTracingTelemetry.Traces.StartActivity("consumer.child123", ActivityKind.Internal, Activity.Current.Context);

                ++messageCounter;
                if (messageCounter % 1024 == 0) { Console.WriteLine($"Received message key: \"{result.Message.Key}\" value: {result.Message.Value}"); }

            }
        }

        IEnumerable<string> ExtractTraceContext(Message<string, string> properties, string key)
        {
            try
            {
                if (properties.Headers.TryGetLastBytes(key, out var value) && value is byte[] bytes)
                {
                    return new[] { Encoding.UTF8.GetString(bytes) };
                }
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Failed to extract trace context");
            }



            return Enumerable.Empty<string>();
        }
    }
}