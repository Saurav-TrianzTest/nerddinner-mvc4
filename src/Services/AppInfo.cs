using System.Reflection;

namespace NerdDinner.Services
{
    public interface IAppInfo
    {
        string Version { get; }
    }

    /// <summary>
    /// Replaces Application["Version"] from Global.asax.
    /// </summary>
    public class AppInfo : IAppInfo
    {
        public AppInfo()
        {
            var version = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(1, 0);
            Version = $"{version.Major}.{version.Minor}";
        }

        public string Version { get; }
    }
}
