using System.Collections.Generic;

namespace CDR.DataRecipient.SDK.Models
{
    public class ErrorList
    {
        public ErrorList()
        {
            this.Errors = [];
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

        public List<Error> Errors { get; set; }

        public bool HasErrors()
        {
            return this.Errors != null && this.Errors.Count > 0;
        }
    }
}
