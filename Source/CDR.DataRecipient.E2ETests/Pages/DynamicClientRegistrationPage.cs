using Microsoft.Playwright;
using System.Threading.Tasks;

namespace CDR.DataRecipient.E2ETests.Pages
{

    internal class DynamicClientRegistrationPage
    {
        private IPage _page;
        private readonly string _dataRecipientBaseUrl;
        private readonly ILocator _lnkDcrMenuItem;
        private readonly ILocator _hedPageHeading;

        private readonly ILocator _selSelectBrand;
        private readonly ILocator _txtClientId;
        private readonly ILocator _txtSsaVersion;
        private readonly ILocator _selIndustry;
        private readonly ILocator _selIndustrySelected;
        private readonly ILocator _txtSoftwareProductId;
        private readonly ILocator _txtRedirectUris;
        private readonly ILocator _txtScope;
        private readonly ILocator _txtTokenSigningAlgo;
        private readonly ILocator _txtTokenAuthMethod;
        private readonly ILocator _txtGrantTypes;
        private readonly ILocator _txtResponseTypes;
        private readonly ILocator _txtApplicationType;

        private readonly ILocator _txtIdTokenSignedResponseAlgo;

        private readonly ILocator _txtRequestSigningAlgo;
        private readonly ILocator _txtAuthorisedSignedResponsegAlgo;
        private readonly ILocator _txtAuthorisedEncryptedResponseAlgo;
        private readonly ILocator _txtAuthorisedEncryptedResponseEnc;

        private readonly ILocator _btnRegister;
        private readonly ILocator _btnUpdate;

        private readonly ILocator _divRegistrationResponseWithHeading;
        private readonly ILocator _divRegistrationResponseJson;
        private readonly ILocator _divViewRegistrationResponse;
        private ILocator _divDiscoveryDocumentDetails;


        public DynamicClientRegistrationPage(IPage page, string dataRecipientBaseUrl)
        {
            _page = page;
            _dataRecipientBaseUrl = dataRecipientBaseUrl;

            _lnkDcrMenuItem = _page.Locator("a >> text=Dynamic Client Registration");
            _hedPageHeading = _page.Locator("h2 >> text=Dynamic Client Registration");

            _selSelectBrand = _page.Locator("id=DataHolderBrandId");
            _txtClientId = _page.Locator("id=ClientId");
            _txtSsaVersion = _page.Locator("id=SsaVersion");
            _selIndustry = _page.Locator("id=Industry");
            _selIndustrySelected = _page.Locator("//select[@id='Industry']/option[@selected='selected']");
            _txtSoftwareProductId = _page.Locator("id=SoftwareProductId");
            _txtRedirectUris = _page.Locator("id=RedirectUris");
            _txtScope = _page.Locator("id=Scope");

            _txtTokenSigningAlgo = _page.Locator("id=TokenEndpointAuthSigningAlg");
            _txtTokenAuthMethod = _page.Locator("id=TokenEndpointAuthMethod");
            _txtGrantTypes = _page.Locator("id=GrantTypes");
            _txtResponseTypes = _page.Locator("id=ResponseTypes");
            _txtApplicationType = _page.Locator("id=ApplicationType");

            _txtIdTokenSignedResponseAlgo = _page.Locator("id=IdTokenSignedResponseAlg");

            _txtRequestSigningAlgo = _page.Locator("id=RequestObjectSigningAlg");
            _txtAuthorisedSignedResponsegAlgo = _page.Locator("id=AuthorizationSignedResponseAlg");
            _txtAuthorisedEncryptedResponseAlgo = _page.Locator("id=AuthorizationEncryptedResponseAlg");
            _txtAuthorisedEncryptedResponseEnc = _page.Locator("id=AuthorizationEncryptedResponseEnc");

            _btnRegister = _page.Locator("//input[@name='register' and @value='Register']");
            _btnUpdate = _page.Locator("//input[@name='register' and @value='Update']");

            _divRegistrationResponseWithHeading = _page.Locator("//div[@id='registration']");
            _divRegistrationResponseJson = _page.Locator(@"h5:has-text(""Client Registration"") ~ div.card-footer >> pre");
            _divViewRegistrationResponse = _page.Locator("//div[@id='modal-registration' and @class='modal show']//div[@class='modal-body']");

        }

        public async Task GotoDynamicClientRegistrationPage()
        {
            await _page.GotoAsync(_dataRecipientBaseUrl);
            await _lnkDcrMenuItem.ClickAsync();
            await _hedPageHeading.TextContentAsync();
        }

        public async Task SelectDataHolderBrandId(string dataholderBrandId)
        {
            await _selSelectBrand.SelectOptionAsync(new[] { dataholderBrandId });
        }

