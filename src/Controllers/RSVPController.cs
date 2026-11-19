using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NerdDinner.Models;

namespace NerdDinner.Controllers
{
    public class RSVPController : Controller
    {
        private readonly NerdDinnerContext _db;
        private readonly ILogger<RSVPController> _logger;

        public RSVPController(NerdDinnerContext db, ILogger<RSVPController> logger)
        {
            _db = db;
            _logger = logger;
        }

        //
        // HTTP POST: /RSVP/Register/1  (state-changing, so POST + antiforgery)
        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(int id)
        {
            if (!await RegisterForDinnerAsync(id))
            {
                return NotFound();
            }
            return RedirectToAction("Details", "Dinners", new { id });
        }

        //
        // AJAX: /RSVP/RegisterAjax/1
        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterAjax(int id)
        {
            if (!await RegisterForDinnerAsync(id))
            {
                return NotFound();
            }
            return Content("Thanks - we'll see you there!");
        }

        private async Task<bool> RegisterForDinnerAsync(int id)
        {
            Dinner dinner = await _db.Dinners.Include(d => d.RSVPs).FirstOrDefaultAsync(d => d.DinnerID == id);
            if (dinner == null)
            {
                return false;
            }

            if (!dinner.IsUserRegistered(User.Identity.Name))
            {
                dinner.RSVPs.Add(new RSVP { AttendeeName = User.Identity.Name });
                await _db.SaveChangesAsync();
                _logger.LogInformation("{User} registered for dinner {DinnerId}.", User.Identity.Name, id);
            }
            return true;
        }

        //
        // AJAX: /RSVP/CancelAjax/1
        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAjax(int id)
        {
            Dinner dinner = await _db.Dinners.Include(d => d.RSVPs).FirstOrDefaultAsync(d => d.DinnerID == id);
            if (dinner == null)
            {
                return NotFound();
            }

            RSVP rsvp = dinner.RSVPs.SingleOrDefault(r => User.Identity.Name == r.AttendeeName);
            if (rsvp != null)
            {
                _db.RSVPs.Remove(rsvp);
                await _db.SaveChangesAsync();
            }

            return Content("Sorry you can't make it!");
        }
    }
}
