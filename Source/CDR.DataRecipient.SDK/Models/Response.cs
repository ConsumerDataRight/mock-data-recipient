using System.Net;

namespace CDR.DataRecipient.SDK.Models
{
    public class Response
    {
        public Response()
        {
            this.Errors = new ErrorList();
        }

        public bool IsSuccessful
        {
            get
            {
                return ((int)this.StatusCode) < 400;
            }
        }

        public HttpStatusCode StatusCode { get; set; }

        public string Message { get; set; }

        public ErrorList Errors { get; set; }
    }

    public class Response<T> : Response
    {
        public T Data { get; set; }
    }
}
