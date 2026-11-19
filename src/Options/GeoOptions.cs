namespace NerdDinner.Options
{
    /// <summary>
    /// Settings for third-party mapping / geolocation services (bound from the "Geo" configuration section).
    /// Provide real keys through user-secrets or environment variables, never in source control.
    /// </summary>
    public class GeoOptions
    {
        public const string SectionName = "Geo";

        public string BingMapsKey { get; set; } = string.Empty;
        public string IpInfoDbKey { get; set; } = string.Empty;
        public string GeoNamesUserName { get; set; } = string.Empty;
    }
}
