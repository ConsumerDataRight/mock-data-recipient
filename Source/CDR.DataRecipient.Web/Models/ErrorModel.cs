using System.ComponentModel.DataAnnotations;

namespace CDR.DataRecipient.Web.Models
{
	public class ErrorModel
	{
        public ErrorModel()
        {
            Meta = null;
            Detail = "";
        }

        public ErrorModel(string code, string title, string description) : this()
        {
            Code = code;
            Title = title;
            Detail = description;
        }

        [Required]
        public string Code { get; set; }
        [Required]
        public string Title { get; set; }
        public string Detail { get; set; }
        public object Meta { get; set; }
    }
}
