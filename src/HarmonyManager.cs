using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using HarmonyLib;
using Receiver2;
using System.Linq;
using Receiver2ModdingKit.ModInstaller;
using Receiver2ModdingKit.Editor;
using ImGuiNET;
using Wolfire;

namespace Receiver2ModdingKit {
    public static class HarmonyManager {

		#region Transpilers

		[HarmonyPatch(typeof(RuntimeTileLevelGenerator), "PopulateItems")]
		private static class PopulateItemsTranspiler {
			private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod) {
				CodeMatcher codeMatcher = new CodeMatcher(instructions).MatchForward(false, 
					new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GunScript), "gun_type")),
					new CodeMatch(OpCodes.Ldc_I4_1)
				);

				if (!codeMatcher.ReportFailure(__originalMethod, Debug.Log)) {
					codeMatcher.SetOperandAndAdvance(
						AccessTools.Field(typeof(GunScript), "magazine_root_types")
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
					new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GunScript), "gun_model")),
					new CodeMatch(OpCodes.Ldc_I4_1),
					new CodeMatch(OpCodes.Bne_Un)
				);

				if (!codeMatcher.ReportFailure(__originalMethod, Debug.LogError)) {
					codeMatcher
						.Advance(1)
						.InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
						.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModdingKitCorePlugin), "UpdateModGuns")));
				}
				
				return codeMatcher.InstructionEnumeration();
			}
		}

		[HarmonyPatch(typeof(MenuManagerScript), "UpdateDeveloperMenu")]
		private static class DevMenuTranspiler {
			private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod) {
				CodeMatcher codeMatcher = new CodeMatcher(instructions, generator)
				.MatchForward(false, 
					new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(ImGui), "EndMainMenuBar"))
				);

				if (!codeMatcher.ReportFailure(__originalMethod, Debug.LogError)) {
					codeMatcher
						.SetAndAdvance(OpCodes.Ldstr, "Modding")
						.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ImGui), "BeginMenu", new Type[] { typeof(string) })))
						.Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ImGui), "EndMainMenuBar")))
						.InsertBranchAndAdvance(OpCodes.Brfalse, codeMatcher.Pos)
						.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModInfoAsset), "DisplayImGuiControls")))
						.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ImGui), "EndMenu")));
				}
								
				return codeMatcher.InstructionEnumeration();
			}
		}

		[HarmonyPatch(typeof(AudioManager), "Update")]
		private static class AudioDebugMenuTranspiler {
			private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod) {
				CodeMatcher codeMatcher = new CodeMatcher(instructions, generator)
				.MatchForward(false, 
					new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(ImGui), "End"))
				)
				.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CustomSounds.ModAudioManager), "DrawImGUIDebug")));

				return codeMatcher.InstructionEnumeration();
			}
		}

		#endregion


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
				
		internal static class HarmonyInstances {
			public static Harmony Core;
			public static Harmony PopulateItems;
			public static Harmony GunScript;
			public static Harmony ModHelpEntry;
			public static Harmony CustomSounds;
			public static Harmony TransformDebug;
			public static Harmony DevMenu;
			public static Harmony FMODDebug;
		}

		internal static void UnpatchAll() {
			foreach(var field in typeof(HarmonyInstances).GetFields()) {
				Harmony patch = (Harmony) field.GetValue(null);
				if (field != null) patch.UnpatchSelf();
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

		[HarmonyPatch(typeof(CartridgeSpec), "SetFromPreset")]
		[HarmonyPrefix]
		private static void PatchSetCartridge(CartridgeSpec.Preset preset, ref CartridgeSpec __instance) {
			if (ModdingKitCorePlugin.custom_cartridges.ContainsKey((uint) preset)) {
				CartridgeSpec spec = ModdingKitCorePlugin.custom_cartridges[(uint) preset];

				__instance.extra_mass = spec.extra_mass;
				__instance.mass = spec.mass;
				__instance.speed = spec.speed;
				__instance.diameter = spec.diameter;
			}
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

			foreach(var ev in ModdingKitCorePlugin.ExecuteOnStartup.GetInvocationList()) {
				try {
					ev.DynamicInvoke();
				} catch (Exception e) {
					Debug.LogError("Failed invoking startup event for method " + ev.Method.Name + ";\nDumping stack trace:");
					Debug.LogError(e);
				}
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

			ModdingKitCorePlugin.instance.StartCoroutine(FixLegacySounds()); //Calling this method has to be delayed to wait for patches from all plugins to get applied
		}
	}
}
