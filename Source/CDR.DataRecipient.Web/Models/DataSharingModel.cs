namespace CDR.DataRecipient.Web.Models
{
    public class DataSharingModel
    {
        public string CdsSwaggerLocation { get; set; }
        /// <summary>
        /// This is the group name of the APIs according to the standards. e.g. "Banking" API, "Common" API.
        /// </summary>
        public string ApiGroupName { get; set; }
        public string BasePath { get; set; }
    }
}
