using System;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using Receiver2ModdingKit.ModInstaller;

namespace Receiver2ModdingKit {
	internal class ModdingKitConfig {
		public static ConfigFile Config {
			get { return ModdingKitCorePlugin.instance.Config; }
		}

		public static ConfigEntry<bool> use_custom_sounds;

		private static Dictionary<string, ConfigEntry<string>> custom_keybinds_config = new Dictionary<string, ConfigEntry<string>>();

		public static void Initialize() {
			use_custom_sounds = Config.Bind("Guns settings", "Use Custom Sounds", true, "Should modded guns use custom sounds.");

			SettingsMenuManager.CreateSettingsMenuOption("Custom Sounds", use_custom_sounds, 9);

			SettingsMenuManager.CreateSettingsMenuButton("Install Gun Mod", "Install", ModLoader.SearchForMod, 10); // Głupie
		}

		public static void AddConfigEventListener(EventHandler<SettingChangedEventArgs> settings_changed_listener) {
			if (ModdingKitCorePlugin.instance != null) ModdingKitCorePlugin.instance.Config.SettingChanged += settings_changed_listener;
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
