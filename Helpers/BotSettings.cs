using System.Xml.Serialization;
using System.IO;
using System.Windows.Forms;
using System.ComponentModel;

using YetAnotherRelogger.Helpers.Bot;

namespace YetAnotherRelogger.Helpers
{
    #region BotSettings
    public sealed class BotSettings
    {
        #region singleton
        static readonly BotSettings instance = new BotSettings();

        static BotSettings()
        {
        }

        BotSettings()
        {
            Bots = new BindingList<BotClass>();
            settingsdirectory = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Settings");
        }

        public static BotSettings Instance
        {
            get
            {
                return instance;
            }
        }
        #endregion

        public BindingList<BotClass> Bots;
        private string settingsdirectory;
        public static string SettingsDirectory
        {
            get
            {
                return instance.settingsdirectory;
            }
        }
        public void Save()
        {
            var xml = new XmlSerializer(Bots.GetType());

            if (!Directory.Exists(SettingsDirectory))
                Directory.CreateDirectory(SettingsDirectory);


            using (var writer = new StreamWriter(Path.Combine(SettingsDirectory, "Bots.xml")))
            {

                xml.Serialize(writer, Bots);
            }
        }

        public void Load()
        {
            var xml = new XmlSerializer(Bots.GetType());

            if (!File.Exists(Path.Combine(SettingsDirectory, "Bots.xml")))
                return;

            using (var reader = new StreamReader(Path.Combine(SettingsDirectory, "Bots.xml")))
            {
                Bots = xml.Deserialize(reader) as BindingList<BotClass>;
            }
        }
    }
    #endregion
}
