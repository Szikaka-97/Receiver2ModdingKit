using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Receiver2;
using HarmonyLib;


namespace Receiver2ModdingKit.ModInstaller {
	public static class ModLoader {

		private static GameObject prefab_9mm;
		internal static ModInstallerObject mod_installer;

		public static void SearchForMod() {
			mod_installer = ModdingKitCorePlugin.instance.gameObject.AddComponent<ModInstallerObject>();
		}

		internal static void InstallGunMod(ModInstallerObject.ModDirectoryInfo info) {

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
			);

			if (gun.visible_in_spawnmenu) {
				items.Add(gun);
			}

			if (!prefab_9mm) { //Fallback bullet prefab
				prefab_9mm = items.First( item => { return item is ShellCasingScript && ((ShellCasingScript) item).cartridge_type == CartridgeSpec.Preset._9mm && ((ShellCasingScript) item).go_casing != null; }).gameObject;
			}
				
			Editor.InBuiltCartridge cartridge = gun.GetComponent<Editor.InBuiltCartridge>(); //Handling cartridges
			if (cartridge) {
				try {
					gun.loaded_cartridge_prefab = items.First( item => { return item is ShellCasingScript && ((ShellCasingScript) item).cartridge_type == cartridge.preset && ((ShellCasingScript) item).go_casing != null; }).gameObject;
					UnityEngine.Object.DestroyImmediate(cartridge);
				} catch (InvalidOperationException) {
					Debug.LogError("Error when setting cartridge for gun \"" + gun.InternalName + "\". Cannot find prefab for cartridge type \"" + cartridge.preset + "\"");
				}
			}
			else if (gun.loaded_cartridge_prefab) {
				ShellCasingScript round = gun.loaded_cartridge_prefab.GetComponent<ShellCasingScript>();

				if (!ModdingKitCorePlugin.custom_cartridges.ContainsKey((uint) round.cartridge_type)) {
					if (round.glint_renderer && round.glint_renderer.material.name != "ItemGlint (Instance)") {
						round.glint_renderer.material = prefab_9mm.GetComponent<ShellCasingScript>().glint_renderer.material;
					}

					ModdingKitCorePlugin.custom_cartridges.Add(
						(uint) round.cartridge_type,
						gun.GetCustomCartridgeSpec()
					);

					items.Add(round);
				}
			}
			else {
				gun.loaded_cartridge_prefab = prefab_9mm;
			}

			if (gun.magazine_root_types.Length != 0) { //Loading magazine
				List<MagazineScript> magazines =
					((List<MagazineScript>) ReflectionManager.RCS_magazine_prefabs_all.GetValue(ReceiverCoreScript.Instance()))
						.Where( mag => !items.Contains(mag) && gun.magazine_root_types.Contains(mag.magazine_root_type)).ToList();

				foreach (MagazineScript magazine in magazines) { //Handling magazine pegboard collision
					if (magazine.round_prefab == null || magazine.round_prefab.GetComponent<ShellCasingScript>() == null) magazine.round_prefab = gun.loaded_cartridge_prefab;

					if (magazine.GetComponent<PegboardHangableItem>() && magazine.GetComponent<PegboardHangableItem>().pegboard_hanger != null) {
						PegboardHanger hanger = magazine.GetComponent<PegboardHangableItem>().pegboard_hanger;
						Bounds magazineBounds = new Bounds();

						foreach (var renderer in magazine.GetComponentsInChildren<MeshRenderer>()) {
							magazineBounds.Encapsulate(
								new Bounds(
									hanger.transform.InverseTransformPoint(renderer.bounds.center),
									hanger.transform.TransformPoint(renderer.bounds.extents)
								)
							);
						}

						ReflectionManager.PH_bounds.SetValue(hanger, magazineBounds);
					}
				}

				items.AddRange( magazines );
			}

			if (!Locale.active_locale_tactics.ContainsKey(gun.InternalName)) { //Loading pause menu description
				Locale.active_locale_tactics.Add(gun.InternalName, gun.GetGunTactics());
			}

			ModHelpEntry entry = gun.GetGunHelpEntry(); //Loading help menu entry
			if (entry != null && !ModHelpEntryManager.entries.ContainsKey(gun.InternalName)) {
				ModHelpEntryManager.entries.Add(gun.InternalName, entry);
			}

			if (gun.audio != null) { //Loading custom audio
				gun.audio.Initialize(gun);

				if (!CustomSounds.ModAudioManager.GetAllEvents().Contains(gun.audio.sound_events[0])) gun.audio.force_options = Editor.CustomSoundsList.ForceOptions.ForceDefaultSounds;
			}

			if (gun.GetComponent<PegboardHangableItem>() && gun.GetComponent<PegboardHangableItem>().pegboard_hanger != null) { //Handling guns pegboard collision
				PegboardHanger hanger = gun.GetComponent<PegboardHangableItem>().pegboard_hanger;
				Bounds gunBounds = new Bounds();

				foreach (var renderer in gun.GetComponentsInChildren<MeshRenderer>()) {
					gunBounds.Encapsulate(
						new Bounds(
							hanger.transform.InverseTransformPoint(renderer.bounds.center),
							hanger.transform.TransformPoint(renderer.bounds.extents)
						)
					);
				}

				ReflectionManager.PH_bounds.SetValue(hanger, gunBounds);
			}

			gun.InitializeGun();

			if (gun.spawns_in_dreaming) {
				ReceiverCoreScript.Instance().PlayerData.unlocked_gun_names.Add(gun.InternalName);
			}

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
