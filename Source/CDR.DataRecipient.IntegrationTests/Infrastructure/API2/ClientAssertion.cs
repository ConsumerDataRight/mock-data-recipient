using System;
using System.Collections.Generic;

#nullable enable

namespace CDR.DataRecipient.IntegrationTests.Infrastructure.API2
{
    public class ClientAssertion
    {
        public string? CertificateFilename { get; init; } //= BaseTest.CERTIFICATE_FILENAME;
        public string? CertificatePassword { get; init; } //= BaseTest.CERTIFICATE_PASSWORD;
        public string? Iss { get; init; } // = BaseTest.SOFTWAREPRODUCT_ID.ToLower();
        public string? Aud { get; init; }
        public string? Kid { get; init; }

        public string Get()
        {
            _ = CertificateFilename ?? throw new ArgumentNullException(nameof(CertificateFilename));
            _ = CertificatePassword ?? throw new ArgumentNullException(nameof(CertificatePassword));
            _ = Kid ?? throw new ArgumentNullException(nameof(Kid));

            var now = DateTime.UtcNow;
            var exp = now.AddSeconds(300);

            var subject = new Dictionary<string, object>
            {
                { "iat", new DateTimeOffset(now).ToUnixTimeSeconds() },
                { "exp", new DateTimeOffset(exp).ToUnixTimeSeconds() },
                { "jti", Guid.NewGuid().ToString() },
            };

            if (Iss != null)
            {
                subject.Add("iss", Iss);
                subject.Add("sub", Iss);
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
