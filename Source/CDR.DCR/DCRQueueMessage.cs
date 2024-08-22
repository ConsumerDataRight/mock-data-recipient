using Azure.Storage.Queues.Models;
using Newtonsoft.Json;
using System;

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