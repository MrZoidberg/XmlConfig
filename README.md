XmlConfig
===============

XmlConfig is an application configuration system written in C# and based on XML

Features
----------

XmlConfig offers the following:

 - Thread-safe configuration class
 - Easy to use and extend
 - You can store the configuration in encrypted or plain xml files
 - Support of settings changed event

 Sample
 ----------

 ``` csharp

class TestSettings: Settings
{
    [SettingItem("RegistrationInfo")]
    public ServerRegistrationInfo RegistrationInfo
    {
        get
        {
            var info = (ServerRegistrationInfo) this["RegistrationInfo"];
            if (info == null)
            {
                info = new ServerRegistrationInfo();
                this["RegistrationInfo"] = info;
            }
            return info;
        }
        set
        {
            this["RegistrationInfo"] = value;
        }
    }
}

//Set current settings 
//Null password means we don't use encryption
SettingsManager.SetCurrentSettings<TestSettings>("settings.xml", null);

//Try to load the settings from the file
try
{
    SettingsManager.Load();
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}

//Change some settings
SettingsManager.GetSettings<TestSettings>().RegistrationInfo = new ServerRegistrationInfo("user1",
                                                                                          "password1",
                                                                                          "http://google.com");
//Save settings                                                                                          
SettingsManager.Save();