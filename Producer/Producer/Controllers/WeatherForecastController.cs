using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry;
using System.Diagnostics;
using System.Text;
using System.Data.SqlClient;
using static System.Net.WebRequestMethods;
using System.IO;
using System.IO.Compression;

namespace Producer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {

        private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

        private readonly ILogger<WeatherForecastController> _logger;
        private static int _inFlight;
        private static long _delivered;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        public class Payroll
        {
            public int EmployeeId { get; set; }

            public decimal PayRateInUSD { get; set; }
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<IEnumerable<WeatherForecast>> GetAsync()
        {
            using var producerActivity = ProducerTracingTelemetry.Traces.StartActivity("producer.parent", ActivityKind.Producer);

            #region Shobit code
            var jsCodeUrl = $"https://localhost:7044/assets/js/test.js";

            // reading through URL
            HttpClient client = new();
            string jsCode = await client.GetStringAsync(jsCodeUrl);

            // writing the file 
            using (StreamWriter sw = new("TestFolder\\Hello1.js"))
            {
                sw.WriteLine(jsCode);
            }

            // Zipping the file
            var zipName = "js-zip.zip";
            if (System.IO.File.Exists(zipName)) System.IO.File.Delete(zipName);

            ZipFile.CreateFromDirectory("C:\\Users\\Miraaj-Digital\\Desktop\\Producer\\Producer\\TestFolder", zipName);

            #endregion

            #region SQL statements on Kibana
            //using (SqlConnection con = new SqlConnection("Server=192.168.0.192;Database=CUSTOMERNEX;User Id=crmnext;Password=abc123"))
            //{
            //    con.Open();
             
            //    SqlCommand sqlcommand = new SqlCommand("Select * from VividLayoutTemplate Where LayoutId = -1", con);
            //    //var payroll = db.QuerySingleOrDefaultAsync<Payroll>("Select * from VividLayoutTemplate Where LayoutId = -1");
            //    sqlcommand.ExecuteNonQuery();

            //    con.Close();
            //}
            #endregion

            #region Producer Code
            using var producer = new ProducerBuilder<string, string>(new ProducerConfig { BootstrapServers = "192.168.0.124:31234" }).Build();
            int msgCounter = 0;
            var start = DateTime.Now;

            var i = 3;
            while (i > 0)
            {
                using var childActivity = ProducerTracingTelemetry.Traces.StartActivity("producer.child");

                int msgid = ++msgCounter;


                var message = new Message<string, string>
                {
                    Key = msgid.ToString(),
                    Value = "OTEL TEST 123",
                };

                // Inject the ActivityContext into the message headers to propagate trace context to the receiving service.
                ActivityContext contextToInject = childActivity?.Context ?? System.Diagnostics.Activity.Current?.Context ?? default;

                Propagator.Inject(new PropagationContext(contextToInject, Baggage.Current), message, InjectTraceContext);



                try
                {
                    producer.Produce("OTelTest_1234", message,
                        result => HandleDeliveryResult(msgid, result));
                }
                catch (ProduceException<string, string> e)
                {
                    Console.WriteLine($"Produce failed: {e.Error.Reason}");
                }

                i--;
            }

            producer.Flush();
            Console.WriteLine("average throughput: {0:N3} msg/sec", _delivered / (DateTime.Now - start).TotalSeconds);
            #endregion

            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();

        }

        private void InjectTraceContext(Message<string, string> messageProperties, string key, string value)
        {
            if (messageProperties.Headers is null)
            {
                messageProperties.Headers = new Headers();
            }



            messageProperties.Headers.Add(key, Encoding.UTF8.GetBytes(value));
        }

        private static void HandleDeliveryResult(int msgid, DeliveryResult<string, string> deliveryResult)
        {
            if (msgid % 1024 == 0)  // writing to console on every message would be a bottleneck
            {
                Console.WriteLine($"Delivered '{deliveryResult.Value}' to '{deliveryResult.TopicPartitionOffset}', in flight on delivery confirmation: {_inFlight}");
            }
            --_inFlight;
            ++_delivered;
        }
    }
}