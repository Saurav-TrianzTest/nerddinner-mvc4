using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NerdDinner.Models;
using X.PagedList;
using X.PagedList.EF;

namespace NerdDinner.Controllers
{
    public class DinnersController : Controller
    {
        private const int PageSize = 25;

        private readonly NerdDinnerContext _db;
        private readonly ILogger<DinnersController> _logger;

        public DinnersController(NerdDinnerContext db, ILogger<DinnersController> logger)
        {
            _db = db;
            _logger = logger;
        }

        //
        // GET: /Dinners/
        public async Task<IActionResult> Index(int? page)
        {
            int pageIndex = page.HasValue && page.Value > 0 ? page.Value : 1;

            var now = DateTime.Now;
            var dinners = _db.Dinners
                .AsNoTracking()
                .Where(d => d.EventDate >= now)
                .OrderBy(d => d.EventDate);

            return View(await dinners.ToPagedListAsync(pageIndex, PageSize));
        }

        //
        // GET: /Dinners/Details/5
        public async Task<IActionResult> Details(int id)
        {
            Dinner dinner = await _db.Dinners.Include(d => d.RSVPs).AsNoTracking().FirstOrDefaultAsync(d => d.DinnerID == id);
            if (dinner == null)
            {
                return NotFound();
            }
            return View(dinner);
        }

        //
        // GET: /Dinners/Create
        [Authorize]
        public IActionResult Create()
        {
            var dinner = new Dinner()
            {
                EventDate = DateTime.Now.AddDays(7),
                HostedBy = User.Identity.Name
            };

            return View(dinner);
        }

        //
        // POST: /Dinners/Create
        [HttpPost, Authorize, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,EventDate,Description,ContactPhone,Address,Country,Location")] Dinner dinner)
        {
            if (ModelState.IsValid)
            {
                dinner.HostedBy = User.Identity.Name;

                dinner.RSVPs = new List<RSVP>
                {
                    new RSVP { AttendeeName = User.Identity.Name }
                };

                _db.Dinners.Add(dinner);
                await _db.SaveChangesAsync();
                _logger.LogInformation("Dinner {DinnerId} created by {User}.", dinner.DinnerID, User.Identity.Name);
                return RedirectToAction("Index");
            }

            dinner.HostedBy = User.Identity.Name;
            return View(dinner);
        }

        //
        // GET: /Dinners/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            Dinner dinner = await _db.Dinners.AsNoTracking().FirstOrDefaultAsync(d => d.DinnerID == id);
            if (dinner == null)
            {
                return NotFound();
            }
            if (!dinner.IsHostedBy(User.Identity.Name))
            {
                return View("InvalidOwner");
            }
            return View(dinner);
        }

        //
        // POST: /Dinners/Edit/5
        [HttpPost, ActionName("Edit"), Authorize, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int id)
        {
            // Load the stored entity so ownership is checked against the stored host, not posted data.
            Dinner dinner = await _db.Dinners.FirstOrDefaultAsync(d => d.DinnerID == id);
            if (dinner == null)
            {
                return NotFound();
            }
            if (!dinner.IsHostedBy(User.Identity.Name))
            {
                return View("InvalidOwner");
            }

            // Only whitelisted properties can be updated (prevents over-posting).
            bool updated = await TryUpdateModelAsync(dinner, string.Empty,
                d => d.Title, d => d.EventDate, d => d.Description, d => d.ContactPhone,
                d => d.Address, d => d.Country, d => d.Location);

            if (updated && ModelState.IsValid)
            {
                try
                {
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _db.Dinners.AnyAsync(d => d.DinnerID == id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction("Index");
            }
            return View(dinner);
        }

        //
        // GET: /Dinners/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            Dinner dinner = await _db.Dinners.AsNoTracking().FirstOrDefaultAsync(d => d.DinnerID == id);
            if (dinner == null)
            {
                return NotFound();
            }
            if (!dinner.IsHostedBy(User.Identity.Name))
            {
                return View("InvalidOwner");
            }
            return View(dinner);
        }

        //
        // POST: /Dinners/Delete/5
        [HttpPost, ActionName("Delete"), Authorize, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            Dinner dinner = await _db.Dinners.FindAsync(id);
            if (dinner == null)
            {
                return NotFound();
            }

            if (!dinner.IsHostedBy(User.Identity.Name))
            {
                return View("InvalidOwner");
            }

            _db.Dinners.Remove(dinner);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Dinner {DinnerId} deleted by {User}.", id, User.Identity.Name);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> WebSlicePopular()
        {
            ViewData["Title"] = "Popular Nerd Dinners";
            var now = DateTime.Now;
            var model = await _db.Dinners
                .AsNoTracking()
                .Where(d => d.EventDate >= now)
                .OrderByDescending(d => d.RSVPs.Count)
                .Take(5)
                .ToListAsync();
            return View("WebSlice", model);
        }

        public async Task<IActionResult> WebSliceUpcoming()
        {
            ViewData["Title"] = "Upcoming Nerd Dinners";
            DateTime d = DateTime.Now.AddMonths(2);
            var model = await _db.Dinners
                .AsNoTracking()
                .Where(x => x.EventDate < d)
                .OrderByDescending(x => x.EventDate)
                .Take(5)
                .ToListAsync();
            return View("WebSlice", model);
        }
    }
}
