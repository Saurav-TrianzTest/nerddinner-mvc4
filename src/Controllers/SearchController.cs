using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NerdDinner.Models;
using NerdDinner.ModelBinders;
using NerdDinner.Services;
using NetTopologySuite.Geometries;

namespace NerdDinner.Controllers
{
    public class JsonDinner
    {
        public int DinnerID { get; set; }
        public DateTime EventDate { get; set; }
        public string Title { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Description { get; set; }
        public int RSVPCount { get; set; }
        public string Url { get; set; }
    }

    /// <summary>
    /// Preserves the legacy Web API contract consumed by NerdDinner.js:
    ///   GET  api/Search?latitude=1.0&amp;longitude=1.0
    ///   GET  api/Search?location=Seattle
    ///   POST api/Search?limit=10
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private const double SearchRadiusMeters = 2000;

        private readonly NerdDinnerContext _db;
        private readonly IGeolocationService _geolocation;
        private readonly ILogger<SearchController> _logger;

        public SearchController(NerdDinnerContext db, IGeolocationService geolocation, ILogger<SearchController> logger)
        {
            _db = db;
            _geolocation = geolocation;
            _logger = logger;
        }

        // GET api/Search?latitude=1.0&longitude=1.0
        // GET api/Search?location=30901
        // GET api/Search?location=Seattle
        [HttpGet]
        public async Task<ActionResult<IEnumerable<JsonDinner>>> Get(
            [FromQuery] double? latitude, [FromQuery] double? longitude, [FromQuery] string location,
            CancellationToken cancellationToken)
        {
            if (latitude.HasValue && longitude.HasValue)
            {
                return Ok(await FindByLocationAsync(latitude.Value, longitude.Value, cancellationToken));
            }

            if (string.IsNullOrWhiteSpace(location))
            {
                return Ok(Array.Empty<JsonDinner>());
            }

            LatLong foundLocation = await _geolocation.PlaceOrZipToLatLongAsync(location, cancellationToken);
            if (foundLocation == null)
            {
                return Ok(Array.Empty<JsonDinner>());
            }

            var results = await FindByLocationAsync(foundLocation.Lat, foundLocation.Long, cancellationToken);
            return Ok(results.OrderByDescending(p => p.EventDate).ToList());
        }

        // POST api/Search?limit=10
        // The endpoint is read-only, so it is excluded from antiforgery validation.
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult<IEnumerable<JsonDinner>>> GetMostPopularDinners([FromQuery] int limit, CancellationToken cancellationToken)
        {
            limit = Math.Clamp(limit, 1, 100);

            var now = DateTime.Now;
            var mostPopularDinners = await _db.Dinners
                .AsNoTracking()
                .Include(d => d.RSVPs)
                .Where(d => d.EventDate >= now)
                .OrderByDescending(d => d.RSVPs.Count)
                .Take(limit)
                .ToListAsync(cancellationToken);

            return Ok(mostPopularDinners.Select(JsonDinnerFromDinner).ToList());
        }

        private async Task<List<JsonDinner>> FindByLocationAsync(double latitude, double longitude, CancellationToken cancellationToken)
        {
            //4326 format puts LONGITUDE first then LATITUDE
            Point sourcePoint = PointModelBinder.GeometryFactory.CreatePoint(new Coordinate(longitude, latitude));

            var results = await _db.Dinners
                .AsNoTracking()
                .Include(d => d.RSVPs)
                .Where(d => d.Location != null && d.Location.Distance(sourcePoint) < SearchRadiusMeters)
                .OrderBy(d => d.Location.Distance(sourcePoint))
                .ToListAsync(cancellationToken);

            return results.Select(JsonDinnerFromDinner).ToList();
        }

        private static JsonDinner JsonDinnerFromDinner(Dinner dinner)
        {
            return new JsonDinner
            {
                DinnerID = dinner.DinnerID,
                EventDate = dinner.EventDate,
                Latitude = dinner.Location?.Y ?? 0,
                Longitude = dinner.Location?.X ?? 0,
                Title = dinner.Title,
                Description = dinner.Description,
                RSVPCount = dinner.RSVPs.Count,
                Url = dinner.DinnerID.ToString()
            };
        }
    }
}
