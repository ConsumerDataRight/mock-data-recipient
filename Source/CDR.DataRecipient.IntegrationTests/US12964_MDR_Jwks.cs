using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Newtonsoft.Json;
using Xunit;
using System.Linq;

namespace CDR.DataRecipient.IntegrationTests
{
	public class US12964_MDR_Jwks : BaseTest
	{
		class Jwks_Expected
		{
			public class Key
			{
#pragma warning disable IDE1006                
				public string kty { get; set; }
				public string use { get; set; }
				public string kid { get; set; }
				public string e { get; set; }
				public string n { get; set; }
#pragma warning restore IDE1006                
			}

			public Key[] Keys { get; set; }
		}

		[Fact]
		public async Task AC01_Get_ShouldRespondWith_200OK_ValidJWKS()
		{			
			// Arrange
			var apiCall = new Infrastructure.API
			{
				HttpMethod = HttpMethod.Get,
				URL = $"https://{HOSTNAME_DATARECIPIENT}:9001/jwks",
			};

            // Act
            var response = await apiCall.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                // Assert - Check content type
                Assert_HasContentType_ApplicationJson(response.Content);

                // Assert - Check JWKS
                var actualJson = await response.Content.ReadAsStringAsync();
                var actual = JsonConvert.DeserializeObject<Jwks_Expected>(actualJson);
                actual.Keys.Length.Should().Be(2);
                var sigKey = actual.Keys.FirstOrDefault(k => k.use == "sig");
                sigKey.Should().NotBeNull();
                var encKey = actual.Keys.FirstOrDefault(k => k.use == "enc");
                encKey.Should().NotBeNull();
            }
        }
	}
}
