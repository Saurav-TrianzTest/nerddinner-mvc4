using System.Globalization;
using System.Xml.Linq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NerdDinner.Helpers;
using NerdDinner.Options;

namespace NerdDinner.Services
{
    public interface IGeolocationService
    {
        Task<LatLong> PlaceOrZipToLatLongAsync(string placeOrZip, CancellationToken cancellationToken = default);
        Task<LocationInfo> HostIpToPlaceNameAsync(string ip, CancellationToken cancellationToken = default);
    }

    public class GeolocationService : IGeolocationService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly GeoOptions _options;
        private readonly ILogger<GeolocationService> _logger;

        public GeolocationService(HttpClient httpClient, IMemoryCache cache, IOptions<GeoOptions> options, ILogger<GeolocationService> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<LatLong> PlaceOrZipToLatLongAsync(string placeOrZip, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(placeOrZip))
            {
                return null;
            }

            placeOrZip = placeOrZip.Trim();
            string cacheKey = "geonames:" + placeOrZip.ToLowerInvariant();

            if (!_cache.TryGetValue(cacheKey, out XDocument result))
            {
                string url = string.Format(CultureInfo.InvariantCulture,
                    "https://secure.geonames.org/postalCodeSearch?{0}={1}&maxRows=1&style=SHORT&username={2}",
                    placeOrZip.IsNumeric() ? "postalcode" : "placename",
                    Uri.EscapeDataString(placeOrZip),
                    Uri.EscapeDataString(_options.GeoNamesUserName ?? string.Empty));

                try
                {
                    await using var stream = await _httpClient.GetStreamAsync(url, cancellationToken);
                    result = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
                }
                catch (Exception ex) when (ex is HttpRequestException or System.Xml.XmlException)
                {
                    _logger.LogWarning(ex, "Geocoding lookup failed for '{Location}'.", placeOrZip);
                    return null;
                }

                _cache.Set(cacheKey, result, new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromDays(1) });
            }

            var code = result.Descendants("code").FirstOrDefault();
            if (code == null)
            {
                return null;
            }

            return new LatLong
            {
                Lat = (float)code.Element("lat"),
                Long = (float)code.Element("lng")
            };
        }

        public async Task<LocationInfo> HostIpToPlaceNameAsync(string ip, CancellationToken cancellationToken = default)
        {
            if (ip == "127.0.0.1")
            {
                ip = "71.117.141.83";
            }

            string url = string.Format(CultureInfo.InvariantCulture,
                "https://api.ipinfodb.com/v3/ip-city/?ip={0}&key={1}&format=xml",
                Uri.EscapeDataString(ip ?? string.Empty),
                Uri.EscapeDataString(_options.IpInfoDbKey ?? string.Empty));

            try
            {
                await using var stream = await _httpClient.GetStreamAsync(url, cancellationToken);
                var result = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);

                return (from x in result.Descendants("Response")
                        select new LocationInfo
                        {
                            City = (string)x.Element("cityName"),
                            RegionName = (string)x.Element("regionName"),
                            Country = (string)x.Element("countryName"),
                            ZipPostalCode = (string)x.Element("zipCode"),
                            Position = new LatLong
                            {
                                Lat = (float?)x.Element("latitude") ?? 0f,
                                Long = (float?)x.Element("longitude") ?? 0f
                            }
                        }).FirstOrDefault();
            }
            catch (Exception ex) when (ex is HttpRequestException or System.Xml.XmlException)
            {
                _logger.LogWarning(ex, "IP geolocation lookup failed.");
                return null;
            }
        }
    }

    public class LatLong
    {
        public float Lat { get; set; }
        public float Long { get; set; }
    }

    public class LocationInfo
    {
        public string Country { get; set; }
        public string RegionName { get; set; }
        public string City { get; set; }
        public string ZipPostalCode { get; set; }
        public LatLong Position { get; set; }
    }
}
