namespace CDR.DataRecipient.API.Logger
{
    using Serilog;

    public interface IRequestResponseLogger
    {
        ILogger Log { get; }
    }
}