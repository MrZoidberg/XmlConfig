using System;

namespace Mihmerk.XmlConfig.Storage
{
    public interface ISettingsStorage
    {
        void Load(Settings settings);
        void Save(Settings settings);

        Func<WrongSettingsFileVersionArgs, object> SettingsNeedUpdateAction { get; set; }
    }

    public abstract class WrongSettingsFileVersionArgs : EventArgs
    {
        public readonly Version CurrentVersion;
        public readonly Version FileVersion;


        protected WrongSettingsFileVersionArgs(Version currentVersion, Version fileVersion)
        {
            CurrentVersion = currentVersion;
            FileVersion = fileVersion;
        }
    }
}
