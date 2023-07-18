using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq;
using System;
using UnityEngine;
using HarmonyLib;
using ImGuiNET;
using Receiver2;
using Receiver2ModdingKit.ModInstaller;
using Receiver2ModdingKit.Editor;
using System.IO;

namespace Receiver2ModdingKit {
    public static class HarmonyManager {
		internal static class HarmonyInstances {
			public static Harmony Core;
			public static Harmony PopulateItems;
			public static Harmony GunScript;
			public static Harmony ModHelpEntry;
			public static Harmony CustomSounds;
			public static Harmony TransformDebug;
			public static Harmony DevMenu;
			public static Harmony FMODDebug;
			public static Harmony Thunderstore;
		}

		#region Transpilers

		[HarmonyPatch(typeof(RuntimeTileLevelGenerator), nameof(RuntimeTileLevelGenerator.PopulateItems))]
		private static class PopulateItemsTranspiler {
			private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod) {
				CodeMatcher codeMatcher = new CodeMatcher(instructions).MatchForward(false, 
					new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GunScript), nameof(GunScript.gun_type))),
					new CodeMatch(OpCodes.Ldc_I4_1)
				);

				if (!codeMatcher.ReportFailure(__originalMethod, Debug.Log)) {
					codeMatcher.SetOperandAndAdvance(
						AccessTools.Field(typeof(GunScript), nameof(GunScript.magazine_root_types))
					).InsertAndAdvance(
						new CodeInstruction(OpCodes.Ldlen)
					).SetOpcodeAndAdvance(
						OpCodes.Ldc_I4_0
					).SetOpcodeAndAdvance(
						OpCodes.Bne_Un_S
					);
				}

				return codeMatcher.InstructionEnumeration();
			}
		}

		[HarmonyPatch(typeof(GunScript), "Update")]
		private static class GunScriptTranspiler {
			private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod) {
				CodeMatcher codeMatcher = new CodeMatcher(instructions)
				.MatchForward(false, 
					new CodeMatch(OpCodes.Ldarg_0),
					new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GunScript), nameof(GunScript.gun_model))),
					new CodeMatch(OpCodes.Ldc_I4_1),
					new CodeMatch(OpCodes.Bne_Un)
				);

				if (!codeMatcher.ReportFailure(__originalMethod, Debug.LogError)) {
					codeMatcher
						.Advance(1)
						.InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
						.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModGunScript), nameof(ModGunScript.UpdateModGun))));
				}
				
				return codeMatcher.InstructionEnumeration();
			}
		}

		[HarmonyPatch(typeof(MenuManagerScript), "UpdateDeveloperMenu")]
		private static class DevMenuTranspiler {
			private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod) {
				CodeMatcher codeMatcher = new CodeMatcher(instructions, generator)
				.MatchForward(false,
					new CodeMatch(OpCodes.Ldstr, "Subtitles")
				);

				if (!codeMatcher.ReportFailure(__originalMethod, Debug.LogError)) {
					codeMatcher
						.SetAndAdvance(OpCodes.Ldstr, "Tapes Unlock Debug")
						.InsertAndAdvance(new CodeInstruction(OpCodes.Ldstr, ""))
						.InsertAndAdvance(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ModTapeManager), nameof(ModTapeManager.tapes_debug_window_open))))
						.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ImGui), nameof(ImGui.MenuItem), new Type[] { typeof(string), typeof(string), typeof(bool) })))
						.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModTapeManager), nameof(ModTapeManager.SwitchMenuVisible))))
						.Insert(new CodeInstruction(OpCodes.Ldstr, "Subtitles"))
						.Advance(-1)
						.InsertBranch(OpCodes.Brfalse, codeMatcher.Pos + 1);
				}

				codeMatcher.MatchForward(false, 
					new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(ImGui), nameof(ImGui.EndMainMenuBar)))
				);

				if (!codeMatcher.ReportFailure(__originalMethod, Debug.LogError)) {
					codeMatcher
						.SetAndAdvance(OpCodes.Ldstr, "Modding")
						.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ImGui), nameof(ImGui.BeginMenu), new Type[] { typeof(string) })))
						.Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ImGui), nameof(ImGui.EndMainMenuBar))))
						.InsertBranchAndAdvance(OpCodes.Brfalse, codeMatcher.Pos)
						.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModInfoAsset), nameof(ModInfoAsset.DisplayImGuiControls))))
						.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ImGui), nameof(ImGui.EndMenu))));
				}
								
				return codeMatcher.InstructionEnumeration();
			}
		}

		[HarmonyPatch(typeof(AudioManager), "Update")]
		private static class AudioDebugMenuTranspiler {
			private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod) {
				CodeMatcher codeMatcher = new CodeMatcher(instructions, generator)
				.MatchForward(false, 
					new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(ImGui), nameof(ImGui.End)))
				);

				if (!codeMatcher.ReportFailure(__originalMethod, Debug.LogError)) {
					codeMatcher.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CustomSounds.ModAudioManager), nameof(CustomSounds.ModAudioManager.DrawImGUIDebug))));
				}

				return codeMatcher.InstructionEnumeration();
			}
		}

		#if THUNDERSTORE
		private static class R2ModManTranspilers {
			[HarmonyPatch(typeof(Locale), nameof(Locale.GetTapeSubtitle), new Type[] { typeof(string), typeof(LocaleID)})]
			[HarmonyPostfix]
			public static void PatchLocaleSubtitles(string tape_id, LocaleID locale_id, ref TapeSubtitles __result) {
				ModTapeManager.TryReplaceSubtitles(tape_id, ref __result);
			}

			[HarmonyPatch(typeof(ReceiverCoreScript), "Awake")]
			public static class R2MMCoreTranspiler {
				private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod) {
					CodeMatcher codeMatcher = new CodeMatcher(instructions, generator)
					.MatchForward(false, 
						new CodeMatch(OpCodes.Ldarg_0),
						new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(ReceiverCoreScript), nameof(ReceiverCoreScript.LoadPersistentData)))
					);

					if (!codeMatcher.ReportFailure(__originalMethod, Debug.LogError)) {
						codeMatcher.Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Thunderstore.Thunderstore), nameof(Thunderstore.Thunderstore.InstallGuns))));
					}

					return codeMatcher.InstructionEnumeration();
				}
			}

			[HarmonyPatch(typeof(TapeManager), "OnEnable")]
			public static class R2MMTapesTranspiler {
				private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod) {
					CodeMatcher codeMatcher = new CodeMatcher(instructions, generator)
					.MatchForward(true, 
						new CodeMatch(OpCodes.Ldarg_0),
						new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(TapeManager), "tape_prefabs_all")),
						new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(List<GameObject>), "get_" + nameof(List<GameObject>.Count)))
					);

					if (!codeMatcher.ReportFailure(__originalMethod, Debug.LogError)) {
						codeMatcher
							.InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0, null))
							.Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Thunderstore.Thunderstore), nameof(Thunderstore.Thunderstore.InstallTapes))));
					}

					return codeMatcher.InstructionEnumeration();
				}
			}

			[HarmonyPatch(typeof(ModulePrefabsList), "OnEnable")]
			public static class R2MMTilesTranspiler {
				private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod) {
					CodeMatcher codeMatcher = new CodeMatcher(instructions, generator)
					.MatchForward(true,
						new CodeMatch(OpCodes.Ldloc_2),
						new CodeMatch(OpCodes.Ldloc_1),
						new CodeMatch(OpCodes.Ldlen),
						new CodeMatch(OpCodes.Conv_I4),
						new CodeMatch(OpCodes.Blt)
					);

					if (!codeMatcher.ReportFailure(__originalMethod, Debug.LogError)) {
						codeMatcher
							.Advance(2)
							.InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0, null))
							.Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Thunderstore.Thunderstore), nameof(Thunderstore.Thunderstore.InstallTiles))));
					}

					return codeMatcher.InstructionEnumeration();
				}
			}
		}
		#endif

		#endregion

		#region General Patches

		[HarmonyPatch(typeof(MagazineScript), "UpdateRoundPositions")]
		[HarmonyPostfix]
		private static void PatchMagazineRoundPositions(ref MagazineScript __instance) {
			if (__instance is not DoubleStackMagazine) return;

			for (int i = 0; i < __instance.NumRounds(); i++) {
				var round = __instance.rounds[i];

				bool left = i % 2 == 0 == (__instance.rounds.Count % 2 == 0) != __instance.round_position_params.mirror_double_stack;

				round.transform.localPosition = new Vector3(
					(Vector3.right * round.GetComponent<BoxCollider>().size.x * __instance.round_position_params.param_e * (left ? -1 : 1)).x,
					round.transform.localPosition.y,
					round.transform.localPosition.z
				);
			}

			if (__instance.extracting) {
				var round = __instance.rounds[0];

				float x = Mathf.InverseLerp(
					Vector3.Dot(__instance.slot.transform.Find("barrel/point_round_entering").position, __instance.transform.forward),
					Vector3.Dot(__instance.round_top.position, __instance.transform.forward),
					Vector3.Dot(__instance.slot.transform.Find("slide/point_chambered_round").position, __instance.transform.forward)
				);

				bool left = (__instance.rounds.Count % 2 == 0) != __instance.round_position_params.mirror_double_stack;

				round.transform.localPosition = new Vector3(
					(round.transform.right * round.GetComponent<BoxCollider>().size.x * __instance.round_position_params.param_e * (left ? -1 : 1)).x * x,
					round.transform.localPosition.y,
					round.transform.localPosition.z
				);
			}
		}

		[HarmonyPatch(typeof(TapesMenuScript), "OnLoad")]
        [HarmonyPrefix]
		private static void PatchTapeMenuLoad() {
			if (ReceiverCoreScript.Instance().game_mode.GetGameMode() == GameMode.ReceiverMall) {
				ModTapeManager.PrepareTapesForCompound();
			}
			else {
				ModTapeManager.PrepareTapesForGame();
			}
		}

		[HarmonyPatch(typeof(TapesMenuScript), "CreateMenuEntries")]
        [HarmonyPrefix]
        private static void PrePatchTapeCreateMenuEntries(ref TapesMenuScript __instance) {
			ModTapeManager.Init();

			GameObject entry_prefab = UnityEngine.Object.Instantiate(GameObject.Find("ReceiverCore/Menus/Overlay Menu Canvas/Aspect Ratio Fitter/New Pause Menu/Backdrop1/Sub-Menu Layout Group/New Tape Menu/Entries Layout/ScrollableContent Variant/Viewport/Content/Standard/Invalid"));

			__instance.ecsPrefab = entry_prefab;
        }

		[HarmonyPatch(typeof(TapesMenuScript), "CreateMenuEntries")]
        [HarmonyPostfix]
		private static void PostPatchTapeCreateMenuEntries(TapesMenuScript __instance) {
			ModTapeManager.CreateModTapes(__instance);
		}

		[HarmonyPatch(typeof(CartridgeSpec), nameof(CartridgeSpec.SetFromPreset))]
		[HarmonyPrefix]
		private static bool PatchSetCartridge(CartridgeSpec.Preset preset, ref CartridgeSpec __instance) {
			if (ModShellCasingScript.mod_cartridges.ContainsKey(preset)) {
				__instance = ModShellCasingScript.mod_cartridges[preset];

				return false;
			}
			return true;
		}

		[HarmonyPatch(typeof(ReceiverCoreScript), "Awake")]
		[HarmonyPostfix]
		private static void PatchCoreAwake(List<GameObject> ___gun_prefabs_all) {
			foreach(var gun in ___gun_prefabs_all) {
				var modGunScript = gun.GetComponent<ModGunScript>();

				if (modGunScript != null) {
					try {
						ModLoader.LoadGun(modGunScript);
					} catch (Exception e) {
						Debug.LogError("Error happened when loading gun " + modGunScript.InternalName);
						Debug.LogError(e);
					}
				}
			}

			//doing this prevents stocks for some guns from clipping into the camera.
			ReceiverCoreScript.Instance().player_prefab.GetComponent<PlayerScript>().main_camera_prefab.GetComponent<Camera>().nearClipPlane = 0.02f;

			foreach (var subtitle_file in Directory.GetFiles(Path.Combine(Application.persistentDataPath, "Tapes"), "*.srt", SearchOption.AllDirectories)) {
				FileInfo subtitle_file_info = new FileInfo(subtitle_file);

				ModTapeManager.RegisterSubtitles(subtitle_file_info.Name.Replace(".srt", ""), subtitle_file_info);
			}

			try {
				ModdingKitConfig.Initialize();
			} catch {
				Debug.LogError("An error accured when trying to create settings for the Modding Kit");
			}

			foreach (var asset_bundle in AssetBundle.GetAllLoadedAssetBundles()) {
				BankList[] lists = asset_bundle.LoadAllAssets<BankList>();

				if (lists != null) { 
					CustomSounds.ModAudioManager.LoadBanksFromLists(lists);
				}
			}

			foreach (var tape in ReceiverCoreScript.Instance().tape_loadout_asset.GetModTapes()) {
				LocaleTapeMenuEntry entry = new LocaleTapeMenuEntry();
				entry.name = "Invalid"; //the name is always "Invalid", for all entries. At least those that I checked.
				entry.id_string = tape.tape_id_string;
				entry.title = tape.title;
				entry.description = tape.text;
				Locale.active_locale_tape_menu_entries_string.Add(tape.tape_id_string, entry);
			}

			foreach(var ev in ModdingKitCorePlugin.ExecuteOnStartup.GetInvocationList()) {
				try {
					ev.DynamicInvoke();
				} catch (Exception e) {
					Debug.LogError("Failed invoking startup event for method " + ev.Method.Name + ";\nDumping stack trace:");
					Debug.LogError(e);
				}
			}
		}

		[HarmonyPatch(typeof(ShellCasingScript), nameof(ShellCasingScript.CollisionSound))]
		[HarmonyPrefix]
		private static bool PatchCollisionSound(BallisticMaterial material, ref ShellCasingScript __instance) {
			if (__instance.physics_collided || __instance is not ModShellCasingScript) return true;
    
			ModShellCasingScript bullet = (ModShellCasingScript) __instance;
    
			bullet.physics_collided = true;

			if (!bullet.IsSpent()) {
				AudioManager.PlayOneShot3D(bullet.sound_bullet_fall_hard, bullet.transform.position, 1f, 1f);
        
				return false;
			}
    
			switch (material.material_label) {
				case BallisticMaterial.MaterialLabel.Glass:
				case BallisticMaterial.MaterialLabel.Metal:
				case BallisticMaterial.MaterialLabel.Concrete:
				case BallisticMaterial.MaterialLabel.Wood:
				case BallisticMaterial.MaterialLabel.Tile:
				case BallisticMaterial.MaterialLabel.Drywall:
				case BallisticMaterial.MaterialLabel.HardMetal:
				case BallisticMaterial.MaterialLabel.CardboardBox:
				case BallisticMaterial.MaterialLabel.GlassBulletproof:
					AudioManager.PlayOneShot3D(bullet.sound_shell_casing_impact_hard, bullet.transform.position, 1f, 1f);
            
					break;
				case BallisticMaterial.MaterialLabel.Flesh:
				case BallisticMaterial.MaterialLabel.Pillow:
				case BallisticMaterial.MaterialLabel.Plant:
				case BallisticMaterial.MaterialLabel.Thick_Metal:
				case BallisticMaterial.MaterialLabel.Plastic:
				case BallisticMaterial.MaterialLabel.Cloth:
				case BallisticMaterial.MaterialLabel.Paper:
					AudioManager.PlayOneShot3D(bullet.sound_shell_casing_impact_soft, bullet.transform.position, 1f, 1f);
            
					break;
			}
    
			return false;
		}

		#endregion

		internal static void UnpatchAll() {
			foreach(var field in typeof(HarmonyInstances).GetFields()) {
				Harmony patch = (Harmony) field.GetValue(null);
				if (patch != null) patch.UnpatchSelf();
			}
		}

		private static System.Collections.IEnumerator FixLegacySounds() {
			yield return null; //1 frame of delay

			foreach (var method in HarmonyInstances.CustomSounds.GetPatchedMethods()) {
				Patches patch = Harmony.GetPatchInfo(method); //You look reasonably sane bruv

				if(patch.Owners.Count == 1) yield break; //All is well, no need to do anything

				HarmonyInstances.CustomSounds.Unpatch(method, HarmonyPatchType.Prefix, patch.Owners.First(ownerID => ownerID != HarmonyInstances.CustomSounds.Id));
			}
		}

		internal static void Initialize() {
			HarmonyInstances.Core = Harmony.CreateAndPatchAll(typeof(HarmonyManager));
			HarmonyInstances.PopulateItems = Harmony.CreateAndPatchAll(typeof(PopulateItemsTranspiler));
			HarmonyInstances.GunScript = Harmony.CreateAndPatchAll(typeof(GunScriptTranspiler));
			HarmonyInstances.ModHelpEntry = Harmony.CreateAndPatchAll(typeof(ModHelpEntryManager));
			HarmonyInstances.CustomSounds = Harmony.CreateAndPatchAll(typeof(CustomSounds.ModAudioPatches));

			#if DEBUG
			HarmonyInstances.DevMenu = Harmony.CreateAndPatchAll(typeof(DevMenuTranspiler));
			HarmonyInstances.TransformDebug = Harmony.CreateAndPatchAll(typeof(TransformDebugScope));
			HarmonyInstances.FMODDebug = Harmony.CreateAndPatchAll(typeof(AudioDebugMenuTranspiler));
			#endif

			#if THUNDERSTORE
			if (Thunderstore.Thunderstore.LaunchedWithR2ModMan) {
				HarmonyInstances.Thunderstore = Harmony.CreateAndPatchAll(typeof(R2ModManTranspilers));

				foreach (var clazz in typeof(R2ModManTranspilers).GetNestedTypes()) {
					HarmonyInstances.Thunderstore.PatchAll(clazz);
				}
			}
			#endif

			ModdingKitCorePlugin.instance.StartCoroutine(FixLegacySounds()); //Calling this method has to be delayed to wait for patches from all plugins to get applied
		}
	}
}
