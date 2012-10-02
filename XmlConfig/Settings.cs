using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Mihmerk.XmlConfig.Storage;

namespace Mihmerk.XmlConfig
{
    public class SettingChangedEventArgs: EventArgs
    {
        public string SettingName { get; set; }

        public SettingChangedEventArgs(String settingName)
        {
            SettingName = settingName;
        }
    }

    /// <summary>
    /// Interface for settings class
    /// <remarks> Remarks for implementing a child class:
    /// 1. Store all settings in the internal dictionary.
    /// 2. A child class need to have a default consuctor.
    /// 3. You have to manually call the FireSettingChangedEvent method
    ///    when setting is changed.
    /// </remarks>
    /// </summary>
    public abstract class Settings : ISettings
    {
        private readonly ConcurrentDictionary<String, object> _settings;
        private readonly Dictionary<string, PropertyInfo> _properties;
        internal object NotUsedSettings { get; set; }

        internal Dictionary<string, PropertyInfo> Properties
        {
            get
            {
                return _properties;
            }
        }

        internal ReadOnlyCollection<PropertyInfo> AllProperties
        {
            get
            {
                return _properties.Values.ToList().AsReadOnly();
            }
        }

        public Version SettingsVersion { get; set; }

        /// <summary>
        /// Gets the setting by key.
        /// </summary>
        /// <param name="key">The setting's key</param>
        public object this[String key]
        {
            get
            {
                return _settings[key];
            }
            set
            {
                _settings[key] = value;
            }
        }

        public bool ContainsKey(String key)
        {
            return _settings.ContainsKey(key);
        }

        public void Load()
        {
            _storage.Load(this);
            FireLoadedEvent();
        }

        public void Save()
        {
            _storage.Save(this);
            FireSavedEvent();
        }

        public Boolean IsClassOrSubClass<TSettings>()
        {
            return this is TSettings || GetType().IsSubclassOf(typeof(TSettings));
        }

        private ISettingsStorage _storage;
        public ISettingsStorage Storage
        {
            get
            {
                return _storage;
            }
            internal set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                if (_storage is IDisposable)
                    ((IDisposable)_storage).Dispose();
                _storage = value;
            }
        }

        /// <summary>
        /// Occurs when settings are loaded.
        /// </summary>
        public event EventHandler<EventArgs> Loaded;

        /// <summary>
        ///  Occurs when settings are saved.
        /// </summary>
        public event EventHandler<EventArgs> Saved;

        /// <summary>
        /// Occurs when a setting changed.
        /// </summary>
        public event EventHandler<SettingChangedEventArgs> SettingChanged;

        protected Settings()
        {
            _properties = new Dictionary<string, PropertyInfo>();
            _settings = new ConcurrentDictionary<string, object>();
            Initialize();
        }

        private void Initialize()
        {
            SettingsVersion = CurrentVersion;
            GetType().GetProperties().ToList().ForEach((InitPropertyInfo));
        }

        private void InitPropertyInfo(PropertyInfo propertyInfo)
        {
            var attr = (SettingItem)Attribute.GetCustomAttribute(propertyInfo, typeof(SettingItem));
            if (attr != null)
            {
                _properties.Add(attr.Key, propertyInfo);

                var defAttr = (DefaultValueAttribute)Attribute.GetCustomAttribute(propertyInfo, typeof(DefaultValueAttribute));
                if (defAttr != null)
                {
                    //Save values to internal dictionary
                    _settings[attr.Key] = defAttr.Value;
                }
                else
                {
                    Type targetType = propertyInfo.PropertyType;
                    _settings[attr.Key] = targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
                }
            }
        }

        private void FireLoadedEvent()
        {
            if (Loaded != null)
            {
                Loaded(this, EventArgs.Empty);
            }
        }

        private void FireSavedEvent()
        {
            if (Saved != null)
            {
                Saved(this, EventArgs.Empty);
            }
        }

        protected void FireSettingChangedEvent(String settingName)
        {
            if (SettingChanged != null)
            {
                SettingChanged(this, new SettingChangedEventArgs(settingName));
            }
        }

        public static Version CurrentVersion
        {
            get
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return version;
            }
        }
    }
}
