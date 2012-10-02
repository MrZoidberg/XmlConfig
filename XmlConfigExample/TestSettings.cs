using System;
using Mihmerk.XmlConfig;

namespace Mihmerk.XmlConfigExample
{
    [Serializable]
    public class ServerRegistrationInfo : IEquatable<ServerRegistrationInfo>
    {
        public string UserName
        {
            get;
            set;
        }

        public string Password
        {
            get;
            set;
        }

        private string _serverUrl;

        public string ServerUrl
        {
            get
            {
                return _serverUrl;
            }
            set
            {
                _serverUrl = value;
            }
        }

        [System.Xml.Serialization.XmlIgnore]
        public Uri ServerUri
        {
            get
            {
                return _serverUrl == null ? null : new Uri(_serverUrl);
            }
        }

        public ServerRegistrationInfo()
        {
        }

        public ServerRegistrationInfo(string userName, string password, string serverUrl)
        {
            UserName = userName;
            Password = password;
            ServerUrl = serverUrl;
        }

        public void ClearRegistration()
        {
            UserName = null;
            Password = null;
            ServerUrl = null;
        }

        #region Implementation of IEquatable<ServerRegistrationInfo>

        public bool Equals(ServerRegistrationInfo other)
        {
            if (other == null)
                return false;

            return UserName == other.UserName && Password == other.Password &&
                   _serverUrl == other._serverUrl;
        }

        #endregion
    }  

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
}
