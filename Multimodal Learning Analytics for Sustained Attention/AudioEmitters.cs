using Microsoft.Psi;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Data;
using Microsoft.Psi.Interop.Format;
using Microsoft.Psi.Interop.Serialization;
using Microsoft.Psi.Interop.Transport;
using Newtonsoft.Json.Linq;
using System;

namespace Multimodal_Learning_Analytics_for_Sustained_Attention
{
    class AudioEmitter
    {
        private IProducer<double> Source { get; set; }
        public Emitter<double> Emitter { get; private set; }

        private bool isRunning = false;

        public AudioEmitter(Pipeline pipeline, string topic, string host, bool sync, IFormatDeserializer deserializer) {
            Source = new NetMQSource<double>(pipeline, topic, $"tcp://{host}", deserializer).Where((m, e) => m != -1.0).Select((m, e) => {
                if (sync)
                {
                    e.OriginatingTime = TimeSync.NtpToSysTime(e.OriginatingTime);
                }

                return m;
            });

            Emitter = Source.Out;
        }

        public void Write(string name, PsiExporter store) {
            Emitter.Write(name, store);
        }

        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            Emitter.Close(DateTime.UtcNow);
        }
    }

    class AudioEmitterArray
    {
        private IProducer<short[]> Source { get; set; }
        public Emitter<AudioBuffer> Emitter { get; private set; }

        public AudioEmitterArray(Pipeline pipeline, string topic, string host, bool sync, WaveFormat waveFormat, int blockSize, IFormatDeserializer deserializer) {
            Emitter = pipeline.CreateEmitter<AudioBuffer>(this, $"internal-{topic}");
            Source = new NetMQSource<string>(pipeline, topic, $"tcp://{host}", deserializer).Where((m, e) => m != "").Select(x => JArray.Parse(x).ToObject<short[]>());

            Source.Out.Do((x, e) => {
                byte[] pl = new byte[blockSize * 2];
                Buffer.BlockCopy(x, 0, pl, 0, blockSize * 2);
                DateTime ts = e.OriginatingTime;

                if (sync)
                {
                    ts = TimeSync.NtpToSysTime(ts);
                }

                Emitter.Post(new AudioBuffer(pl, waveFormat), ts);
            });
        }

        public void Write(string name, PsiExporter store) {
            Emitter.Write(name, store);
        }

        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            Source.Out.Close(DateTime.UtcNow);
            Emitter.Close(DateTime.UtcNow);
        }
    }
}
