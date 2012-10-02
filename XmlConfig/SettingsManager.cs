using System;
using System.Xml;
using Mihmerk.XmlConfig.Storage;

namespace Mihmerk.XmlConfig
{
    public static class SettingsManager
    {
        private static ISettings _settings;
        private static readonly object _lockObject = new object();

        /// <summary>
        /// Gets the current settings.
        /// </summary>
        /// <value>The settings.</value>
        public static ISettings Settings
        {
            get
            {
                lock (_lockObject)
                {
                    return _settings;
                }
            }
        }


        /// <summary>
        /// Gets the current settings.
        /// </summary>
        /// <value>The settings.</value>
        public static T GetSettings<T>() where T : ISettings
        {
            lock (_lockObject)
            {
                return (T)_settings;
            }
        }

        /// <summary>
        /// Sets the current settings and stores them in the encrypted file.
        /// </summary>
        /// <typeparam name="T">Type of settings. Should be derived from DestinationSettings</typeparam>
        /// <param name="file">The file name.</param>
        /// <param name="passPhrase">The pass phrase.</param>
        /// <param name="settingsVersion">The settings version.</param>
        /// <param name="needUpdateAction">The need update action.</param>
        public static void SetCurrentSettings<T>(String file, String passPhrase, Version settingsVersion = null, Func<WrongXmlSettingsFileVersionArgs, XmlDocument> needUpdateAction = null) where T : Settings
        {
            if (String.IsNullOrEmpty(file))
                throw new ArgumentException("'file' cannot be null or empty");

            ISettingsStorage storage;
            if (String.IsNullOrEmpty(passPhrase))
            {
                storage = new PlainXmlSettingsStorage(file);
            }
            else
            {
                storage = new EncryptedXmlSettingsStorage(file, passPhrase);
            }

            if (needUpdateAction != null)
            {
                storage.SettingsNeedUpdateAction =
                    args => { return needUpdateAction((WrongXmlSettingsFileVersionArgs)args); };
            }
            _settings = SettingsFactory.CreateSettings<T>(storage);
            if (settingsVersion != null)
            {
                _settings.SettingsVersion = settingsVersion;
            }
        }

        /// <summary>
        /// Loads current settings.
        /// </summary>
        public static void Load()
        {
            lock (_lockObject)
            {
                if (_settings == null)
                    throw new InvalidOperationException("Can not load settings. Settings object is not set");
                _settings.Load();
            }
        }

        /// <summary>
        /// Saves current settings.
        /// </summary>
        public static void Save()
        {
            lock (_lockObject)
            {
                if (_settings == null)
                    throw new InvalidOperationException("Can not save settings. Settings object is not set");

                _settings.Save();
            }
        }
    }
}
