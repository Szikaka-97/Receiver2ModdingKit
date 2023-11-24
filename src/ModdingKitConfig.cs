using System;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using Receiver2ModdingKit.ModInstaller;

namespace Receiver2ModdingKit {
	internal class ModdingKitConfig {
		public static ConfigFile Config {
#if !UNITY_EDITOR
			get { return ModdingKitCorePlugin.instance.Config; }
#else
			get { return null; }
#endif
		}

		public static ConfigEntry<bool> use_custom_sounds;

		private static Dictionary<string, ConfigEntry<string>> custom_keybinds_config = new Dictionary<string, ConfigEntry<string>>();

		public static ConfigEntry<bool> enable_asset_caching;
		public static ConfigEntry<string> asset_database_path;
		public static ConfigEntry<string> asset_cache_path;

		public static void Initialize() {
			use_custom_sounds = Config.Bind("Guns settings", "Use Custom Sounds", true, "Should modded guns use custom sounds.");

			asset_database_path = Config.Bind("Assets settings", "Asset Database Path", "./AssetsDatabase", "Path to the database of asset file infos. Change if you want to synchronize across multiple plugin instances");

			enable_asset_caching = Config.Bind("Assets settings", "Enable Asset Caching", true, "Should the plugin chache the extracted assets.");

			asset_cache_path = Config.Bind("Assets settings", "Asset Cache Path", "./AssetsCache", "Path where cached assets will be stored. Change if you want to synchronize across multiple plugin instances");

			SettingsMenuManager.CreateSettingsMenuOption("Custom Sounds", use_custom_sounds, 9);

			SettingsMenuManager.CreateSettingsMenuButton("Install Gun Mod", "Install", ModLoader.SearchForMod, 10); // Głupie
		}

		public static void AddConfigEventListener(EventHandler<SettingChangedEventArgs> settings_changed_listener) {
			if (ModdingKitCorePlugin.instance != null && Config != null) Config.SettingChanged += settings_changed_listener;
		}

		public static void UpdateKeybindValue(Keybind keybind) {
			string keybind_key = "-1";

			if (keybind.key != null) {
				keybind_key = keybind.key.IsRedirect() ? keybind.key.GetKey().ToString() : ((KeyCode) keybind.key.GetKey()).ToString();
			}

			custom_keybinds_config[keybind.GetLongName()].Value = keybind_key;
		}

		public static string BindKeybindConfig(Keybind keybind) {
			string keybind_key = "-1";

			if (keybind.key != null) {
				keybind_key = keybind.key.IsRedirect() ? keybind.key.GetKey().ToString() : ((KeyCode) keybind.key.GetKey()).ToString();
			}

			var keybind_config_entry = Config.Bind("Custom Keybinds", keybind.GetLongName(), keybind_key);

			custom_keybinds_config[keybind.GetLongName()] = keybind_config_entry;

			return keybind_config_entry.Value;
		}
	}
}
