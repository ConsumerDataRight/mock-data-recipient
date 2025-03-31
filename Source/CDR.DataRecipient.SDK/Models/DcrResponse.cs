namespace CDR.DataRecipient.SDK.Models
{
    public class DcrResponse : Response<Registration>
    {
        private string _payload;

        public string ClientId
        {
            get
            {
                if (this.Data == null)
                {
                    return null;
                }

                return this.Data.ClientId;
            }
        }

        public string Payload
        {
            get
            {
                return _payload;
            }

            set
            {
                _payload = value;
                this.Data = Newtonsoft.Json.JsonConvert.DeserializeObject<Registration>(_payload);
            }
        }
    }
}
