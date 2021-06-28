using Microsoft.Psi;
using Microsoft.Psi.Components;
using Newtonsoft.Json;
using System;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;

namespace Multimodal_Learning_Analytics_for_Sustained_Attention.IRTemperature
{
    public class IRTemperatureReader : IProducer<TemperatureDataJson>, ISourceComponent, IDisposable
    {
        private readonly Pipeline pipeline;
        private SerialPort stream;

        private readonly StringBuilder sb; // String builder used to build data messages from arduino
        private bool hasReceivedValidData = false;

        private System.Timers.Timer timer;

        public Emitter<TemperatureDataJson> Out { get; private set; }

        public IRTemperatureReader(Pipeline pipeline)
        {
            this.pipeline = pipeline;
            this.sb = new StringBuilder();
            this.Out = pipeline.CreateEmitter<TemperatureDataJson>(this, nameof(this.Out));
        }

        public void Start(Action<DateTime> notifyCompletionTime)
        {
            notifyCompletionTime(DateTime.MaxValue);

            // Check if every 10 seconds if we have received at least 1 valid message in the last 10 seconds
            this.timer = new System.Timers.Timer(10 * 1000);
            this.timer.Elapsed += CheckHasReceivedData;
            this.timer.AutoReset = true;
            this.timer.Start();

            stream = new SerialPort(MLAConfig.IR_COM_PORT, MLAConfig.IR_BAUD_RATE);
            stream.Open(); //Open the Serial Stream.
            stream.DataReceived += new SerialDataReceivedEventHandler((object sender, SerialDataReceivedEventArgs e) =>
            {
                if (this.Out.HasSubscribers)
                {
                    try
                    {
                        string data = stream.ReadExisting();
                        foreach (char c in data)
                        {
                            if (c == '\n')
                            {
                                string message = sb.ToString();
                                sb.Clear();

                                try
                                {
                                    TemperatureDataJson jsonData = JsonConvert.DeserializeObject<TemperatureDataJson>(message);
                                    hasReceivedValidData = true;
                                    this.Out.Post(jsonData, this.pipeline.GetCurrentTime());
                                } catch (Exception err)
                                {
                                    Console.WriteLine("Warning: error in serializing IR json message!");
                                    Console.WriteLine(err);
                                }
                            }
                            else
                            {
                                sb.Append(c);
                            }
                        }
                    } catch (Exception)
                    {
                        Console.WriteLine("Warning: Timeout during IR temperature reading!");
                    }
                }
            });
        }

        private void CheckHasReceivedData(Object source, System.Timers.ElapsedEventArgs e)
        {
            if (this.pipeline == null)
            {
                return;
            }
            Console.WriteLine("Checking if IR temperature has received valid data in the last 10 seconds at {0:HH:mm:ss.fff}", e.SignalTime);
            if (!hasReceivedValidData)
            {
                throw new Exception("IR temperature reader has not received any data yet, please verify");
            }
            hasReceivedValidData = false;
        }

        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            notifyCompleted();
            this.timer.Stop();
            this.timer.Dispose();
            this.Dispose();
        }

        public void Dispose()
        {
            if (this.stream != null)
            {
                if (this.stream.IsOpen)
                {
                    this.stream.Close();
                }
                this.stream.Dispose();
                this.stream = null;
            }
        }
    }
}
