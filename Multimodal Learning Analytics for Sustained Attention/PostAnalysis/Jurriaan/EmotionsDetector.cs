using Microsoft.Psi;
using Microsoft.Psi.Imaging;
using System;
using System.Collections.Generic;
using Rectangle = System.Drawing.Rectangle;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Psi.Interop.Transport;
using Microsoft.Psi.Interop.Format;
using Microsoft.Psi.Components;
using System.Threading;

namespace Multimodal_Learning_Analytics_for_Sustained_Attention.PostAnalysis.Jurriaan
{
    public class EmotionsDetector : Subpipeline, IConsumer<Shared<Image>>, IProducer<List<DetectedFace>>
    {
        private NetMQWriter<byte[]> frameWriter;
        private NetMQSource<dynamic> faceDetectionSource;
        private Connector<Shared<Image>> inFrames;

        public EmotionsDetector(Pipeline pipeline) : base(pipeline, nameof(EmotionsDetector))
        {
            this.inFrames = this.CreateInputConnectorFrom<Shared<Image>>(pipeline, nameof(this.inFrames));

            this.frameWriter = new NetMQWriter<byte[]>(
                this,
                "webcamFrames",
                "tcp://127.0.0.1:12345",
                MessagePackFormat.Instance
            );

            DateTime lastSentOriginatingTime = DateTime.MaxValue;
            DateTime lastReceivedOriginatingTime = DateTime.MinValue;

            this.inFrames
                .Do((_, e) => {
                    lastSentOriginatingTime = e.OriginatingTime;
                })
                .EncodeJpeg(90)
                .Select(frame => frame.Resource.GetBuffer())
                .PipeTo(frameWriter);

            IEnumerable<bool> WaitForPending()
            {
                while (lastReceivedOriginatingTime < lastSentOriginatingTime)
                {
                    yield return true;
                }
            }

            this.faceDetectionSource = new NetMQSource<dynamic>(
                this,
                "faces",
                "tcp://127.0.0.1:12346",
                MessagePackFormat.Instance
            );

            this.Out = faceDetectionSource
                .Select((faces, e) =>
                {
                    lastReceivedOriginatingTime = e.OriginatingTime;
                    return ((IEnumerable<dynamic>)faces)
                        .Select(faceMsg =>
                        {
                            DetectedFace detectedFace = new DetectedFace
                            {
                                Box = new Rectangle(faceMsg["box"][0], faceMsg["box"][1], faceMsg["box"][2], faceMsg["box"][3]),
                                Emotions = new Emotions
                                {
                                    Angry = faceMsg["emotions"]["angry"],
                                    Disgust = faceMsg["emotions"]["disgust"],
                                    Fear = faceMsg["emotions"]["fear"],
                                    Happy = faceMsg["emotions"]["happy"],
                                    Neutral = faceMsg["emotions"]["neutral"],
                                    Sad = faceMsg["emotions"]["sad"],
                                    Surprise = faceMsg["emotions"]["surprise"]
                                }
                            };
                            //Console.WriteLine("EmotionsDetector: " + detectedFace);
                            return detectedFace;
                        }).ToList();
                }).Out;

            Generators.Sequence(pipeline, WaitForPending(), TimeSpan.FromSeconds(1));
        }

        public Receiver<Shared<Image>> In => this.inFrames.In;

        public Emitter<List<DetectedFace>> Out { get; private set; }
    }
}
