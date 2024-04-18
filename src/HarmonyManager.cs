using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using HarmonyLib;
using ImGuiNET;
using Receiver2;
using Receiver2ModdingKit.ModInstaller;
using Receiver2ModdingKit.Editor;
using Receiver2ModdingKit.Helpers;
using Wolfire;
using BepInEx.Logging;

namespace Receiver2ModdingKit {
	public static class HarmonyManager {
		internal static class HarmonyInstances {
			public static Harmony Core;
			public static Harmony Transpilers;
			public static Harmony DebugPatches;
			public static Harmony Thunderstore;
			public static Harmony CustomSounds;
			public static Harmony ModHelpEntry;
		}

		private static void DisplayInstructions(CodeMatcher code_matcher, int breadth) {
			DisplayInstructions(code_matcher, breadth, Debug.LogError);
		}

		private static void DisplayInstructions(CodeMatcher code_matcher, int breadth, Action<string> logger) {
			int start_pos = Mathf.Max(code_matcher.Pos - breadth, 0);
			int end_pos = Mathf.Min(code_matcher.Pos + breadth + 1, code_matcher.Length - 1);

			for (int before = start_pos; before < code_matcher.Pos; before++) {
				logger(code_matcher.InstructionAt(before - code_matcher.Pos).ToString());
			}

			logger(code_matcher.Instruction + " <");

			for (int after = code_matcher.Pos + 1; after < end_pos; after++) {
				logger(code_matcher.InstructionAt(after - code_matcher.Pos).ToString());
			}
		}

		#region Transpilers

