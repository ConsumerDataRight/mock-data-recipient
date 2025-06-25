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
                return this._payload;
            }

            set
            {
                this._payload = value;
                this.Data = Newtonsoft.Json.JsonConvert.DeserializeObject<Registration>(this._payload);
            }
        }
    }
}
