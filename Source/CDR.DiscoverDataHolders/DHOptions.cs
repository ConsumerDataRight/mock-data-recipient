namespace CDR.DiscoverDataHolders
{
    public class DHOptions
    {
        public string DataRecipient_DB_ConnectionString { get; set; }
        public string DataRecipient_Logging_DB_ConnectionString { get; set; }
        public string AzureWebJobsStorage { get; set; }
        public string StorageConnectionString { get; set; }
        public string FUNCTIONS_WORKER_RUNTIME { get; set; }
        public string Register_Token_Endpoint { get; set; }
        public string Register_Get_DH_Brands { get; set; }
        public string Register_Get_DH_Brands_XV { get; set; }
        public string Software_Product_Id { get; set; }
        public string Client_Certificate { get; set; }
        public string Client_Certificate_Password { get; set; }
        public string Signing_Certificate { get; set; }
        public string Signing_Certificate_Password { get; set; }
        public bool Ignore_Server_Certificate_Errors { get; set; }
        public string QueueName { get; set; }
    }
}
