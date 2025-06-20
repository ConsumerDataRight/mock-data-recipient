using System.Net;

namespace CDR.DataRecipient.SDK.Models
{
    public class Response<T> : Response
    {
        public T Data { get; set; }
    }
}
