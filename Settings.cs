using System.IO;
using System.Xml.Serialization;

namespace Enlisted
{
    public class Settings
    {
        public int DailyWage { get; set; } = 10;

        private static Settings _instance;
        public static Settings Instance => _instance ??= Load();

        private static Settings Load()
        {
            var moduleDir = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(typeof(Settings).Assembly.Location) ?? "",
                "..", ".."); // goes from bin\Win64_Shipping_Client\ back to Modules\Enlisted\
            var path = Path.GetFullPath(Path.Combine(moduleDir, "settings.xml"));

            try
            {
                if (File.Exists(path))
                {
                    var ser = new XmlSerializer(typeof(Settings));
                    using var fs = File.OpenRead(path);
                    return (Settings)ser.Deserialize(fs);
                }
            }
            catch { /* fall back to defaults */ }

            return new Settings();
        }
    }
}
