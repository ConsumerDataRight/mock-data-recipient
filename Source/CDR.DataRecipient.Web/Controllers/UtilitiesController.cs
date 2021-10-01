using CDR.DataRecipient.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CDR.DataRecipient.Web.Configuration;
using CDR.DataRecipient.SDK.Extensions;
using System;
using CDR.DataRecipient.SDK.Register;

namespace CDR.DataRecipient.Web.Controllers
{
    [Route("utilities")]
    public class UtilitiesController : Controller
    {
        public const string DEFAULT_PRIVATE_KEY =
            @"-----BEGIN PRIVATE KEY-----
MIIEvwIBADANBgkqhkiG9w0BAQEFAASCBKkwggSlAgEAAoIBAQDqSkpDrvhkPp0K
slpl7NqHgemvwPnGjg+fhOJsO1apavjaCGrZJ0nT2VoeKXiHW3727dgtst41CVur
aLPK9C/+XyZZ8FGpvv5SAR9PgQUCWuIPmHzjGxuwrzmVrPumHssDySPXXO3G5SqN
4ZuKtyeDa/oYzyfMxKTyfiD2c1LLf4bSdlw9g+phxshLA+IVf1E3ho8rKyi64nWT
UghV0/NRVoBDA/kX1HtMTPWVaQPguPWfD6bHonF7l+KjO4xLzusPwD0Be2477num
Zb+bjR+IMlEXtyxg5gvU3fYBRwgislbQph0iZa1o4yb6YACVsZfbPPfOdcSUpoSb
SsGpl3HlAgMBAAECggEBAN+LQK6xzNjBEVA6epxT0PKIVzNVujzkIS4AOZYxS+/c
XFKUw3Ys0XlsIlszEIh+GXd72s9HolMSeb/j5+CW+xAHM22PKiv/S2NtJLXUEbZ8
hsOAqHB81f/QQO56HaYULXqQOT1ssoca825qu+Ev/mib5wYy6fOsEQI6rDLaOD5m
vQ0CauaTjQdOZJ7CarMJeIz1HphwN+veW/YWuYNLmNsdn0B0/LFXMRunZ69wovfI
4+fn2tjakqTv7FjdbK+ZXrQIyDQst/3i4f5ttgnfcgCa21aSEnC39+dV9VDyQXJc
D4fo27c2dqzjnMcITl5LQ1oe1fjIjIQ+Y6OJX+pR7QECgYEA+VzvvZwn41PPTta+
1PekS3Uv3OKfxW8Jk8HCxvJLJs0E7lwiJsl+bpv4IwS9S7mQ62XN8GpVEa1fooRa
ZUj3zks6ISvPz8Gqq0WNTSCcRQckUBNqkVqVlrV4Obruz9Ymu1Grm1mUiRAYUoYQ
3uaimUrCdmmdHM7R6MA4gpzhZcECgYEA8IanMht+NrzTm4Ljnv7D59F0dysTrLV/
xgGlARSlsH2gi5F01MxCSDQV4FiXG8+LyWNns4lmmFuIdVbImT2tt44OQsn4WxFi
17i7YMq56bigd0klVg76w5tdBVP1dJSHDBOBk6VpXUnV+qkkNXFljwjuN46BIRDO
zuIe/CB0/SUCgYEAsM+R+grwSYMSml8wHoedShfEoUVbbj2mN8uKlVAVs2Rpm61e
VcxHRpx23DWvFzNzq0WbOV3cBdW92tkn02tisjaq9/w9tJ0oq5p8b3Sw+UzwFYs+
4+Or75mqrpx6WooJGob1PAjPhkQQSuteqP41yqW0rwuB6HxJYFRzfUFJnMECgYEA
4qzT9xoH6YgtGJrS722DrP4td31GTnbCUxYLrigKOnk6iy3q3/0b3jYQA90Zk6EW
SRYAjifIY3+n64V9CUYiaCFdeT4ka5bIAytak86aRRS1TZXGtQLq+tt5X+MPKO4t
E4lyjXXPBZbnLRKoSCF1J8av0fXf5gyUCk76CnyAJG0CgYA38AmsG4/z83G0g8RG
3i9Jzxw7QCVJfaibZ5hoSsK61wMHHFW4JrlO7uhIOd4bn0iV2Mqj1bD6Kg4Tfxc3
//TigAqQ6M46sweOglFlFkKmBtAmjsfBpHMSOyIedCxCBYQESeW4BdqXMU+OqUnZ
RIj2P/wtcJRnuztcszBJsnt7NQ==
-----END PRIVATE KEY-----
";

        private readonly ILogger<UtilitiesController> _logger;
        private readonly IConfiguration _config;

        public UtilitiesController(
            IConfiguration config,
            ILogger<UtilitiesController> logger)
        {
            _logger = logger;
            _config = config;
        }

        [HttpGet]
        [Route("id-token")]
        public IActionResult IdToken()
        {
            var model = new IdTokenModel();
            return View(model);
        }

        [HttpPost]
        [Route("id-token")]
        public IActionResult IdToken(IdTokenModel model)
        {
            var sp = _config.GetSoftwareProductConfig();

            if (string.IsNullOrEmpty(model.IdTokenEncrypted))
            {
                return View(model);
            }

            model.IdTokenDecrypted = model.IdTokenEncrypted.DecryptIdToken(sp.SigningCertificate.X509Certificate);
            model.IdTokenClaims = model.IdTokenDecrypted.GetTokenClaims();

            return View(model);
        }

        [HttpGet]
        [Route("private-key-jwt")]
        public IActionResult PrivateKeyJwt()
        {
            var model = new PrivateKeyJwtModel();
            SetDefaults(model);
            return View(model);
        }

        [HttpPost]
        [Route("private-key-jwt")]
        public IActionResult PrivateKeyJwt(PrivateKeyJwtModel model)
        {
            var sp = _config.GetSoftwareProductConfig();
            var privateKeyFormatted = FormatPrivateKey(model.PrivateKey);
            var privateKeyJwt = new PrivateKeyJwt(privateKeyFormatted);
            model.ClientAssertion = privateKeyJwt.Generate(model.Issuer, model.Audience, model.Jti, model.ExpiryMinutes);
            model.ClientAssertionClaims = model.ClientAssertion.GetTokenClaims();

            return View(model);
        }

        /// <summary>
        /// Apply formatting to the provided private key.
        /// </summary>
        /// <param name="privateKey">Raw private key</param>
        /// <returns></returns>
        private string FormatPrivateKey(string privateKey)
        {
            return privateKey
                .Replace("-----BEGIN PRIVATE KEY-----", "")
                .Replace("-----END PRIVATE KEY-----", "")
                .Replace("\r\n", "")
                .Trim();
        }

        private void SetDefaults(PrivateKeyJwtModel model)
        {
            var sp = _config.GetSoftwareProductConfig();
            var reg = _config.GetRegisterConfig();

            model.Issuer = sp.SoftwareProductId;
            model.Audience = reg.TokenEndpoint;
            model.PrivateKey = DEFAULT_PRIVATE_KEY;
            model.ExpiryMinutes = 10;
        }
    }
}
