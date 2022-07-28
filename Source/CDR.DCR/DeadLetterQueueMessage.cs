using Newtonsoft.Json;

namespace CDR.DCR
{
    public class DeadLetterQueueMessage
    {
        [JsonProperty("messageVersion")]
        public string MessageVersion { get; set; }

        [JsonProperty("messageSource")]
        public string MessageSource { get; set; }

        [JsonProperty("sourceMessageId")]
        public string SourceMessageId { get; set; }

        [JsonProperty("sourceMessageInsertionTime")]
        public string SourceMessageInsertionTime { get; set; }

        [JsonProperty("dataHolderBrandId")]
        public string DataHolderBrandId { get; set; }
    }
}