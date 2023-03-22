using FluentAssertions;
using Xunit;
using System.Linq;
using CDR.DataRecipient.SDK.Models;

namespace CDR.DataRecipient.IntegrationTests
{
    public class US44937_MDR_MTLSEndpointAliases : BaseTest
    {
        [Theory]
        [InlineData("TokenEndpoint", nameof(OidcDiscovery.TokenEndpoint), "TokenEndpoint", null)]
        [InlineData("TokenEndpoint", nameof(OidcDiscovery.TokenEndpoint), "TokenEndpoint", "")]
        [InlineData("TokenEndpoint alias", nameof(OidcDiscovery.TokenEndpoint), "TokenEndpoint", "TokenEndpoint alias")]
        [InlineData("RevocationEndpoint", nameof(OidcDiscovery.RevocationEndpoint), "RevocationEndpoint", null)]
        [InlineData("RevocationEndpoint", nameof(OidcDiscovery.RevocationEndpoint), "RevocationEndpoint", "")]
        [InlineData("RevocationEndpoint alias", nameof(OidcDiscovery.RevocationEndpoint), "RevocationEndpoint", "RevocationEndpoint alias")]
        [InlineData("IntrospectionEndpoint", nameof(OidcDiscovery.IntrospectionEndpoint), "IntrospectionEndpoint", null)]
        [InlineData("IntrospectionEndpoint", nameof(OidcDiscovery.IntrospectionEndpoint), "IntrospectionEndpoint", "")]
        [InlineData("IntrospectionEndpoint alias", nameof(OidcDiscovery.IntrospectionEndpoint), "IntrospectionEndpoint", "IntrospectionEndpoint alias")]
        [InlineData("UserInfoEndpoint", nameof(OidcDiscovery.UserInfoEndpoint), "UserInfoEndpoint", null)]
        [InlineData("UserInfoEndpoint", nameof(OidcDiscovery.UserInfoEndpoint), "UserInfoEndpoint", "")]
        [InlineData("UserInfoEndpoint alias", nameof(OidcDiscovery.UserInfoEndpoint), "UserInfoEndpoint", "UserInfoEndpoint alias")]
        [InlineData("RegistrationEndpoint", nameof(OidcDiscovery.RegistrationEndpoint), "RegistrationEndpoint", null)]
        [InlineData("RegistrationEndpoint", nameof(OidcDiscovery.RegistrationEndpoint), "RegistrationEndpoint", "")]
        [InlineData("RegistrationEndpoint alias", nameof(OidcDiscovery.RegistrationEndpoint), "RegistrationEndpoint", "RegistrationEndpoint alias")]
        [InlineData("PushedAuthorizationRequestEndpoint", nameof(OidcDiscovery.PushedAuthorizationRequestEndpoint), "PushedAuthorizationRequestEndpoint", null)]
        [InlineData("PushedAuthorizationRequestEndpoint", nameof(OidcDiscovery.PushedAuthorizationRequestEndpoint), "PushedAuthorizationRequestEndpoint", "")]
        [InlineData("PushedAuthorizationRequestEndpoint alias", nameof(OidcDiscovery.PushedAuthorizationRequestEndpoint), "PushedAuthorizationRequestEndpoint", "PushedAuthorizationRequestEndpoint alias")]
        public void AC01_WhenMtlsEndpointAlias_ShouldReturnAlias(string expectedValue, string propertyName, string propertyValue, string aliasPropertyValue)
        {
            var property = typeof(OidcDiscovery).GetProperties().First(p => p.Name == propertyName);
            var mtlsAliasedProperty = typeof(MtlsAliases).GetProperties().First(p => p.Name == propertyName);

            // Arrange           
            var oidcDiscovery = new OidcDiscovery();

            property.SetValue(oidcDiscovery, propertyValue);

            if (!string.IsNullOrEmpty(aliasPropertyValue))
            {
                // Set aliased property value                
                oidcDiscovery.MtlsEndpointAliases = new MtlsAliases();
                mtlsAliasedProperty.SetValue(oidcDiscovery.MtlsEndpointAliases, aliasPropertyValue);
            }

            // Act
            var actualValue = property.GetValue(oidcDiscovery);

            // Assert
            actualValue.Should().Be(expectedValue);
        }
    }
}
