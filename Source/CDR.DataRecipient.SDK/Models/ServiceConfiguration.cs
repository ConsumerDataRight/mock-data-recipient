namespace CDR.DataRecipient.SDK.Models
{
#pragma warning disable SA1649 // File name should match first type name
    public interface IServiceConfiguration
#pragma warning restore SA1649 // File name should match first type name
    {
        bool AcceptAnyServerCertificate { get; set; }

        bool EnforceHttpsEndpoints { get; set; }
    }

    public class ServiceConfiguration : IServiceConfiguration
    {
        public ServiceConfiguration()
        {
            // Set defaults.
            this.AcceptAnyServerCertificate = false;
            this.EnforceHttpsEndpoints = true;
        }

        public bool AcceptAnyServerCertificate { get; set; }

        public bool EnforceHttpsEndpoints { get; set; }
    }
}
