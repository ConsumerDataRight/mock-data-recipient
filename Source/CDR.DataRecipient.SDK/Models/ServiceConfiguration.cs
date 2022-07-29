namespace CDR.DataRecipient.SDK.Models
{
    public class ServiceConfiguration : IServiceConfiguration
    {
        public bool AcceptAnyServerCertificate { get; set; }
        public bool EnforceHttpsEndpoints { get; set; }

        public ServiceConfiguration()
        {
            // Set defaults.
            AcceptAnyServerCertificate = false;
            EnforceHttpsEndpoints = true;
        }
    }

    public interface IServiceConfiguration
    {
        bool AcceptAnyServerCertificate { get; set; }
        bool EnforceHttpsEndpoints { get; set; }
    }
}
