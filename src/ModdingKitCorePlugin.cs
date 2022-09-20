using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using Receiver2;

namespace Receiver2ModdingKit {
	[BepInPlugin("pl.szikaka.receiver_2_modding_kit", "Receiver 2 Modding Kit", "0.1.0")]
	internal class ModdingKitCorePlugin : BaseUnityPlugin {
		public static ModdingKitCorePlugin instance {
			get;
			private set;
		}

		internal static Dictionary<uint, CartridgeSpec> custom_cartridges = new Dictionary<uint, CartridgeSpec>();

		public static void UpdateModGuns(GunScript gun) {
			if (gun is ModGunScript) ((ModGunScript) gun).UpdateGun();
		}

		private void Awake() {
			instance = this;

			HarmonyManager.Initialize();
			ReflectionManager.Initialize();

			gameObject.AddComponent<ModHelpEntryManager>();
		}
	}
}
