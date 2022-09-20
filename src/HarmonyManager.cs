using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using HarmonyLib;
using ImGuiNET;
using Receiver2;

namespace Receiver2ModdingKit {
	static class HarmonyManager {
		[HarmonyPatch(typeof(RuntimeTileLevelGenerator), "PopulateItems")]
		static class PopulateItemsTranspiler {
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

		[HarmonyPatch(typeof(MenuManagerScript), "UpdateDeveloperMenu")]
		static class MenuTranspiler {
			private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod) {
				CodeMatcher codeMatcher = new CodeMatcher(instructions).MatchForward(false, 
					new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(ImGui), "EndMainMenuBar"))
				);

				if (!codeMatcher.ReportFailure(__originalMethod, Debug.LogError)) {
					Debug.Log("Patching");

					codeMatcher
						.SetAndAdvance(OpCodes.Nop, null)
						.InsertAndAdvance(new CodeInstruction(OpCodes.Ldstr, "Moje Menu"))
						.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ImGui), "BeginMenu", new Type[] {typeof(string)})))
						.InsertAndAdvance(new CodeInstruction(OpCodes.Pop))
						.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ImGui), "EndMenu")))
						.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ImGui), "EndMainMenuBar")));
				}

				return codeMatcher.InstructionEnumeration();
			}

			public static void createMenu() {
				if (ImGui.BeginMenu("Moje Menu")) {
					ImGui.Text("Lubie Placki");
				}
				ImGui.EndMenu();
			}
		}

		[HarmonyPatch(typeof(GunScript), "Update")]
		static class GunScriptTranspiler {
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

		[HarmonyPatch(typeof(ReceiverCoreScript), "Awake")]
		[HarmonyPostfix]
		static void PatchCoreAwake(ref ReceiverCoreScript __instance, List<GameObject> ___gun_prefabs_all) {
			foreach(var gun in ___gun_prefabs_all) {
				var modGunScript = gun.GetComponent<ModGunScript>();

				if (modGunScript != null) {
					ModLoader.LoadGun(modGunScript);
				}
			}

			ModdingKitConfig.Initialize();
		}

		[HarmonyPatch(typeof(CartridgeSpec), "SetFromPreset")]
		[HarmonyPrefix]
		static void PatchSetCartridge(CartridgeSpec.Preset preset, ref CartridgeSpec __instance) {
			if (ModdingKitCorePlugin.custom_cartridges.ContainsKey((uint) preset)) {
				CartridgeSpec spec = ModdingKitCorePlugin.custom_cartridges[(uint) preset];

				__instance.extra_mass = spec.extra_mass;
				__instance.mass = spec.mass;
				__instance.speed = spec.speed;
				__instance.diameter = spec.diameter;
			}
		}
		
		public static void Initialize() {
			Harmony.CreateAndPatchAll(typeof(HarmonyManager));
			Harmony.CreateAndPatchAll(typeof(PopulateItemsTranspiler));
			Harmony.CreateAndPatchAll(typeof(GunScriptTranspiler));
			Harmony.CreateAndPatchAll(typeof(ModHelpEntryManager));
			Harmony.CreateAndPatchAll(typeof(CustomSounds.ModAudioManager));
		}
	}
}
