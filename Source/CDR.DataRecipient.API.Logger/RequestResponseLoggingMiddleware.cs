using System.Configuration;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IO;
using Microsoft.Net.Http.Headers;
using Serilog;
using Serilog.Events;

namespace CDR.DataRecipient.API.Logger
{
    public class RequestResponseLoggingMiddleware
    {
        const string httpSummaryMessageTemplate =
            "HTTP {RequestMethod} {RequestScheme:l}://{RequestHost:l}{RequestPathBase:l}{RequestPath:l} responded {StatusCode} in {ElapsedTime:0.0000} ms.";

        const string httpSummaryExceptionMessageTemplate =
            "HTTP {RequestMethod} {RequestScheme:l}://{RequestHost:l}{RequestPathBase:l}{RequestPath:l} encountered following error {error}";

        private string? _requestMethod;
        private string? _requestBody;
        private string? _requestHeaders;
        private string? _requestPath;
        private string? _requestQueryString;
        private string? _statusCode;
        private string? _elapsedTime;
        private string? _responseHeaders;
        private string? _responseBody;
        private string? _requestHost;
        private string? _requestIpAddress;
        private string? _requestScheme;
        private string? _exceptionMessage;
        private string? _requestPathBase;
        private string? _clientId;
        private string? _softwareId;
        private string? _fapiInteractionId;
        private string? _dataHolderBrandId;


        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
        readonly RequestDelegate _next;
        private readonly ILogger _requestResponseLogger;
        private readonly IConfiguration _configuration;

        public RequestResponseLoggingMiddleware(RequestDelegate next, IRequestResponseLogger requestResponseLogger, IConfiguration configuration)
        {
            _requestResponseLogger = requestResponseLogger.Log.ForContext<RequestResponseLoggingMiddleware>();
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            InitMembers();
            await ExtractRequestProperties(context);
            await ExtractResponseProperties(context);
        }

        private void InitMembers()
        {
            _requestMethod = _requestBody = _requestHeaders = _requestPath = _requestQueryString =
            _statusCode = _elapsedTime = _responseHeaders = _responseBody = _requestHost = _requestScheme =
            _exceptionMessage = _requestPathBase = _clientId = _softwareId = _fapiInteractionId = _dataHolderBrandId = _requestIpAddress = string.Empty;
        }

        private void LogWithContext()
        {
            var logger = _requestResponseLogger
                .ForContext("SourceContext", "SB-DR-RES")
                .ForContext("RequestMethod", _requestMethod)
                .ForContext("RequestHost", _requestHost)
                .ForContext("RequestIpAddress", _requestIpAddress)
                .ForContext("RequestBody", _requestBody)
                .ForContext("RequestHeaders", _requestHeaders)
                .ForContext("RequestPath", _requestPath)
                .ForContext("RequestQueryString", _requestQueryString)
                .ForContext("StatusCode", _statusCode)
                .ForContext("ResponseHeaders", _responseHeaders)
                .ForContext("ResponseBody", _responseBody)
                .ForContext("ClientId", _clientId)
                .ForContext("SoftwareId", _softwareId)
                .ForContext("FapiInteractionId", _fapiInteractionId);

            if (!string.IsNullOrEmpty(_exceptionMessage))
            {
                logger.Error(httpSummaryExceptionMessageTemplate, _requestMethod, _requestScheme, _requestHost, _requestPathBase, _requestPath, _exceptionMessage);
            }
            else
            {
                logger.Write(LogEventLevel.Information, httpSummaryMessageTemplate, _requestMethod, _requestScheme, _requestHost, _requestPathBase, _requestPath, _statusCode, _elapsedTime);
            }
        }


        private async Task ExtractRequestProperties(HttpContext context)
        {
            try
            {
                context.Request.EnableBuffering();
                await using var requestStream = _recyclableMemoryStreamManager.GetStream();
                await context.Request.Body.CopyToAsync(requestStream);

                _requestBody = ReadStreamInChunks(requestStream);
                context.Request.Body.Position = 0;

                _requestHost = GetHost(context.Request);
                _requestIpAddress = GetIpAddress(context);
                _requestMethod = context.Request.Method;
                _requestScheme = context.Request.Scheme;
                _requestPath = context.Request.Path;
                _requestQueryString = context.Request.QueryString.ToString();
                _requestPathBase = context.Request.PathBase.ToString();

                IEnumerable<string> keyValues = context.Request.Headers.Keys.Select(key => key + ": " + string.Join(",", context.Request.Headers[key]));
                _requestHeaders = string.Join(Environment.NewLine, keyValues);

                ExtractIdFromRequest(context.Request);
            }
            catch (Exception ex)
            {
                _exceptionMessage = ex.Message;
            }
        }

