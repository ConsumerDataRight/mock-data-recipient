using Serilog;

namespace CDR.DataRecipient.API.Logger
{
    /// <summary>
    /// Interface for logging request and response information.
    /// </summary>
    public interface IRequestResponseLogger
    {
        /// <summary>
        /// Gets the logger instance used for logging.
        /// </summary>
        ILogger Log { get; }
    }
}
