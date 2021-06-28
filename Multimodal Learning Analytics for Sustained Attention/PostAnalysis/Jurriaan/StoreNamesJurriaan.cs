using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multimodal_Learning_Analytics_for_Sustained_Attention.PostAnalysis.Jurriaan
{
    class StoreNamesJurriaan
    {
        public static readonly string FILTERED_TEMPERATURE_STORE_NAME = "TemperatureFilteredData";
        public static readonly string FILTERED_PARTICIPANT_TEMPERATURE_STREAM_NAME = ParticipantPipeline.PARTICIPANT_TEMPERATURE_STREAM_NAME + " (filtered)";
        public static readonly string DETECTED_FACES_STORE_NAME = "DetectedFacesData";
        public static readonly string FACE_RECTANGLES_STREAM_NAME = "Face Rectangles";

        public static readonly string ANGRY_STREAM_NAME = "angry";
        public static readonly string DISGUST_STREAM_NAME = "disgust";
        public static readonly string FEAR_STREAM_NAME = "fear";
        public static readonly string HAPPY_STREAM_NAME = "happy";
        public static readonly string NEUTRAL_STREAM_NAME = "neutral";
        public static readonly string SAD_STREAM_NAME = "sad";
        public static readonly string SURPRISE_STREAM_NAME = "surprise";

    }
}
