using System;
using Mihmerk.XmlConfig;

namespace Mihmerk.XmlConfigExample
{
    class Program
    {
        static void Main(string[] args)
        {
            SettingsManager.SetCurrentSettings<TestSettings>("settings.xml", null);
            try
            {
                SettingsManager.Load();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            SettingsManager.GetSettings<TestSettings>().RegistrationInfo = new ServerRegistrationInfo("user1",
                                                                                                      "password1",
                                                                                                      "http://google.com");
            SettingsManager.Save();

        }
    }
}
