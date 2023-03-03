using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDR.DataRecipient.E2ETests.Pages
{
    internal class ConsentAndAuthorisationPages
    {
        private readonly IPage _page;
        private readonly ILocator _txtCustomerId;        
        private readonly ILocator _btnContinue;
        private readonly ILocator _txtOneTimePassword;
        private readonly ILocator _btnAuthorise;
        private readonly ILocator _btnCancel;

        public ConsentAndAuthorisationPages(IPage page)
        {
            _page = page;
            _txtCustomerId = _page.Locator("id=mui-1");            
            _btnContinue = _page.Locator("button:has-text(\"Continue\")");
            _txtOneTimePassword = _page.Locator("id=mui-2");
            _btnAuthorise = _page.Locator("text=Authorise");
            _btnCancel = _page.Locator("text=Cancel");

        }

        public async Task EnterCustomerId(string customerId)
        {
            await _txtCustomerId.WaitForAsync();
            await Task.Delay(1000); //require for JS delayed defaulting of field. It can sometimes overwrite the entered value.
            await _txtCustomerId.FillAsync("");
            await _txtCustomerId.FillAsync(customerId);
        }

        public async Task ClickContinue()
        {
            await _btnContinue.ClickAsync();
        }
        public async Task ClickCancel()
        {
            await _btnCancel.ClickAsync();
        }

        public async Task EnterOtp(string otp)
        {
            await _txtOneTimePassword.FillAsync(otp);
        }

        public async Task SelectAccounts(string accountsToSelectCsv)
        {
            string[] accountsToSelectArray = accountsToSelectCsv?.Split(",");

            foreach (string accountToSelect in accountsToSelectArray)
            {
                await SelectAccount(accountToSelect.Trim());
            }

        }

        public async Task SelectAccount(string accountToSelect)
        {
            await _page.Locator($"//input[@aria-labelledby='account-{accountToSelect}']").CheckAsync();
        }

        public async Task ClickAuthorise()
        {
            await _btnAuthorise.ClickAsync();
        }

    }
}
