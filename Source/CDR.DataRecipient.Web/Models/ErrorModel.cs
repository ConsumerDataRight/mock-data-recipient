using System.ComponentModel.DataAnnotations;

namespace CDR.DataRecipient.Web.Models
{
    public class ErrorModel
    {
        public ErrorModel()
        {
            this.Meta = null;
            this.Detail = string.Empty;
        }

        public ErrorModel(string code, string title, string description)
            : this()
        {
            this.Code = code;
            this.Title = title;
            this.Detail = description;
        }

        [Required]
        public string Code { get; set; }

        [Required]
        public string Title { get; set; }

        public string Detail { get; set; }

        public object Meta { get; set; }
    }
}
