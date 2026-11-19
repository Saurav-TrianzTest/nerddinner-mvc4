using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NerdDinner.Models;

namespace NerdDinner.ViewComponents
{
    /// <summary>
    /// Replaces the [ChildActionOnly] AccountController.RemoveExternalLogins action.
    /// </summary>
    public class RemoveExternalLoginsViewComponent : ViewComponent
    {
        private readonly UserManager<IdentityUser> _userManager;

        public RemoveExternalLoginsViewComponent(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var externalLogins = new List<ExternalLogin>();
            bool hasPassword = false;

            if (user != null)
            {
                var logins = await _userManager.GetLoginsAsync(user);
                externalLogins = logins.Select(l => new ExternalLogin
                {
                    Provider = l.LoginProvider,
                    ProviderDisplayName = l.ProviderDisplayName ?? l.LoginProvider,
                    ProviderUserId = l.ProviderKey
                }).ToList();
                hasPassword = await _userManager.HasPasswordAsync(user);
            }

            ViewBag.ShowRemoveButton = externalLogins.Count > 1 || hasPassword;
            return View(externalLogins);
        }
    }
}
