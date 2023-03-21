using Microsoft.IdentityModel.Tokens;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using static System.Formats.Asn1.AsnWriter;

namespace CDR.DataRecipient.E2ETests.Pages
{
    internal class ParPage
    {

        private IPage _page;
        private readonly ILocator _lnkParMenuItem;
        private readonly ILocator _hedPageHeading;
        private readonly ILocator _selSelectRegistration;
        private readonly ILocator _selSelectArrangementId;
        private readonly ILocator _txtSharingDuration;
        private readonly ILocator _txtScope;
        private readonly ILocator _txtResponseType;
        private readonly ILocator _txtResponseMode;
        private readonly ILocator _btnInitiatePar;
        private readonly ILocator _lnkRequestUri;
        private readonly ILocator _chkUsePkce;
        private readonly ILocator _divErrorMessage;
        private readonly ILocator _lblRequestUri;

        public ParPage(IPage page)
        {
            _page = page;
            _lnkParMenuItem = _page.Locator("a >> text=PAR");
            _hedPageHeading = _page.Locator("h2 >> text=Pushed Authorisation Request (PAR)");
            _selSelectRegistration = _page.Locator("select[name=\"RegistrationId\"]");
            _selSelectArrangementId = _page.Locator("select[name=\"CdrArrangementId\"]");
            _txtSharingDuration = _page.Locator("input[name =\"SharingDuration\"]");
            _txtScope = _page.Locator("input[name=\"Scope\"]");
            _chkUsePkce = _page.Locator("#UsePkce");
            _txtResponseType = _page.Locator("input[name=\"ResponseType\"]");
            _txtResponseMode = _page.Locator("input[name=\"ResponseMode\"]");
            _btnInitiatePar = _page.Locator("div.form >> text=Initiate PAR");
            _lnkRequestUri = _page.Locator("p.results > a");
            _divErrorMessage = _page.Locator(".card-footer");
            _lblRequestUri = _page.Locator("dd:has-text(\"urn:\")");

        }

        public async Task GotoPar()
        {
            await _lnkParMenuItem.ClickAsync();
            await _hedPageHeading.TextContentAsync();
        }

        public async Task CompleteParFormOld(string dhClientId, string dhBrandId, string cdrArrangement, string sharingDuration)
        {
            await _selSelectRegistration.SelectOptionAsync(new[] { $"{dhClientId}|||{dhBrandId}" });
            await _selSelectRegistration.ClickAsync(); // there is JS that runs on the click event, so simulate click here
            await Task.Delay(2000);
            await _selSelectArrangementId.SelectOptionAsync(new[] { cdrArrangement });
            await _txtSharingDuration.FillAsync(sharingDuration);
            await _btnInitiatePar.ClickAsync();
            await _lnkRequestUri.ClickAsync();

        }

        public async Task CompleteParForm(
            string dhClientId,
            string dhBrandId,
            string scope = null,
            string cdrArrangement = null,
            string responseType = "code id_token",
            string responseMode = "fragment",            
            string sharingDuration = "",
            bool usePkce = true)
        {
            await _selSelectRegistration.SelectOptionAsync(new[] { $"{dhClientId}|||{dhBrandId}" });
            await _selSelectRegistration.ClickAsync(); // there is JS that runs on the click event, so simulate click here
            await Task.Delay(2000);
            if (cdrArrangement != null)
            {
                await _selSelectArrangementId.SelectOptionAsync(new[] { cdrArrangement });
            }
            
            await _txtSharingDuration.FillAsync(sharingDuration);

            if (scope != null)
            {
                await _txtScope.FillAsync(scope);
            }
            if (usePkce)
            {
                await _chkUsePkce.CheckAsync();
            }
            else
            {
                await _chkUsePkce.UncheckAsync();
            }

            await _txtResponseType.FillAsync(responseType);
            await _txtResponseMode.FillAsync(responseMode);

        }

        public async Task ClickInitiatePar()
        {
            await _btnInitiatePar.ClickAsync();
        }

        public async Task ClickAuthorizeUrl()
        {
            await _lnkRequestUri.ClickAsync();
        }
        public async Task<string> GetAuthorizeUrl()
        {
            return await _lnkRequestUri.InnerTextAsync();
        }
        public async Task<string> GetRequestUri()
        {
            return await _lblRequestUri.InnerTextAsync();
        }
        public async Task<string> GetErrorMessage()
        {
            return await _divErrorMessage.InnerTextAsync();
        }

        public async Task<string> GetResponseType()
        {
            return await _txtResponseType.InputValueAsync();
        }

        public async Task<string> GetResponseMode()
        {
            return await _txtResponseMode.InputValueAsync();
        }
        public async Task<bool> ErrorExists(string errorToCheckFor)
        {
            try
            {
                var element = await _page.WaitForSelectorAsync($"//*[contains(.,\"{errorToCheckFor}\")]");
                return await element.IsVisibleAsync();
            }
            catch (TimeoutException) { }
            {
                return false;
            }
        }

    }
}
