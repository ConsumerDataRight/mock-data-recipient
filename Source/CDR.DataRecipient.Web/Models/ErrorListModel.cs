using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CDR.DataRecipient.Web.Models
{
	public class ErrorListModel
	{
		public ErrorListModel()
		{
			this.Errors = new List<ErrorModel>();
		}

		public ErrorListModel(ErrorModel error)
		{
			this.Errors = new List<ErrorModel>() { error };
		}

		public ErrorListModel(string errorCode, string errorTitle, string errorDetail = null)
		{
			var error = new ErrorModel(errorCode, errorTitle, errorDetail);
			this.Errors = new List<ErrorModel>() { error };
		}

		[Required]
		public List<ErrorModel> Errors { get; set; }

		public bool HasErrors()
		{
			return Errors != null && Errors.Any();
		}
	}
}
