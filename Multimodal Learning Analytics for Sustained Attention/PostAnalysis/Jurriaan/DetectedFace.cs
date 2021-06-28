using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multimodal_Learning_Analytics_for_Sustained_Attention.PostAnalysis.Jurriaan
{
    public class DetectedFace
    {
        [JsonProperty("box")]
        public Rectangle Box { get; set; }

        [JsonProperty("emotions")]
        public Emotions Emotions { get; set; }

        public override string ToString()
        {
            return $"Face at {Box.X},{Box.Y},{Box.Width},{Box.Height} with emotions {Emotions}";
        }
    }

    public class Emotions
    {
        [JsonProperty("angry")]
        public double Angry { get; set; }

        [JsonProperty("disgust")]
        public double Disgust { get; set; }

        [JsonProperty("fear")]
        public double Fear { get; set; }

            [JsonProperty("happy")]
        public double Happy { get; set; }

            [JsonProperty("neutral")]
        public double Neutral { get; set; }

            [JsonProperty("sad")]
        public double Sad { get; set; }

            [JsonProperty("surprise")]
        public double Surprise { get; set; }

        public override string ToString()
        {
            return $"{{Angry: {Angry}, Disgust: {Disgust}, Fear: {Fear}, Happy: {Happy}, Neutral: {Neutral}, Sad: {Sad}, Surprise: {Surprise}}}";
        }
    }
}
