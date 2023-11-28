using FluentAssertions;
using Xunit;
using System.Linq;
using CDR.DataRecipient.SDK.Models;
using Moq;
using CDR.DataRecipient.SDK.Services.DataHolder;
using Moq.Protected;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CDR.DataRecipient.SDK.Services.Tokens;
using System;
using Xunit.Abstractions;
using Newtonsoft.Json;

namespace CDR.DataRecipient.IntegrationTests
{
    public class US61217_MDR_UserInfo : BaseTest
    {
        [Fact]
        public async Task AC01_DhUserInfoWithComplexAddress_ShouldNotThrowException()
        {
            // Arrange

            // Mock a DH Client with a complex json result for the User Info endpoint.
            var mockDhResponseHandler = new Mock<HttpMessageHandler>();
            var mockDhApiRequest = mockDhResponseHandler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
            mockDhApiRequest.ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"given_name\":\"Kamilla\",\"family_name\":\"Smith\",\"name\":\"Kamilla Smith\",\"aud\":\"10e6a335-78ab-410c-8722-7a13f492a9c4\",\"iss\":\"https://mock-data-holder:8001\",\"sub\":\"gAuwOMal3lXIdTDBZrykDg==\",\"address\": {    \"formatted\": \"2 Lonsdale Street, Melbourne, VIC 3000, Australia\",    \"street_address\": \"2 Lonsdale Street\",    \"locality\": \"Melbourne\",    \"region\": \"VIC\",    \"postal_code\": \"3000\",    \"country\": \"Australia\"}}")
            });
            var mockDhClient = new HttpClient(mockDhResponseHandler.Object) { BaseAddress = new Uri("https://localhost/") };

            // Mock Infosec Service to use the mock DH Endpoint
            var mockInfosecService = new Mock<InfosecService>(
                new Mock<IConfiguration>().Object, new Mock<ILogger<InfosecService>>().Object, new Mock<IAccessTokenService>().Object, new Mock<IServiceConfiguration>().Object);
            mockInfosecService.Protected()
                .Setup<HttpClient>("GetHttpClient", ItExpr.IsAny<X509Certificate2>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>())
                .Returns(mockDhClient)
                .Verifiable();

            // Act
            var resultException = await Record.ExceptionAsync(() => mockInfosecService.Object.UserInfo("https://localhost/", null, string.Empty));

            // Assert
            resultException.Should().BeNull();
        }
    }
}