		private static class Transpilers {
			[HarmonyPatch(typeof(GunScript), "Update")]
			[HarmonyTranspiler]
			private static IEnumerable<CodeInstruction> GunScriptTranspiler(IEnumerable<CodeInstruction> instructions) {
				return new SmartCodeMatcher(instructions)
				.MatchForward(false, 
					new CodeMatch(OpCodes.Ldarg_0),
					new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GunScript), nameof(GunScript.gun_model))),
					new CodeMatch(OpCodes.Ldc_I4_1),
					new CodeMatch(OpCodes.Bne_Un)
				).Advance(1)
				.InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
				.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModGunScript), nameof(ModGunScript.UpdateModGun))))
				.InstructionEnumeration();

				// if (!codeMatcher.ReportFailure(__originalMethod, Debug.LogError)) {
				// 	codeMatcher
				// 		.Advance(1)
				// 		.InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
				// 		.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModGunScript), nameof(ModGunScript.UpdateModGun))));
				// }
				
				// return codeMatcher.InstructionEnumeration();
			}

			[HarmonyPatch(typeof(RuntimeTileLevelGenerator), nameof(RuntimeTileLevelGenerator.PopulateItems))]
			[HarmonyTranspiler]
			private static IEnumerable<CodeInstruction> PopulateItemsTranspiler(IEnumerable<CodeInstruction> instructions) {
				return new SmartCodeMatcher(instructions)
				.MatchForward(
					false,
					new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GunScript), nameof(GunScript.gun_type))),
					new CodeMatch(OpCodes.Ldc_I4_1)
				).SetOperandAndAdvance(
					AccessTools.Field(typeof(GunScript), nameof(GunScript.magazine_root_types))
				).InsertAndAdvance(
					new CodeInstruction(OpCodes.Ldlen)
				).SetOpcodeAndAdvance(
					OpCodes.Ldc_I4_0
				).SetOpcodeAndAdvance(
					OpCodes.Bne_Un_S
				).InstructionEnumeration();
			}

			[HarmonyPatch(typeof(LocalAimHandler), "HandleGunControls")]
			[HarmonyTranspiler]
			private static IEnumerable<CodeInstruction> LAHGunControlsTranspiler(IEnumerable<CodeInstruction> instructions) {
				return new SmartCodeMatcher(instructions)
				
				.MatchForward(false, 
					new CodeMatch(OpCodes.Ldarg_1),
					new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GunScript), nameof(GunScript.gun_model))),
					new CodeMatch(OpCodes.Ldc_I4_5),
					new CodeMatch(OpCodes.Bne_Un)
				).Advance(1)
				.SetOperandAndAdvance(AccessTools.Field(typeof(GunScript), nameof(GunScript.slide_lock_is_safety)))
				.SetOpcodeAndAdvance(OpCodes.Ldc_I4_1)

				.MatchForward(false, new CodeMatch(OpCodes.Leave))
				.Advance(1)
				.Insert(new CodeInstruction(OpCodes.Nop))

				.InstructionEnumeration();
			}

			[HarmonyPatch(typeof(LocalAimHandler), "UpdateLooseBulletDisplay")]
			[HarmonyTranspiler]
			private static IEnumerable<CodeInstruction> LAHBulletDisplayTranspiler(IEnumerable<CodeInstruction> instructions) {
				
				SmartCodeMatcher code_matcher = new SmartCodeMatcher(instructions)
				.MatchForward(true, 
					new CodeMatch(OpCodes.Ldnull),
					new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Object), "op_Equality")),
					new CodeMatch(OpCodes.Brtrue)
				);

				if (code_matcher.IsValid) {
					var branchInstruction = code_matcher.Instruction;

					code_matcher
					.Start()
					.MatchForward(false, 
						new CodeMatch(OpCodes.Ldarg_0),
						new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(LocalAimHandler), "get_IsHoldingMagazine")),
						new CodeMatch(OpCodes.Brfalse)
					).InsertAndAdvance(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Extensions), nameof(Extensions.lah_force_bullet_display))))
					.Insert(branchInstruction);
				}

				return code_matcher.InstructionEnumeration();
			}

			[HarmonyPatch(typeof(LocalAimHandler), nameof(LocalAimHandler.HandleGunControls))]
			[HarmonyTranspiler]
			private static IEnumerable<CodeInstruction> LAHHandleGunControlsTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
				var code_matcher = new SmartCodeMatcher(instructions, generator)
				.MatchForward(true, 
					new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(GunScript), nameof(GunScript.mag_seated_wrong)))
				)
				.MatchBack(true,
					new CodeMatch(OpCodes.Call),
					new CodeMatch(OpCodes.Call),
					new CodeMatch(OpCodes.Newobj)
				)
				.Advance()
				.InsertAndAdvance(
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Isinst, typeof(ModGunScript))
				)
				.Insert(OpCodes.Call, AccessTools.Method(typeof(Vector3), "op_Multiply", new Type[] { typeof(Vector3), typeof(float) }))
				.CreateBranch(
					OpCodes.Brfalse_S,
					new CodeInstruction[] {
						new CodeInstruction(OpCodes.Ldarg_1),
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModGunScript), nameof(ModGunScript.GetMagSmackStrength)))
					},
					new CodeInstruction[] {
						new CodeInstruction(OpCodes.Ldc_R4, 1f)
					}
				);

				var local_var = generator.DeclareLocal(typeof(Vector3));

				code_matcher
				.Advance()
				.InsertAndAdvance(
					new CodeInstruction(OpCodes.Stloc, local_var.LocalIndex),
					new CodeInstruction(OpCodes.Ldloca, local_var.LocalIndex),
					new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Time), "get_time")),
					new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(Vector3), nameof(Vector3.z))),
					new CodeInstruction(OpCodes.Ldloc, local_var.LocalIndex)
				);

				return code_matcher.InstructionEnumeration();
			}

			[HarmonyPatch(typeof(LocalAimHandler), nameof(LocalAimHandler.Update))]
			[HarmonyTranspiler]
			private static IEnumerable<CodeInstruction> LAHUpdateXRayTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
				return new CodeMatcher(instructions)
				.MatchForward(true,
					new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(LocalAimHandler), nameof(LocalAimHandler.inspect_x_ray_timer)))
				)
				.Advance(2)
				.InsertAndAdvance(
					new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Time), "get_timeScale")),
					new CodeInstruction(OpCodes.Div)
				)
				.InstructionEnumeration();
			}

			[HarmonyPatch(typeof(MagazineScript), nameof(MagazineScript.UpdateRoundPositions))]
			[HarmonyTranspiler]
			private static IEnumerable<CodeInstruction> MagazineFollowerTranspiler(IEnumerable<CodeInstruction> instructions) {
				return new SmartCodeMatcher(instructions)
				.MatchForward(true,
					new CodeMatch(OpCodes.Brfalse),
					new CodeMatch(OpCodes.Ldc_R4),
					new CodeMatch(OpCodes.Ldarg_0)
				)
				.Advance(-1)
				.RemoveInstruction()
				.MatchForward(false,
					new CodeMatch(OpCodes.Sub)
				)
				.RemoveInstruction()
				.InstructionEnumeration();
			}
		}

		private static class DebugTranspilers {
			[HarmonyPatch(typeof(MenuManagerScript), "UpdateDeveloperMenu")]
			[HarmonyTranspiler]
			private static IEnumerable<CodeInstruction> DevMenuTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
				var code_matcher = new SmartCodeMatcher(instructions, generator);

				return code_matcher

				.MatchForward(false,
					new CodeMatch(OpCodes.Ldstr, "Subtitles")
				).SetAndAdvance(OpCodes.Ldstr, "Tapes Unlock Debug")
				.InsertAndAdvance(new CodeInstruction(OpCodes.Ldstr, ""))
				.InsertAndAdvance(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ModTapeManager), nameof(ModTapeManager.tapes_debug_window_open))))
				.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ImGui), nameof(ImGui.MenuItem), new Type[] { typeof(string), typeof(string), typeof(bool) })))
				.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModTapeManager), nameof(ModTapeManager.SwitchMenuVisible))))
				.Insert(new CodeInstruction(OpCodes.Ldstr, "Subtitles"))
				.Advance(-1)
				.InsertBranch(OpCodes.Brfalse, code_matcher.Pos + 1)

				.MatchForward(false, 
					new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(ImGui), nameof(ImGui.EndMainMenuBar)))
				).SetAndAdvance(OpCodes.Ldstr, "Modding")
				.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ImGui), nameof(ImGui.BeginMenu), new Type[] { typeof(string) })))
				.Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ImGui), nameof(ImGui.EndMainMenuBar))))
				.InsertBranchAndAdvance(OpCodes.Brfalse, code_matcher.Pos)
				.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModInfoAsset), nameof(ModInfoAsset.DisplayImGuiControls))))
				.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ImGui), nameof(ImGui.EndMenu))))
				.InstructionEnumeration();
			}

			[HarmonyPatch(typeof(AudioManager), "Update")]
			[HarmonyTranspiler]
			private static IEnumerable<CodeInstruction> AudioDebugMenuTranspiler(IEnumerable<CodeInstruction> instructions) {
				return new SmartCodeMatcher(instructions)
				.MatchForward(false, 
					new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(ImGui), nameof(ImGui.End)))
				).InsertAndAdvance(
					new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CustomSounds.ModAudioManager), nameof(CustomSounds.ModAudioManager.DrawImGUIDebug)))
				).InstructionEnumeration();
			}
		}

		private static class R2ModManPatches {
			[HarmonyPatch(typeof(Locale), nameof(Locale.GetTapeSubtitle), new Type[] { typeof(string), typeof(LocaleID)})]
			[HarmonyPostfix]
			public static void PatchLocaleSubtitles(string tape_id, LocaleID locale_id, ref TapeSubtitles __result) {
				ModTapeManager.TryReplaceSubtitles(tape_id, ref __result);
			}

			[HarmonyPatch(typeof(ReceiverCoreScript), "Awake")]
			[HarmonyTranspiler]
			private static IEnumerable<CodeInstruction> R2MMCoreTranspiler(IEnumerable<CodeInstruction> instructions) {
				return new SmartCodeMatcher(instructions)
				.MatchForward(false, 
					new CodeMatch(OpCodes.Ldarg_0),
					new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(ReceiverCoreScript), nameof(ReceiverCoreScript.LoadPersistentData)))
				).Insert(
					new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Thunderstore.Thunderstore), nameof(Thunderstore.Thunderstore.InstallGuns)))
				).InstructionEnumeration();
			}
			

			[HarmonyPatch(typeof(TapeManager), "OnEnable")]
			[HarmonyTranspiler]
			private static IEnumerable<CodeInstruction> R2MMTapesTranspiler(IEnumerable<CodeInstruction> instructions) {
				return new SmartCodeMatcher(instructions)
				.MatchForward(true, 
					new CodeMatch(OpCodes.Ldarg_0),
					new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(TapeManager), "tape_prefabs_all")),
					new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(List<GameObject>), "get_" + nameof(List<GameObject>.Count)))
				).InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0, null))
				.Insert(
					new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Thunderstore.Thunderstore), nameof(Thunderstore.Thunderstore.InstallTapes)))
				).InstructionEnumeration();
			}
			

			[HarmonyPatch(typeof(ModulePrefabsList), "OnEnable")]
			[HarmonyTranspiler]
			private static IEnumerable<CodeInstruction> R2MMTilesTranspiler(IEnumerable<CodeInstruction> instructions) {
				return new SmartCodeMatcher(instructions)
				.MatchForward(true,
					new CodeMatch(OpCodes.Ldloc_2),
					new CodeMatch(OpCodes.Ldloc_1),
					new CodeMatch(OpCodes.Ldlen),
					new CodeMatch(OpCodes.Conv_I4),
					new CodeMatch(OpCodes.Blt)
				).Advance(2)
				.InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0, null))
				.Insert(
					new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Thunderstore.Thunderstore), nameof(Thunderstore.Thunderstore.InstallTiles)))
				).InstructionEnumeration();
			}

			/*[HarmonyPatch(typeof(PlayerInputTutorialScript), "UpdateUI")]
			[HarmonyPostfix]
			private static IEnumerable<CodeInstruction> TranspileAddBurstMessage(IEnumerable<CodeInstruction> instructions)
			{
				var codeMatcher = new SmartCodeMatcher(instructions)
				 .MatchForward(true,
					new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(PlayerInputTutorialScript), "show_safety_help")),
					new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(DirtyWatcherBase<bool>), nameof(DirtyWatcherBase<bool>.Value))),
					new CodeMatch(OpCodes.Stloc_S),
					new CodeMatch(OpCodes.Ldloc_S),
					new CodeMatch(OpCodes.Switch)
				 )
				 .Advance(1);

				var endLabel = codeMatcher.Operand;

				codeMatcher
				 .InsertAndAdvance(OpCodes.Ldarg_0);

				codeMatcher.CreateLabel(out var label);

				codeMatcher.Advance(-1).Instruction.labels.Add(label);

				codeMatcher.Advance(2)
					.InsertAndAdvance(
					new CodeInstruction(OpCodes.Ldc_I4, 0x00000381),
					new CodeInstruction(OpCodes.Newarr, typeof(System.String)),
					new CodeInstruction(OpCodes.Dup),
					new CodeInstruction(OpCodes.Ldc_I4_0),
					new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(PlayerInputTutorialScript), nameof(PlayerInputTutorialScript.label_firemode))),
					new CodeInstruction(OpCodes.Stelem_Ref),
					new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PlayerInputTutorialScript), nameof(PlayerInputTutorialScript.FormatUIString))),
					new CodeInstruction(OpCodes.Ldloc_S, 11),
					new CodeInstruction(OpCodes.Ldloc_S, 11),
					new CodeInstruction(OpCodes.Ldc_I4_0),
					new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PlayerInputTutorialScript), "AddDisplayLine")),
					new CodeInstruction(OpCodes.Br, endLabel)
					);


				return codeMatcher.InstructionEnumeration();
			}*/
		}

		#endregion

		#region General Patches

		internal static class GetConsoleColorPatch
		{
			internal static ConsoleColor consoleColor;

			[HarmonyPatch(typeof(LogLevelExtensions), nameof(LogLevelExtensions.GetConsoleColor))]
			[HarmonyPostfix]
			internal static void ChangeGetColorResult(ref ConsoleColor __result)
			{
				__result = consoleColor;
			}
		}

		internal static class LogEventArgsToStringPatch
		{
			internal static string levelName;

			[HarmonyPatch(typeof(LogEventArgs), nameof(LogEventArgs.ToString))]
			[HarmonyPostfix]
			internal static void ChangeLogEventArgsLevel(LogEventArgs __instance, ref string __result)
			{
				__result = string.Format("[{0,-7}:{1,10}] {2}", levelName, __instance.Source.SourceName, __instance.Data);
			}
		}

		[HarmonyPatch(typeof(MagazineScript), "UpdateRoundPositions")]
		[HarmonyPostfix]
		private static void PatchMagazineRoundPositions(ref MagazineScript __instance) {
			if (!(__instance is DoubleStackMagazine)) return;

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

		[HarmonyPatch(typeof(MagazineScript), nameof(MagazineScript.Awake))]
		[HarmonyPostfix]
		private static void UpdateSpringOnAwake(MagazineScript __instance)
		{
			if (__instance.spring)
			{
				__instance.spring.UpdateScale();
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

		public static void PrintMethodIL(Type type, string name, bool original = false)
		{
			PrintMethodIL(AccessTools.Method(type, name), original);
		}

		public static void PrintMethodIL(MethodInfo method, bool original = false)
		{
			var shitfuck = (original) ? PatchProcessor.GetOriginalInstructions(method) : PatchProcessor.GetCurrentInstructions(method);

			var shitfuckCount = shitfuck.Count;
			for (int instructionIndex = 0; instructionIndex < shitfuckCount; instructionIndex++)
			{
				Debug.Log(shitfuck[instructionIndex]);
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

			ModLoader.Finish();

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

			foreach(var ev in ModdingKitEvents.ExecuteOnStartup.GetInvocationList()) {
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
			if (__instance.physics_collided || !(__instance is ModShellCasingScript)) return true;
	
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

		[HarmonyPatch(typeof(MagazineScript), nameof(MagazineScript.Awake))]
		[HarmonyPrefix]
		private static void PatchMagazineAwake(MagazineScript __instance) {
			foreach (var spring in __instance.GetComponentsInChildren<SpringCompressInstance>()) {
				if (spring == null) {
					Debug.LogError("Null spring in magazine \"" + __instance.InternalName + "\"");
					continue;
				}

				if (spring.orig_center.magnitude > 0) {
					continue;
				}
				spring.orig_center = (spring.transform.InverseTransformPoint(spring.new_top.position) + spring.transform.InverseTransformPoint(spring.new_bottom.position)) / 2;
				spring.orig_dist = Vector3.Distance(spring.new_top.position, spring.new_bottom.position);
			}
		}

		[HarmonyPatch(typeof(RuntimeTileLevelGenerator), "SpawnMagazine")]
		[HarmonyPostfix]
		private static void PatchSpawnMagazine(ref ActiveItem __result) {
			if (ModdingKitEvents.ItemSpawnHandlers.ContainsKey(ReceiverCoreScript.Instance().CurrentLoadout.gun_internal_name)) {
				foreach (var spawn_event in ModdingKitEvents.ItemSpawnHandlers[ReceiverCoreScript.Instance().CurrentLoadout.gun_internal_name]) {
					try {
						if (spawn_event.Invoke(ref __result)) {
							break;
						}
					} catch (Exception e) {
						Debug.LogError("Failed invoking item spawn event method " + spawn_event.Method.Name + " for gun " + ReceiverCoreScript.Instance().CurrentLoadout.gun_internal_name + ";\nDumping stack trace:");
						Debug.LogError(e);
					}
				}
			}
		}

		[HarmonyPatch(typeof(PlayerLoadout), nameof(PlayerLoadout.Deserialize))]
		[HarmonyPrefix]
		private static void PatchDeserializeLoadout(JSONNode jn_root) {
			if (jn_root.HasKey("gun_persistent_data")) {
				Extensions.current_gun_data = jn_root["gun_persistent_data"].AsObject;
			}
		}

		[HarmonyPatch(typeof(PlayerLoadout), nameof(PlayerLoadout.Serialize))]
		[HarmonyPostfix]
		private static void PatchSerializeLoadout(ref JSONObject __result) {
			if (LocalAimHandler.player_instance != null && LocalAimHandler.player_instance.TryGetGun(out var gun) && gun is ModGunScript) {
				__result["gun_persistent_data"] = (gun as ModGunScript).EncodeJSON(gun.GetPersistentData());					
			}
		}

		[HarmonyPatch(typeof(PlayerLoadout), nameof(PlayerLoadout.Reserialize))]
		[HarmonyPostfix]
		private static void PatchReserializeLoadout() {
			Extensions.current_gun_data = null;
		}

		[HarmonyPatch(typeof(ReceiverCoreScript), nameof(ReceiverCoreScript.SpawnGun))]
		[HarmonyPostfix]
		private static void PatchSpawnGun(ref GunScript __result) {
			if (__result == null) return;

			if (__result is ModGunScript && (__result as ModGunScript).OwnData(Extensions.current_gun_data)) {
				__result.SetPersistentData((__result as ModGunScript).DecodeJSON(Extensions.current_gun_data));
			}
		}

		[HarmonyPatch(typeof(SettingsMenuScript), nameof(SettingsMenuScript.OnKeybindsClick))]
		[HarmonyPostfix]
		private static void PatchOpenKeybinds() {
			if (LocalAimHandler.player_instance == null || !LocalAimHandler.player_instance.TryGetGun(out var gun)) return;

			KeybindsManager.SetKeybindsActive(gun.weapon_group_name);
		}

		[HarmonyPatch(typeof(RankingProgressionGameMode), nameof(RankingProgressionGameMode.EvaluateNextLoadout))]
		[HarmonyPrefix]
		private static void PatchEvaluateLoadout(ref HashSet<string> ___campaign_loadouts) {
			string[] loadout_names = ReceiverCoreScript.Instance().weapon_loadout_asset.GetAllLoadoutNames();

			___campaign_loadouts.RemoveWhere( loadout_name => {
				bool contains = loadout_names.Contains(loadout_name);

				#if DEBUG

				if (!contains) {
					Debug.Log("Couldn't find loadout " + loadout_name + ", dropping it");
				}

				#endif

				return !contains;
			});
		}

		[HarmonyPatch(typeof(ReceiverCoreScript), nameof(ReceiverCoreScript.GetWorldGenerationConfigurationFromName))]
		[HarmonyPostfix]
		private static void PatchGetWCG(ref WorldGenerationConfiguration __result) {
			if (__result == null) return;

			string[] loadout_names = ReceiverCoreScript.Instance().weapon_loadout_asset.GetAllLoadoutNames();

			__result.loadouts.RemoveAll( loadout => {
				string loadout_name = loadout.name;

				bool contains = loadout_names.Contains(loadout_name);

				#if DEBUG

				if (!contains) {
					Debug.Log("Couldn't find loadout " + loadout_name + ", dropping it");
				}

				#endif

				return !contains;
			});
		}

		[HarmonyPatch(typeof(RankingProgressionCampaign), nameof(RankingProgressionCampaign.LoadFile))]
		[HarmonyPostfix]
		private static void PatchLoadCampaign(string filename) {
			JSONNode campaign_data = ReceiverCoreScript.LoadJSONFile(filename);

			if (campaign_data.HasKey("use_modded_guns") && campaign_data["use_modded_guns"].AsBool && !ModdedGunsCampaignManager.modded_guns_campaigns.Contains(filename)) {
				ModdedGunsCampaignManager.modded_guns_campaigns.Add(filename);
			}
		}

		[HarmonyPatch(typeof(WorldGenerationConfiguration), nameof(WorldGenerationConfiguration.Deserialize))]
		[HarmonyPostfix]
		private static void PatchLoadWCG(ref WorldGenerationConfiguration __result) {
			GameModeBase game_mode = ReceiverCoreScript.Instance().game_mode;

			if (game_mode != null && game_mode is RankingProgressionGameMode) {
				string campaign_path = AccessTools.Field(typeof(RankingProgressionGameMode), "campaign_path").GetValue(game_mode) as string;

				if (ModdedGunsCampaignManager.modded_guns_campaigns.Contains(campaign_path)) {
					string[] loadout_names = ReceiverCoreScript.Instance().weapon_loadout_asset.GetAllLoadoutNames();

					__result.loadouts.RemoveAll( loadout => !loadout_names.Contains(loadout.name) );

					foreach (var loadout_name in loadout_names) {
						var loadout = ReceiverCoreScript.Instance().weapon_loadout_asset.GetLoadoutPrefab(loadout_name);

						if (!loadout.gun_internal_name.StartsWith("wolfire") && !__result.loadouts.Any( loadout_value => loadout_value.name == loadout_name)) {
							__result.loadouts.Add(new LoadoutValue() { name = loadout_name });
						}
					}
				}
			}
		}

		[HarmonyPatch(typeof(LocalAimHandler), nameof(LocalAimHandler.StepRecoil))]
		[HarmonyPostfix]
		private static void PatchStepRecoil() {
			LocalAimHandler lah = LocalAimHandler.player_instance;

			foreach (var hand in lah.hands) {
				if (hand.state == LocalAimHandler.Hand.State.HoldingGun) {
					var gun = hand.slot.contents[0] as ModGunScript;

					if (gun != null) {
						Vector3 last_recoil_event = hand.recoil_events.Last();
						Vector2 multiplier = gun.GetStepRecoilMultiplier();

						last_recoil_event.x *= multiplier.x;
						last_recoil_event.y *= multiplier.y;

						hand.recoil_events[hand.recoil_events.Count - 1] = last_recoil_event;
					}
				}
			}
		}

	// Maybe later
		// [HarmonyPatch(typeof(ReceiverCoreScript), "SpawnPlayer")]
		// [HarmonyPostfix]
		// private static void PatchStartIntro(ReceiverCoreScript __instance) {
		// 	PlayerLoadout loadout = __instance.player.lah.loadout;

		// 	if (
		// 		__instance.game_mode.GetGameMode() == GameMode.RankingCampaign
		// 		&&
		// 		(__instance.game_mode as RankingProgressionGameMode).progression_data.receiver_rank == 0
		// 		&&
		// 		loadout != null
		// 	) {
		// 		var tile_with_gun = RuntimeTileLevelGenerator.instance.GetTiles()[2];

		// 		var gun = tile_with_gun.GetComponentInChildren<GunScript>();

		// 		if (gun != null && gun.InternalName != loadout.gun_internal_name && __instance.TryGetItemPrefab<GunScript>(loadout.gun_internal_name, out var replacement_gun)) {
		// 			Vector3 gun_position = gun.transform.position;
		// 			Quaternion gun_rotation = gun.transform.rotation;

		// 			Debug.Log("Changed gun");

		// 			if (replacement_gun.two_handed) {
		// 				gun_position += Vector3.up * 0.5f;
		// 			}
					
		// 			UnityEngine.Object.DestroyImmediate(gun.gameObject);

		// 			UnityEngine.Object.Instantiate(replacement_gun, gun_position, gun_rotation, tile_with_gun.transform).Move(null);
		// 		}
		// 	}
		// }

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

		private static System.Collections.IEnumerator WaitForModel10ToSpawn()
		{
			var guns = UnityEngine.Object.FindObjectsOfType<GunScript>();
			for (int gunIndex = 0; gunIndex < guns.Length; gunIndex++)
			{
				if (guns[gunIndex].gun_model == GunModel.Model10)
				{
					UnityEngine.Object.Instantiate(ReceiverCoreScript.Instance().GetGunPrefab(ReceiverCoreScript.Instance().CurrentLoadout.gun_internal_name), guns[gunIndex].transform.position, guns[gunIndex].transform.rotation);
					guns[gunIndex].gameObject.SetActive(false);
					yield break;
				}
			}
		}

		internal static void Initialize() {
			HarmonyInstances.Core = Harmony.CreateAndPatchAll(typeof(HarmonyManager));
			HarmonyInstances.Transpilers = Harmony.CreateAndPatchAll(typeof(Transpilers));
			HarmonyInstances.ModHelpEntry = Harmony.CreateAndPatchAll(typeof(ModHelpEntryManager));
			HarmonyInstances.CustomSounds = Harmony.CreateAndPatchAll(typeof(CustomSounds.ModAudioPatches));

			#if DEBUG
			HarmonyInstances.DebugPatches = Harmony.CreateAndPatchAll(typeof(DebugTranspilers));
			HarmonyInstances.DebugPatches.PatchAll(typeof(TransformDebugScope));
			#endif

			if (Thunderstore.Thunderstore.LaunchedWithR2ModMan) {
				HarmonyInstances.Thunderstore = Harmony.CreateAndPatchAll(typeof(R2ModManPatches));
			}

			ModdingKitCorePlugin.instance.StartCoroutine(FixLegacySounds()); //Calling this method has to be delayed to wait for patches from all plugins to get applied
		}
	}
}
