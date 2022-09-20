using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Receiver2;

namespace Receiver2ModdingKit {
	public static class ModLoader {

		private static GameObject prefab_9mm;
		public static void InstallGunMod() {
			
		}
		
		public static bool IsGunModInstalled(string gunName) {
			if (Directory.Exists(Path.Combine(Application.persistentDataPath, "Guns", gunName))) {
				foreach (string file in Directory.GetFiles(Path.Combine(Application.persistentDataPath, "Guns", gunName))) {
					if (file.Contains(SystemInfo.operatingSystemFamily.ToString().ToLower())) return true;
				}
			}

			return false;
		}

		public static void LoadGun(ModGunScript gun) {
			if (IsGunLoaded(gun)) {
				Debug.Log("Gun " + gun.InternalName + " is already loaded");
				return;
			}

			List<InventoryItem> items = new List<InventoryItem>( //Loading gun
				ReceiverCoreScript.Instance().generic_prefabs
			) { gun };

			if (!prefab_9mm) { //Fallback bullet prefab
				prefab_9mm = items.First( item => { return item is ShellCasingScript && ((ShellCasingScript) item).cartridge_type == CartridgeSpec.Preset._9mm; }).gameObject;
			}
				
			Editor.InBuiltCartridge cartridge = gun.GetComponent<Editor.InBuiltCartridge>(); //Handling cartridges
			if (cartridge) {
				try {
					gun.loaded_cartridge_prefab = items.First( item => { return item is ShellCasingScript && ((ShellCasingScript) item).cartridge_type == cartridge.preset; }).gameObject;
					UnityEngine.Object.DestroyImmediate(cartridge);
				} catch (InvalidOperationException) {
					Debug.LogError("Error when setting cartridge for gun \"" + gun.InternalName + "\". Cannot find prefab for cartridge type \"" + cartridge.preset + "\"");
				}
			}
			else if (gun.loaded_cartridge_prefab) {
				ShellCasingScript round = gun.loaded_cartridge_prefab.GetComponent<ShellCasingScript>();

				if (round.glint_renderer && round.glint_renderer.material.name != "ItemGlint (Instance)") {
					round.glint_renderer.material = prefab_9mm.GetComponent<ShellCasingScript>().glint_renderer.material;
				}

				ModdingKitCorePlugin.custom_cartridges.Add(
					(uint) round.cartridge_type,
					gun.GetCustomCartridgeSpec()
				);

				items.Add(round);
			}
			else {
				gun.loaded_cartridge_prefab = prefab_9mm;
			}

			if (gun.magazine_root_types.Length != 0) { //Loading magazine
				List<MagazineScript> magazines =
					((List<MagazineScript>) ReflectionManager.RCS_magazine_prefabs_all.GetValue(ReceiverCoreScript.Instance()))
						.Where( mag => !items.Contains(mag) && gun.magazine_root_types.Contains(mag.magazine_root_type)).ToList();

				foreach (MagazineScript magazine in magazines) {
					if (!magazine.round_prefab) magazine.round_prefab = gun.loaded_cartridge_prefab;
				}

				items.AddRange( magazines );
			}

			if (!Locale.active_locale_tactics.ContainsKey(gun.InternalName)) { //Loading pause menu description
				Locale.active_locale_tactics.Add(gun.InternalName, gun.GetGunTactics());
			}

			ModHelpEntry entry = gun.GetGunHelpEntry(); //Loading help menu entry
			if (entry != null) {
				entry.settings_button_active = gun.generate_settings_button;
				ModHelpEntryManager.entries.Add(gun.InternalName, entry);
			}

			if (gun.audio != null) {
				gun.audio.Initialize(gun);

				if (!CustomSounds.ModAudioManager.GetAllEvents().Contains(gun.audio.sound_events[0])) gun.audio.force_options = Editor.CustomSoundsList.ForceOptions.ForceDefaultSounds;
			}

			gun.InitializeGun();

			ReceiverCoreScript.Instance().generic_prefabs = items.ToArray();
		}

		public static bool IsGunLoaded(GunScript gun) {
			foreach (InventoryItem item in ReceiverCoreScript.Instance().generic_prefabs) {
				if (item is GunScript && item.InternalName == gun.InternalName) return true;
			}

			return false;
		}

		public static bool IsGunLoaded(string gunName) {
			foreach (InventoryItem item in ReceiverCoreScript.Instance().generic_prefabs) {
				if (item is GunScript && item.InternalName == gunName) return true;
			}

			return false;
		}
	}
}
