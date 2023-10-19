using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using ServerSync;

namespace DeathPinRemoval
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class DeathPinRemovalPlugin : BaseUnityPlugin
    {
        internal const string ModName = "DeathPinRemoval";
        internal const string ModVersion = "1.0.3";
        internal const string Author = "Azumatt";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        internal static string ConnectionError = "";
        private readonly Harmony _harmony = new(ModGUID);

        public static readonly ManualLogSource DeathPinRemovalLogger =
            BepInEx.Logging.Logger.CreateLogSource(ModName);

        private static readonly ConfigSync ConfigSync = new(ModGUID)
            { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

        public enum Toggle
        {
            On = 1,
            Off = 0
        }

        public void Awake()
        {
            _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On,
                "If on, the configuration is locked and can be changed by server admins only.");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

            TotalPinRemoval = config("1 - Pin Control", "TotalPinRemoval", Toggle.Off, "If on, no death pin will be created when the player dies.");
            RemoveOnEmpty = config("1 - Pin Control", "RemovePinOnTombstoneInteract", Toggle.On, "If on, the death pin for the tombstone will be removed when the tombstone is fully looted.");
            RemoveIfEmpty = config("1 - Pin Control", "RemovePinIfInventoryWasEmpty", Toggle.On, "If on, the death pin will be removed if the player's inventory was empty when they died.");


            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
        }

        private void OnDestroy()
        {
            Config.Save();
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                DeathPinRemovalLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                DeathPinRemovalLogger.LogError($"There was an issue loading your {ConfigFileName}");
                DeathPinRemovalLogger.LogError("Please check your config entries for spelling and format!");
            }
        }


        #region ConfigOptions

        private static ConfigEntry<Toggle> _serverConfigLocked = null!;
        public static ConfigEntry<Toggle> TotalPinRemoval = null!;
        public static ConfigEntry<Toggle> RemoveOnEmpty = null!;
        public static ConfigEntry<Toggle> RemoveIfEmpty = null!;

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            [UsedImplicitly] public int? Order;
            [UsedImplicitly] public bool? Browsable;
            [UsedImplicitly] public string? Category;
            [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer;
        }

        #endregion
    }
}