        class ClaimIdentifiers
        {
            public const string ClientId = "client_id";
            public const string SoftwareId = "software_id";
            public const string Iss = "iss";
        }

        void SetIdFromJwt(string jwt, string identifierType, ref string idToSet)
        {
            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(jwt) == true)
            {
                var decodedJwt = handler.ReadJwtToken(jwt);
                var id = decodedJwt.Claims.FirstOrDefault(x => x.Type == identifierType)?.Value ?? String.Empty;

                idToSet = id;
            }
        }

        private void ExtractIdFromRequest(HttpRequest request)
        {
            try
            {
                //try fetching from the JWT in the authorization header - /arrangements/revoke
                var authorization = request.Headers[HeaderNames.Authorization];
                if (AuthenticationHeaderValue.TryParse(authorization, out var headerValue))
                {
                    var scheme = headerValue.Scheme;
                    var parameter = headerValue.Parameter;

                    if (scheme == JwtBearerDefaults.AuthenticationScheme && parameter != null)
                    {
                        _dataHolderBrandId = String.Empty;
                        SetIdFromJwt(parameter, ClaimIdentifiers.Iss, ref _dataHolderBrandId);
                    }
                }                
            }
            catch (Exception ex)
            {
                _exceptionMessage = ex.Message;
            }

        }

        private string ReadStreamInChunks(Stream stream)
        {
            try
            {
                const int readChunkBufferLength = 4096;
                stream.Seek(0, SeekOrigin.Begin);
                using var textWriter = new StringWriter();
                using var reader = new StreamReader(stream);
                var readChunk = new char[readChunkBufferLength];
                int readChunkLength;
                do
                {
                    readChunkLength = reader.ReadBlock(readChunk, 0, readChunkBufferLength);
                    textWriter.Write(readChunk, 0, readChunkLength);
                } while (readChunkLength > 0);
                return textWriter.ToString();
            }
            catch (Exception ex)
            {
                _exceptionMessage = ex.Message;
            }

            return "";
        }


        private async Task ExtractResponseProperties(HttpContext httpContext)
        {

            var originalBodyStream = httpContext.Response.Body;
            await using var responseBody = _recyclableMemoryStreamManager.GetStream();
            httpContext.Response.Body = responseBody;

            var sw = Stopwatch.StartNew();

            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                _exceptionMessage = ex.Message;
                throw;
            }
            finally
            {
                sw.Stop();
                _elapsedTime = sw.ElapsedMilliseconds.ToString();

                responseBody.Seek(0, SeekOrigin.Begin);
                _responseBody = await new StreamReader(responseBody).ReadToEndAsync();
                responseBody.Seek(0, SeekOrigin.Begin);

                IEnumerable<string> keyValues = httpContext.Response.Headers.Keys.Select(key => key + ": " + string.Join(",", httpContext.Response.Headers[key]));
                _responseHeaders = string.Join(System.Environment.NewLine, keyValues);

                _statusCode = httpContext.Response.StatusCode.ToString();

                LogWithContext();

                // This is for middleware hooked before us to see our changes.
                // Otherwise the original stream would be seen which cannot be read again.
                // The middleware that sends the response to the client is affected as well.
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
        private string GetHost(HttpRequest request)
        {
            // 1. check if the X-Forwarded-Host header has been provided -> use that
            // 2. If not, use the request.Host
            string hostHeaderKey = _configuration.GetValue<string>("SerilogRequestResponseLogger:HostNameHeaderKey") ?? "X-Forwarded-Host";

            if (!request.Headers.TryGetValue(hostHeaderKey, out var keys))
            {
                return request.Host.ToString();
            }

            return keys.First();
        }

        private string? GetIpAddress(HttpContext context)
        {
            string ipHeaderKey = _configuration.GetValue<string>("SerilogRequestResponseLogger:IPAddressHeaderKey") ?? "X-Forwarded-For";

            if (!context.Request.Headers.TryGetValue(ipHeaderKey, out var keys))
            {
                return context.Connection.RemoteIpAddress?.ToString();
            }

            // The Client IP address may contain a comma separated list of ip addresses based on the network devices
            // the traffic traverses through.  We get the first (and potentially only) ip address from the list as the client IP.
            // We also remove any port numbers that may be included on the client IP.
            return keys.First()
                .Split(',')[0]  // Get the first IP address in the list, in case there are multiple.
                .Split(':')[0]; // Strip off the port number, in case it is attached to the IP address.
        }

    }
}
