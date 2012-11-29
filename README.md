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
```

License
------

Copyright (c) 2012 by Mikhail Merkulov

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.