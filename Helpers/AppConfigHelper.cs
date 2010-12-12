using System.Configuration;

namespace ProverbTeleprompter.Helpers
{
    public class AppConfigHelper
    {

        #region App.Config helpers

        public static void SetUserSetting(string key, object value)
        {
            Properties.Settings.Default[key] = value;
        }

        public static void SetAppSetting(string key, string value)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            SetAppSetting(key, value, config);

        }

        public static void SetAppSetting(string key, string value, Configuration config)
        {
            try
            {
                config.AppSettings.Settings[key].Value = value;
            }
            catch
            {
                config.AppSettings.Settings.Add(key, value);
                config.AppSettings.Settings[key].Value = value;
            }

            config.Save(ConfigurationSaveMode.Modified, false);
            ConfigurationManager.RefreshSection("appSettings");
        }


        #endregion
    }
}
