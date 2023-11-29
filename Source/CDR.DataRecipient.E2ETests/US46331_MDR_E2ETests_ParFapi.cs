using Azure;
using CDR.DataRecipient.E2ETests.Pages;
using CDR.DataRecipient.SDK.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Data.SqlClient;
using Microsoft.Playwright;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static System.Formats.Asn1.AsnWriter;

#nullable enable

namespace CDR.DataRecipient.E2ETests
{
    public class US46331_MDR_E2ETests_ParFapi : BaseTest, IClassFixture<TestFixture>
    {

        public const string DH_DEFAULT_PAR_SCOPE = "openid profile common:customer.basic:read bank:accounts.basic:read bank:transactions:read cdr:registration";

        [Theory]
        [InlineData("Missing Response Type", DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, DH_DEFAULT_PAR_SCOPE, true, "", "fragment", "ERR-GEN-008: response_type is missing")]
        [InlineData("Invalid Response Type", DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, DH_DEFAULT_PAR_SCOPE, true, "foo", "fragment", "response_type is not supported")]
        [InlineData("Missing Response Mode for Code Flow", DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, DH_DEFAULT_PAR_SCOPE, null, "code", "", "ERR-GEN-013: response_mode is not supported")]
        [InlineData("Invalid Response Mode for Code Flow", DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, DH_DEFAULT_PAR_SCOPE, null, "code", "fragment", "Invalid response_mode for response_type")]
        [InlineData("Invalid Response Mode for Hybrid Flow", DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, DH_DEFAULT_PAR_SCOPE, null, "code id_token", "jwt", "Invalid response_mode for response_type")]
        [InlineData("Missing Scope", DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, "", null, "code id_token", "fragment", "scope is missing")]
        [InlineData("Invalid Scope", DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, "foo", null, "code id_token", "fragment", "openid scope is missing")]
        [InlineData("Valid Response Mode for Code Flow", DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID, DH_DEFAULT_PAR_SCOPE, null, "code", "jwt", "")]
        public async Task AC04_AC0_AC06_AC07_InvalidParRequests(string scenarioName, string dhBrandId, string drBrandId, string drSoftwareProductId, string dhScope, bool? useDefaultResponseTypeForDCR, string responseType, string responseMode, string expectedError)
        {
            try
            {
                string testName = $"{nameof(US46331_MDR_E2ETests_ParFapi)} - {nameof(AC04_AC0_AC06_AC07_InvalidParRequests)} - {scenarioName}";
                string? dhClientId = null;
                await ArrangeAsync(testName, async (page) =>
                {

                    var responseTypeForRegCreation =useDefaultResponseTypeForDCR !=null && useDefaultResponseTypeForDCR == true ? "code id_token" : responseType;

                    dhClientId = await ClientRegistration_Create(page, dhBrandId, drBrandId, drSoftwareProductId, responseTypes: responseTypeForRegCreation)
                        ?? throw new NullReferenceException(nameof(dhClientId));
                });

                await TestAsync(testName, async (page) =>
                {
                    await page.GotoAsync(WEB_URL);
                    ParPage parPage = new ParPage(page);
                    await parPage.GotoPar();
                    await parPage.CompleteParForm(dhClientId, dhBrandId, dhScope, responseType: responseType, responseMode: responseMode, sharingDuration: SHARING_DURATION);
                    await parPage.ClickInitiatePar();

                    string actualError = await parPage.GetErrorMessage();
                    if (String.IsNullOrEmpty(expectedError))
                    {
                        actualError.Should().BeEmpty(because: $"{scenarioName} error string should be empty.");
                    }
                    else
                    {
                        actualError.Should().Contain(expectedError, because: scenarioName);
                    }

                });
            }
            finally
            {
                await CleanupAsync(async (page) =>
                {
                    try { await ClientRegistration_Delete(page); } catch { };
                });
            }
        }

        [Fact]
        public async Task AC01_ParDefaultValues()
        {

            try
            {
                string testName = $"{nameof(US46331_MDR_E2ETests_ParFapi)} - {nameof(AC01_ParDefaultValues)}";
                string? dhClientId = null;
                await ArrangeAsync(testName, async (page) =>
                {
                    dhClientId = await ClientRegistration_Create(page, DH_BRANDID, DR_BRANDID, DR_SOFTWAREPRODUCTID)
                        ?? throw new NullReferenceException(nameof(dhClientId));
                });

                await TestAsync(testName, async (page) =>
                {
                    await page.GotoAsync(WEB_URL);
                    ParPage parPage = new ParPage(page);
                    await parPage.GotoPar();

                    string actualResponseType = await parPage.GetResponseType();
                    string actualResponseMode = await parPage.GetResponseMode();

                    // Assert
                    using (new AssertionScope())
                    {
                        actualResponseType.Should().Be("code id_token");
                        actualResponseMode.Should().Be("fragment");
                    }

                });
            }
            finally
            {
                await CleanupAsync(async (page) =>
                {
                    try { await ClientRegistration_Delete(page); } catch { };
                });
            }

        }
    }
}