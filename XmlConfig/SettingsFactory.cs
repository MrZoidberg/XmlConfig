using System;
using System.Reflection;
using Mihmerk.XmlConfig.Storage;

namespace Mihmerk.XmlConfig
{
    internal static class SettingsFactory
    {
        public static T CreateSettings<T>(ISettingsStorage storage) where T : Settings
        {
            if (storage == null)
                throw new ArgumentNullException("storage");

            var constr = typeof (T).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, Type.DefaultBinder, new Type[] {},
                null);

            if (constr != null)
            {
                T settings = (T) constr.Invoke(new object[0]);
                settings.Storage = storage;
                return settings;
            }

            return null;
        }
    }
}
