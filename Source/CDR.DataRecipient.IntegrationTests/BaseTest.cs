using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using CDR.DataRecipient.IntegrationTests.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Xunit;
using Xunit.Sdk;

#nullable enable

namespace CDR.DataRecipient.IntegrationTests
{
    class DisplayTestMethodNameAttribute : BeforeAfterTestAttribute
    {
        static int count = 0;

        public override void Before(MethodInfo methodUnderTest)
        {
            Console.WriteLine($"Test #{++count} - {methodUnderTest.DeclaringType?.Name}.{methodUnderTest.Name}");
        }

        public override void After(MethodInfo methodUnderTest)
        {
        }
    }

    // Put all tests in same collection because we need them to run sequentially since some tests are mutating DB.
    [Collection("IntegrationTests")]
    [TestCaseOrderer("CDR.DataRecipient.IntegrationTests.XUnit.Orderers.AlphabeticalOrderer", "CDR.DataRecipient.IntegrationTests")]
    [DisplayTestMethodName]
    abstract public class BaseTest
    {
        // Register
        public static string REGISTER_MTLS_TOKEN_URL => $"https://{HOSTNAME_REGISTER}:7001/idp/connect/token"; // Register Token API
        public static string REGISTER_MTLS_DATAHOLDERBRANDS_URL => $"https://{HOSTNAME_REGISTER}:7001/cdr-register/v1/banking/data-holders/brands";
        public const string CLIENTASSERTIONTYPE = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";

        // Data recipient
        public const string CERTIFICATE_FILENAME = "Certificates/client.pfx";
        public const string CERTIFICATE_PASSWORD = "#M0ckDataRecipient#";
        public const string JWT_CERTIFICATE_FILENAME = "Certificates/jwks.pfx";
        public const string JWT_CERTIFICATE_PASSWORD = "#M0ckDataRecipient#";

        public const string SOFTWAREPRODUCT_ID = "C6327F87-687A-4369-99A4-EAACD3BB8210";

        public static string DATARECIPIENT_ARRANGEMENTS_REVOKE_URL => $"https://{HOSTNAME_DATARECIPIENT}:9001/arrangements/revoke";

        // Data holder
        public const string DATAHOLDER_CERTIFICATE_FILENAME = "Certificates/mock-data-holder-server.pfx";
        public const string DATAHOLDER_CERTIFICATE_PASSWORD = "#M0ckDataHolder#";
        public const string DATAHOLDER_BRAND = "804FC2FB-18A7-4235-9A49-2AF393D18BC7"; // Bank 1
        public static string DATAHOLDER_BRAND_INFOSECBASEURL => $"https://{HOSTNAME_DATAHOLDER}:8001";

        // SQL connection strings
        static public string REGISTER_CONNECTIONSTRING => Configuration["ConnectionStrings:Register"]
            ?? throw new Exception($"{nameof(REGISTER_CONNECTIONSTRING)} - configuration setting not found");
        static public string DATARECIPIENT_CONNECTIONSTRING => Configuration["ConnectionStrings:DataRecipient"]
            ?? throw new Exception($"{nameof(DATARECIPIENT_CONNECTIONSTRING)} - configuration setting not found");

        // Hostnames
        static public string HOSTNAME_REGISTER => Configuration["Hostnames:Register"]
            ?? throw new Exception($"{nameof(HOSTNAME_REGISTER)} - configuration setting not found");
        static public string HOSTNAME_DATAHOLDER => Configuration["Hostnames:DataHolder"]
            ?? throw new Exception($"{nameof(HOSTNAME_DATAHOLDER)} - configuration setting not found");
        static public string HOSTNAME_DATARECIPIENT => Configuration["Hostnames:DataRecipient"]
            ?? throw new Exception($"{nameof(HOSTNAME_DATARECIPIENT)} - configuration setting not found");

        static private IConfigurationRoot? configuration;
        static public IConfigurationRoot Configuration
        {
            get
            {
                if (configuration == null)
                {
                    configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json")
                        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true)
                        .Build();
                }

                return configuration;
            }
        }

