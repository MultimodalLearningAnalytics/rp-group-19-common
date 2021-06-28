using Newtonsoft.Json;

namespace Multimodal_Learning_Analytics_for_Sustained_Attention.IRTemperature
{
    public class TemperatureDataJson
    {
        [JsonProperty("ambientTemp")]
        public double AmbientTemperature { get; set; }
        [JsonProperty("objectTemp")]
        public double ObjectTemperature { get; set; }
    }
}
