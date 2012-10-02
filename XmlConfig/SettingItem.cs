using System;

namespace Mihmerk.XmlConfig
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class SettingItem : Attribute
    {
        private readonly String _key;
        private readonly String _serializerMethodName;
        private readonly String _deserializerMethodName;

        public String Key
        {
            get { return _key; }
        }

        public String SerializerMethodName
        {
            get { return _serializerMethodName; }
        }

        public String DeserializerMethodName
        {
            get { return _deserializerMethodName; }
        }

        public SettingItem(String key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            if (key.Length == 0)
                throw new ArgumentException("The provided 'key' argument must not be empty");

            _key = key;
        }

        public SettingItem(String key, String serializerMethodName, String deserializerMethodName)
            : this(key)
        {
            _serializerMethodName = serializerMethodName;
            _deserializerMethodName = deserializerMethodName;
        }
    }
}
