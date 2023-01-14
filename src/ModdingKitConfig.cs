using System;
using BepInEx.Configuration;
using Receiver2ModdingKit.ModInstaller;

namespace Receiver2ModdingKit {
	internal class ModdingKitConfig {
		public static ConfigFile Config {
			get { return ModdingKitCorePlugin.instance.Config; }
		}

		public static ConfigEntry<bool> use_custom_sounds;
		public static ConfigEntry<bool> immersive_aiming;

		public static void Initialize() {
			use_custom_sounds = Config.Bind("Guns settings", "Use Custom Sounds", true, "Should modded guns use custom sounds.");
			immersive_aiming = Config.Bind("Guns settings", "Immersive two handed aiming", true, "Tilt the camera slightly while aiming with a long gun");

			SettingsMenuManager.CreateSettingsMenuOption("Custom Sounds", use_custom_sounds, 9);
			SettingsMenuManager.CreateSettingsMenuOption("Immersive Aiming", immersive_aiming, 10);

			SettingsMenuManager.CreateSettingsMenuButton("Click", ModLoader.SearchForMod, 11);
		}

		public static void AddConfigEventListener(EventHandler<SettingChangedEventArgs> settings_changed_listener) {
			if (ModdingKitCorePlugin.instance != null) ModdingKitCorePlugin.instance.Config.SettingChanged += settings_changed_listener;
		}
	}
}