        /// <summary>
        /// Assert response content and expectedJson are equivalent
        /// </summary>
        /// <param name="expectedJson">The expected json</param>
        /// <param name="content">The response content</param>
        public static async Task Assert_HasContent_Json(string? expectedJson, HttpContent? content)
        {
            content.Should().NotBeNull();
            if (content == null)
            {
                return;
            }

            var actualJson = await content.ReadAsStringAsync();
            Assert_Json(expectedJson, actualJson);
        }

        /// <summary>
        /// Assert response content is empty
        /// </summary>
        /// <param name="content">The response content</param>
        public static async Task Assert_HasNoContent(HttpContent? content)
        {
            content.Should().NotBeNull();
            if (content == null)
            {
                return;
            }

            var actualJson = await content.ReadAsStringAsync();
            actualJson.Should().BeNullOrEmpty();
        }

        /// <summary>
        /// Assert actual json is equivalent to expected json
        /// </summary>
        /// <param name="expectedJson">The expected json</param>
        /// <param name="actualJson">The actual json</param>
        public static void Assert_Json(string? expectedJson, string actualJson)
        {
            static object? Deserialize(string name, string json)
            {
                try
                {
                    return JsonConvert.DeserializeObject<object>(json);
                }
                catch
                {
                    throw new Exception($@"Error deserialising {name} - ""{json}""");
                }
            }

            expectedJson.Should().NotBeNull();
            actualJson.Should().NotBeNull();

            if (expectedJson == null || actualJson == null)
            {
                return;
            }

            object? expectedObject = Deserialize(nameof(expectedJson), expectedJson);
            object? actualObject = Deserialize(nameof(actualJson), actualJson);

            var expectedJsonNormalised = JsonConvert.SerializeObject(expectedObject);
            var actualJsonNormalised = JsonConvert.SerializeObject(actualObject);

            actualJson?.JsonCompare(expectedJson).Should().BeTrue(
                $"\r\nExpected json:\r\n{expectedJsonNormalised}\r\nActual Json:\r\n{actualJsonNormalised}\r\n"
            );

        }

        /// <summary>
        /// Assert headers has a single header with the expected value.
        /// If expectedValue then just check for the existence of the header (and not it's value)
        /// </summary>
        /// <param name="expectedValue">The expected header value</param>
        /// <param name="headers">The headers to check</param>
        /// <param name="name">Name of header to check</param>
        public static void Assert_HasHeader(string? expectedValue, HttpHeaders headers, string name, bool startsWith = false)
        {
            headers.Should().NotBeNull();
            if (headers != null)
            {
                headers.Contains(name).Should().BeTrue($"name={name}");
                if (headers.Contains(name))
                {
                    var headerValues = headers.GetValues(name);
                    headerValues.Should().ContainSingle(name, $"name={name}");

                    if (expectedValue != null)
                    {
                        string headerValue = headerValues.First();

                        if (startsWith)
                        {
                            headerValue.Should().StartWith(expectedValue, $"name={name}");
                        }
                        else
                        {
                            headerValue.Should().Be(expectedValue, $"name={name}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Assert header has content type of ApplicationJson
        /// </summary>
        /// <param name="content"></param>
        public static void Assert_HasContentType_ApplicationJson(HttpContent content)
        {
            content.Should().NotBeNull();
            content?.Headers.Should().NotBeNull();
            content?.Headers?.ContentType.Should().NotBeNull();
            content?.Headers?.ContentType?.ToString().Should().StartWith("application/json");
        }

        /// <summary>
        /// Assert claim exists
        /// </summary>
        public static void AssertClaim(IEnumerable<Claim> claims, string claimType, string claimValue)
        {
            claims.Should().NotBeNull();
            if (claims != null)
            {
                claims.FirstOrDefault(claim => claim.Type == claimType && claim.Value == claimValue).Should().NotBeNull($"Expected {claimType}={claimValue}");
            }
        }
    }
}
