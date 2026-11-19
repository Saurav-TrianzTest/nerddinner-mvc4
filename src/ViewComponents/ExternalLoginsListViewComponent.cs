using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace NerdDinner.ViewComponents
{
    /// <summary>
    /// Replaces the [ChildActionOnly] AccountController.ExternalLoginsList action.
    /// </summary>
    public class ExternalLoginsListViewComponent : ViewComponent
    {
        private readonly SignInManager<IdentityUser> _signInManager;

        public ExternalLoginsListViewComponent(SignInManager<IdentityUser> signInManager)
        {
            _signInManager = signInManager;
        }

        public async Task<IViewComponentResult> InvokeAsync(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            var schemes = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            return View(schemes);
        }
    }
}
