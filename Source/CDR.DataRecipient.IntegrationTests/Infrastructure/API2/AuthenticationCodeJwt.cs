using System;
using System.Collections.Generic;

#nullable enable

namespace CDR.DataRecipient.IntegrationTests.Infrastructure.API2
{
    public class AuthenticationCodeJwt
    {
        public string? CertificateFilename { get; init; }
        public string? CertificatePassword { get; init; }
        public string? Iss { get; init; }
        public string? Aud { get; init; }
        public string? Kid { get; init; }
        public string? Nfb { get; init; }
        public string State { get; init; } = "";
        public string Code { get; init; } = "";
        public string? ExpiryTimeInSeconds { get; init; }

        public string Get()
        {
            _ = CertificateFilename ?? throw new ArgumentNullException(nameof(CertificateFilename));
            _ = CertificatePassword ?? throw new ArgumentNullException(nameof(CertificatePassword));
            _ = Kid ?? throw new ArgumentNullException(nameof(Kid));

            var now = DateTime.UtcNow;

            var subject = new Dictionary<string, object>
            {
                {"state", State },
                {"code", Code},
                { "nfb", new DateTimeOffset(now).ToUnixTimeSeconds() },
                { "iat", new DateTimeOffset(now).ToUnixTimeSeconds() },
            };

            if (ExpiryTimeInSeconds != null)
            {
                var exp = now.AddSeconds(int.Parse(ExpiryTimeInSeconds));
                subject.Add("exp", new DateTimeOffset(exp).ToUnixTimeSeconds());
            }

            if (Iss != null)
            {
                subject.Add("iss", Iss.ToLower());
            }

            if (Aud != null)
            {
                subject.Add("aud", Aud);
            }

            var jwt = JWT2.CreateJWT(CertificateFilename, CertificatePassword, subject, Kid);

            return jwt;
        }
    }
}
