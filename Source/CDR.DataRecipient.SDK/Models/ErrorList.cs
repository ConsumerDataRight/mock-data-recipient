using System.Collections.Generic;

namespace CDR.DataRecipient.SDK.Models
{
    public class ErrorList
    {
        public List<Error> Errors { get; set; }

        public bool HasErrors()
        {
            return Errors != null && Errors.Count > 0;
        }

        public ErrorList()
        {
            this.Errors = new List<Error>();
        }

        public ErrorList(Error error)
        {
            this.Errors = new List<Error>() { error };
        }

        public ErrorList(string errorCode, string errorTitle, string errorDetail)
        {
            var error = new Error(errorCode, errorTitle, errorDetail);
            this.Errors = new List<Error>() { error };
        }
    }
}
