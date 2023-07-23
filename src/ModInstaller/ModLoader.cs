using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Receiver2;
using HarmonyLib;
using System.Reflection;
using Receiver2ModdingKit.CustomSounds;

namespace Receiver2ModdingKit.ModInstaller {
	public static class ModLoader {

		private static GameObject prefab_9mm;
		internal static ModInstallerObject mod_installer;

		public static void SearchForMod() {
			mod_installer = ModdingKitCorePlugin.instance.gameObject.AddComponent<ModInstallerObject>();
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

			List<InventoryItem> items = new List<InventoryItem>(
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

				if (!ModShellCasingScript.mod_cartridges.ContainsKey(round.cartridge_type)) {
					if ((int) round.cartridge_type > (int) Enum.GetValues(typeof(CartridgeSpec.Preset)).Cast<CartridgeSpec.Preset>().Max()) {
						if (round.glint_renderer && round.glint_renderer.material.name != "ItemGlint (Instance)") {
							round.glint_renderer.material = prefab_9mm.GetComponent<ShellCasingScript>().glint_renderer.material;
						}

						if (round is ModShellCasingScript) {
							ModShellCasingScript.mod_cartridges.Add(
								round.cartridge_type,
								((ModShellCasingScript) round).spec.CreateSpec()
							);
						}
						else {
							Debug.LogWarning("Gun " + gun.InternalName + " adds a cartridge via ModGunScript.GetCustomCartridgeSpec(); Consider using ModShellCasingScript instead");

#pragma warning disable CS0618 // Type or member is obsolete
							ModShellCasingScript.mod_cartridges.Add(
								round.cartridge_type,
								gun.GetCustomCartridgeSpec()
							);
#pragma warning restore CS0618 // Type or member is obsolete
						}

						items.Add(round);
					}
					else {
						Debug.LogError("Gun " + gun.InternalName + " tried to overwrite cartridge " + round.cartridge_type + "; Inbuilt cartridge will be used instead");
					}

				}
				else {
					Debug.LogWarning("Possible redefinition of cartridge with type " + round.cartridge_type);
				}
			}
			else {
				Debug.LogWarning("Gun " + gun.InternalName + " doesn't specify a cartridge and doesn't contain an InBuiltCartridge component and will use a defaul 9mm round");

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

				foreach(var field in gun.GetType().GetFields(BindingFlags.Instance).Where(field => field.FieldType == typeof(string) && field.GetCustomAttribute<CustomEventRef>() != null)) {
					gun.audio.BindField(field);
				}

				foreach (var sound in gun.audio.sound_events) {
					if (sound != "" && !sound.StartsWith("event:/") && !ModAudioManager.GetAllEvents().Contains(sound)) {
						Debug.LogError("Failed to load sound event \"" + sound + "\", falling back on default sounds");
						gun.audio.force_options = Editor.CustomSoundsList.ForceOptions.ForceDefaultSounds;
						break;
					}
				}
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

			try {
				gun.InitializeGun();
			} catch (Exception e) {
				Debug.LogError("Error accured while initializing gun " + gun.InternalName + ":");
				Debug.LogException(e);
			}

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