        public async Task EnterClientId(string clientId)
        {
            await _txtClientId.FillAsync(clientId);
        }
        public async Task EnterSsaVersion(string ssaVersion)
        {
            await _txtSsaVersion.FillAsync(ssaVersion);
        }
        public async Task SelectIndustry(string industry)
        {
            await _selIndustry.SelectOptionAsync(new[] { industry });
        }
        public async Task EnterRedirectUris(string redirectUris)
        {
            await _txtRedirectUris.FillAsync(redirectUris);
        }
        public async Task EnterScope(string scope)
        {
            await _txtScope.FillAsync(scope);
        }
        public async Task EnterTokenAuthSigningAlgo(string tokenAuthSigningAlgo)
        {
            await _txtTokenSigningAlgo.FillAsync(tokenAuthSigningAlgo);
        }
        public async Task EnterTokenAuthSigningMethod(string tokenAuthSigningMethod)
        {
            await _txtTokenAuthMethod.FillAsync(tokenAuthSigningMethod);
        }
        public async Task EnterGrantTypes(string grantTypes)
        {
            await _txtGrantTypes.FillAsync(grantTypes);
        }
        public async Task EnterResponseTypes(string responseTypes)
        {
            await _txtResponseTypes.FillAsync(responseTypes);
        }
        public async Task EnterIdTokenIdTokenSignedResponseAlgo(string idTokenIdTokenSignedResponseAlgo)
        {
            await _txtIdTokenSignedResponseAlgo.FillAsync(idTokenIdTokenSignedResponseAlgo);
        }       
        public async Task EnterRequestSigningAlgo(string requestSigningAlgo)
        {
            await _txtRequestSigningAlgo.FillAsync(requestSigningAlgo);
        }
        public async Task EnterAuthorisedSignedResponsegAlgo(string authorisedSignedResponsegAlgo)
        {
            await _txtAuthorisedSignedResponsegAlgo.FillAsync(authorisedSignedResponsegAlgo);
        }
        public async Task EnterAuthorisedEncryptedResponseAlgo(string authorisedEncryptedResponseAlgo)
        {
            await _txtAuthorisedEncryptedResponseAlgo.FillAsync(authorisedEncryptedResponseAlgo);
        }
        public async Task EnterAuthorisedEncryptedResponseEnc(string authorisedEncryptedResponseEnc)
        {
            await _txtAuthorisedEncryptedResponseEnc.FillAsync(authorisedEncryptedResponseEnc);
        }

        public async Task ClickRegister()
        {
            await _btnRegister.ClickAsync();
        }
        public async Task ClickUpdate()
        {
            await _btnUpdate.ClickAsync();
        }
        public async Task ClickViewRegistration(string clientId)
        {
            await _page.Locator($"//a[@data-id='{clientId}' and text()='View']").ClickAsync();
        }
        public async Task ClickEditRegistration(string clientId)
        {
            await _page.Locator($"//tr[@id='{clientId}']//a[text()='Edit']").ClickAsync();
        }
        public async Task<string> GetClientId()
        {
            return await _txtClientId.InputValueAsync();
        }
        public async Task<string> GetSsaVersion()
        {
            return await _txtSsaVersion.InputValueAsync();
        }
        public async Task<string> GetIndustry()
        {
            return await _selIndustrySelected.TextContentAsync();
        }
        public async Task<string> GetSoftwareProductId()
        {
            return await _txtSoftwareProductId.InputValueAsync();
        }
        public async Task<string> GetRedirectUris()
        {
            return await _txtRedirectUris.InputValueAsync();
        }
        public async Task<string> GetScope()
        {
            return await _txtScope.InputValueAsync();
        }
        public async Task<string> GetTokenSigningAlgo()
        {
            return await _txtTokenSigningAlgo.InputValueAsync();
        }
        public async Task<string> GetTokenAuthMethod()
        {
            return await _txtTokenAuthMethod.InputValueAsync();
        }
        public async Task<string> GetGrantTypes()
        {
            return await _txtGrantTypes.InputValueAsync();
        }
        public async Task<string> GetResponseTypes()
        {
            return await _txtResponseTypes.InputValueAsync();
        }
        public async Task<string> GetApplicationType()
        {
            return await _txtApplicationType.InputValueAsync();
        }
        public async Task<string> GetIdTokenSignedResponseAlgo()
        {
            return await _txtIdTokenSignedResponseAlgo.InputValueAsync();
        }
        public async Task<string> GetRequestSigningAlgo()
        {
            return await _txtRequestSigningAlgo.InputValueAsync();
        }
        public async Task<string> GetAuthorisedSignedResponsegAlgo()
        {
            return await _txtAuthorisedSignedResponsegAlgo.InputValueAsync();
        }
        public async Task<string> GetAuthorisedEncryptedResponseAlgo()
        {
            return await _txtAuthorisedEncryptedResponseAlgo.InputValueAsync();
        }
        public async Task<string> GetAuthorisedEncryptedResponseEnc()
        {
            return await _txtAuthorisedEncryptedResponseEnc.InputValueAsync();
        }
        public async Task<string> GetRegistrationResponse(bool includeHeading = false)
        {
            if (includeHeading)
            {
                return await _divRegistrationResponseWithHeading.TextContentAsync();
            }
            else
            {
                return await _divRegistrationResponseJson.TextContentAsync();
            }

        }
        public async Task<string> GetViewRegistrationResponse()
        {
            return await _divViewRegistrationResponse.TextContentAsync();
        }
        public async Task<string> GetDiscoveryDocumentDetails(string textToSynchroniseWith = null)
        {
            // Workaround to wait for text to synchonise with.
            // Without synchronisation, current text content is returned instead of waiting for text (page to reload)
            if (textToSynchroniseWith == null)
            {
                _divDiscoveryDocumentDetails = _page.Locator("#discovery-document");
            }
            else
            {
                _divDiscoveryDocumentDetails = _page.Locator($"//div[@id='discovery-document']/div[contains(text(), '{textToSynchroniseWith}')]/..");
            }

            return await _divDiscoveryDocumentDetails.TextContentAsync();
        }

    }
}
