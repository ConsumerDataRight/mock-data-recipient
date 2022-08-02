using Newtonsoft.Json;

namespace CDR.DCR
{
    public class DcrQueueMessage
    {
        [JsonProperty("messageVersion")]
        public string MessageVersion { get; set; }

        [JsonProperty("dataHolderBrandId")]
        public string DataHolderBrandId { get; set; }
    }
}