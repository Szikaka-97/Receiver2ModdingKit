using System;
using BepInEx.Configuration;
using Receiver2ModdingKit.ModInstaller;

namespace Receiver2ModdingKit {
	internal class ModdingKitConfig {
		public static ConfigFile Config {
			get { return ModdingKitCorePlugin.instance.Config; }
		}

		public static ConfigEntry<bool> use_custom_sounds;

		public static void Initialize() {
			use_custom_sounds = Config.Bind("Guns settings", "Use Custom Sounds", true, "Should modded guns use custom sounds.");

			SettingsMenuManager.CreateSettingsMenuOption("Custom Sounds", use_custom_sounds, 9);

			SettingsMenuManager.CreateSettingsMenuButton("Install Gun Mod", "Install", ModLoader.SearchForMod, 10); // Głupie
		}

		public static void AddConfigEventListener(EventHandler<SettingChangedEventArgs> settings_changed_listener) {
			if (ModdingKitCorePlugin.instance != null) ModdingKitCorePlugin.instance.Config.SettingChanged += settings_changed_listener;
		}
	}
}
