using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using HarmonyLib;
using Receiver2;
using Receiver2ModdingKit.Helpers;
using SimpleJSON;
using UnityEngine;

namespace Receiver2ModdingKit.CustomRounds {
	public static class CustomRoundTypes {
		public static CartridgeSpec.Preset inventory_selected_cartridge = (CartridgeSpec.Preset)int.MaxValue;

		public static List<CustomRoundDefinition> definitions = new List<CustomRoundDefinition>();

		public static Dictionary<CartridgeSpec.Preset, List<CartridgeSpec.Preset>> presets_children_dictionary = new Dictionary<CartridgeSpec.Preset, List<CartridgeSpec.Preset>>();

		public static ShellCasingScript GetRandomRound(CartridgeSpec.Preset baseVariant) {
			Debug.Log(baseVariant);

			if (presets_children_dictionary.TryGetValue(baseVariant, out var children)) {
				float total = 0f;

				//account for base round;
				total += 1f;

				for (int i = 0; i < children.Count; i++) {
					var roundDef = GetDefinitionForRound(children[i]);

					if (roundDef.IsUnlocked) {
						total += roundDef.spawn_chance;
					}
				}

				var randFloat = Random.Range(0f, total);
				Debug.Log("randFloat: " + randFloat);
				Debug.Log(children.Count);

				if (randFloat <= 1.0f) {
					return ModdingKitCorePlugin.GetRoundPrefab(baseVariant);
				}

				float accumBreak = 0.0f;
				for (int i = 0; i < children.Count; i++) {
					var roundDef = GetDefinitionForRound(children[i]);

					if (roundDef.IsUnlocked) {
						accumBreak += GetDefinitionForRound(children[i]).spawn_chance;
					}

					if (randFloat <= accumBreak) {
						return ModdingKitCorePlugin.GetRoundPrefab(children[i]);
					}
				}

				return ModdingKitCorePlugin.GetRoundPrefab(children[children.Count - 1]);
			}
			else {
				return ModdingKitCorePlugin.GetRoundPrefab(baseVariant);
			}
		}

		public static CustomRoundDefinition GetDefinitionForRound(CartridgeSpec.Preset round) {
			return definitions.FirstOrDefault(def => def.cartridge == round);
		}

		public static CartridgeSpec.Preset[] GetSiblingCartridgeTypes(params CartridgeSpec.Preset[] cartridges) {
			List<CartridgeSpec.Preset> presets = new List<CartridgeSpec.Preset>();

			foreach (var cartridge in cartridges) {
				if (presets_children_dictionary.TryGetValue(cartridge, out var children)) {
					presets.AddRange(children);
				}
			}

			return presets.ToArray();
		}

		public static void RegisterCustomRound(CustomRoundDefinition definition) {
			if (definition == null) {
				throw new System.NullReferenceException("CustomRoundDefinition is null");
			}

			if (presets_children_dictionary.TryGetValue(definition.baseVariant, out var presets)) {
				presets.Add(definition.cartridge);
			}
			else {
				presets_children_dictionary[definition.baseVariant] = new List<CartridgeSpec.Preset>() { definition.cartridge };
			}

			definitions.Add(definition);
		}

		public static void SpawnRoundInChamber(GunScript gunScript, CartridgeSpec.Preset cartridge_type) {
			if (gunScript.transform_chambered_round != null)
			{
				gunScript.round_in_chamber = Object.Instantiate(ModdingKitCorePlugin.GetRoundPrefab(cartridge_type), gunScript.transform_chambered_round.position, gunScript.transform_chambered_round.rotation);
			}
			else
			{
				gunScript.round_in_chamber = Object.Instantiate(ModdingKitCorePlugin.GetRoundPrefab(cartridge_type));
			}
			gunScript.round_in_chamber.transform.parent = gunScript.transform;
			gunScript.round_in_chamber.transform.localScale = Vector3.one;
			gunScript.round_in_chamber.Move(gunScript.GetComponent<InventorySlot>());
		}

		internal static class Patches {
			private static readonly CodeInstruction[] insertGetRandomRoundInstructions = {
				new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GameObject), nameof(GameObject.GetComponent), null, new System.Type[] { typeof(ShellCasingScript) })),
				new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ShellCasingScript), nameof(ShellCasingScript.cartridge_type))),
				new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CustomRoundTypes), nameof(CustomRoundTypes.GetRandomRound))),
				new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Component), nameof(Component.gameObject)))
			};

			[HarmonyPatch(typeof(Balloon), "DoPop")]
			[HarmonyTranspiler]
			private static IEnumerable<CodeInstruction> PatchBalloonPopping(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator, MethodBase __originalMethod) {
				var singleArgGenericInfo = typeof(Object).GetMethods(AccessTools.all).First(mi => mi.GetGenericArguments().Length == 1 && mi.GetParameters().Length == 1).MakeGenericMethod(typeof(GameObject));

				var codeMatcher = new SmartCodeMatcher(instructions, iLGenerator)
					.MatchForward(false, 
						new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(ReceiverCoreScript), nameof(ReceiverCoreScript.Instance))),
						new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ReceiverCoreScript), nameof(ReceiverCoreScript.RoundPrefab))),
						new CodeMatch(OpCodes.Call, singleArgGenericInfo),
						new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(GameObject), nameof(GameObject.GetComponent), null, new System.Type[] { typeof(ShellCasingScript) })),
						new CodeMatch(OpCodes.Stloc_2)
						)
					.Advance(2)
					.InsertAndAdvance(
						insertGetRandomRoundInstructions
								     )
					;

				return codeMatcher.InstructionEnumeration();
			}

			[HarmonyPatch(typeof(BossPlatformElevatorScript), nameof(BossPlatformElevatorScript.Spawn))]
			[HarmonyTranspiler]
			private static IEnumerable<CodeInstruction> PatchBossPlatformScriptSpawn(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator, MethodBase __originalMethod) {
				var instantiateMethodInfo = typeof(Object).GetMethods(AccessTools.all).First(mi => mi.GetGenericArguments().Length == 1 && mi.GetParameters().Length == 3 && mi.GetParameters()[1].ParameterType == typeof(Vector3) && mi.GetParameters()[2].ParameterType == typeof(Quaternion)).MakeGenericMethod(typeof(GameObject));

				var codeMatcher = new SmartCodeMatcher(instructions, iLGenerator)
					.MatchForward(false,
						new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(ReceiverCoreScript), nameof(ReceiverCoreScript.Instance))),
						new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ReceiverCoreScript), nameof(ReceiverCoreScript.RoundPrefab))),
						new CodeMatch(OpCodes.Ldloc_S),
						new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(Random), nameof(Random.rotation))),
						new CodeMatch(OpCodes.Call, instantiateMethodInfo)
					)
					.Advance(2)
					.InsertAndAdvance(
						insertGetRandomRoundInstructions
									 )
					;

				return codeMatcher.InstructionEnumeration();
			}

			[HarmonyPatch(typeof(ClassicBulletPileScript), nameof(ClassicBulletPileScript.Start))]
			[HarmonyTranspiler]
			private static IEnumerable<CodeInstruction> PatchClassicBulletPileScript(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator, MethodBase __originalMethod) {
				var instantiateMethodInfo = typeof(Object).GetMethods(AccessTools.all).First(mi => mi.GetGenericArguments().Length == 1 && mi.GetParameters().Length == 3 && mi.GetParameters()[1].ParameterType == typeof(Vector3) && mi.GetParameters()[2].ParameterType == typeof(Quaternion)).MakeGenericMethod(typeof(GameObject));

				var codeMatcher = new SmartCodeMatcher(instructions, iLGenerator)
					.MatchForward(false,
						new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(ReceiverCoreScript), nameof(ReceiverCoreScript.Instance))),
						new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ReceiverCoreScript), nameof(ReceiverCoreScript.RoundPrefab))),
						new CodeMatch(OpCodes.Ldloc_S),
						new CodeMatch(OpCodes.Ldloc_S),
						new CodeMatch(OpCodes.Call, instantiateMethodInfo)
					)
					.Advance(2)
					.InsertAndAdvance(
						insertGetRandomRoundInstructions
									 )
					;

				return codeMatcher.InstructionEnumeration();
			}

			[HarmonyPatch(typeof(Pumpkin), nameof(Pumpkin.OnShot))]
			[HarmonyTranspiler]
			private static IEnumerable<CodeInstruction> PatchPumpkinOnShot(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator, MethodBase __originalMethod) {
				var singleArgGenericInfo = typeof(Object).GetMethods(AccessTools.all).First(mi => mi.GetGenericArguments().Length == 1 && mi.GetParameters().Length == 1).MakeGenericMethod(typeof(GameObject));

				var codeMatcher = new SmartCodeMatcher(instructions, iLGenerator)
					.MatchForward(false,
						new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(ReceiverCoreScript), nameof(ReceiverCoreScript.Instance))),
						new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ReceiverCoreScript), nameof(ReceiverCoreScript.RoundPrefab))),
						new CodeMatch(OpCodes.Call, singleArgGenericInfo),
						new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(GameObject), nameof(GameObject.GetComponent), null, new System.Type[] { typeof(ShellCasingScript) })),
						new CodeMatch(OpCodes.Stloc_2)
					)
					.Advance(2)
					.InsertAndAdvance(
						insertGetRandomRoundInstructions
									 )
					;

					return codeMatcher.InstructionEnumeration();
			}

			[HarmonyPatch(typeof(RuntimeTileLevelGenerator), nameof(RuntimeTileLevelGenerator.InstantiateRound))]
			[HarmonyTranspiler]
			private static IEnumerable<CodeInstruction> PatchRTLGInstantiateRound(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator, MethodBase __originalMethod) {
				var codeMatcher = new SmartCodeMatcher(instructions, iLGenerator)
					.MatchForward(false,
						new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(ReceiverCoreScript), nameof(ReceiverCoreScript.Instance))),
						new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ReceiverCoreScript), nameof(ReceiverCoreScript.RoundPrefab))),
						new CodeMatch(OpCodes.Ldarg_1),
						new CodeMatch(OpCodes.Ldarg_2),
						new CodeMatch(OpCodes.Stloc_0)
					)
					.Advance(2)
					.InsertAndAdvance(
						insertGetRandomRoundInstructions
									 )
					;

					return codeMatcher.InstructionEnumeration();
			}

			[HarmonyPatch(typeof(AmmoBoxScript), "Start")]
			[HarmonyTranspiler]
			private static IEnumerable<CodeInstruction> PatchAmmoBoxStart(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator, MethodBase __originalMethod) {
				var codeMatcher = new SmartCodeMatcher(instructions, iLGenerator)
					.MatchForward(false,
						new CodeMatch(OpCodes.Ldarg_0),
						new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(AmmoBoxScript), nameof(AmmoBoxScript.round_prefab))),
						new CodeMatch(OpCodes.Ldloc_1),
						new CodeMatch(OpCodes.Ldc_R4, 0.0f),
						new CodeMatch(OpCodes.Ldc_R4, 0.0f),
						new CodeMatch(OpCodes.Ldc_R4, 360.0f),
						new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Random), nameof(Random.RandomRange), new System.Type[] { typeof(float), typeof(float) }))
					)
					.Advance(2)
					.InsertAndAdvance(
						insertGetRandomRoundInstructions
									 )
					;

				return codeMatcher.InstructionEnumeration();
			}

			[HarmonyPatch(typeof(ChamberState), nameof(ChamberState.SpawnBulletOnChamber))]
			[HarmonyTranspiler]
			private static IEnumerable<CodeInstruction> PatchSpawnBulletOnChamber(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator, MethodBase __originalMethod) {
				var codeMatcher = new SmartCodeMatcher(instructions, iLGenerator)
					.MatchForward(false,
						new CodeMatch(OpCodes.Ldarg_0),
						new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(ChamberState), "cylinder")),
						new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GunScript), nameof(GunScript.loaded_cartridge_prefab)))
					)
					.Advance(4)
					.InsertAndAdvance(
						insertGetRandomRoundInstructions
									 )
					;
					
				return codeMatcher.InstructionEnumeration();
			}

			//replace whole ass method with ours, not like it mattered anyways, count all rounds individually instead of trying to be "smart" about it, ergh
			[HarmonyPatch(typeof(GunScript), nameof(GunScript.GetTotalMass))]
			[HarmonyPrefix]
			private static bool PatchGunGetTotalMass(GunScript __instance, ref float __result) {
				float gun_mass = __instance.mass;

				var additional_mass = 0.0f;

				var cartridgeSpec = default(CartridgeSpec);

				if (__instance.round_in_chamber != null) {
					cartridgeSpec.SetFromPreset(__instance.round_in_chamber.cartridge_type);

					additional_mass += cartridgeSpec.extra_mass;

					if (!__instance.round_in_chamber.IsSpent()) {
						additional_mass += cartridgeSpec.mass;
					}
				}

				if (__instance.magazine_instance_in_gun != null) {
					gun_mass += 0.1f;

					for (int roundIndex = 0; roundIndex < __instance.magazine_instance_in_gun.rounds.Count; roundIndex++) {
						cartridgeSpec.SetFromPreset(__instance.magazine_instance_in_gun.rounds[roundIndex].cartridge_type);

						additional_mass += cartridgeSpec.extra_mass;

						//why not
						if (!__instance.magazine_instance_in_gun.rounds[roundIndex].IsSpent()) {
							additional_mass += cartridgeSpec.mass;
						}
					}
				}

				if (__instance.HasCylinder()) {
					for (int cylinderIndex = 0; cylinderIndex < __instance.cylinder.chambers.Length; cylinderIndex++) { 
						var chamber = __instance.cylinder.chambers[cylinderIndex];
						
						if (chamber.HasBullet()) {
							cartridgeSpec.SetFromPreset(chamber.GetBullet().cartridge_type);

							additional_mass += cartridgeSpec.extra_mass;

							if (!chamber.GetBullet().IsSpent()) {
								additional_mass += cartridgeSpec.mass;
							}
						}
					}
				}

				__result = gun_mass + additional_mass * 0.001f;

				return false;
			}

			[HarmonyPatch(typeof(GunScript), nameof(GunScript.FireBullet))]
			[HarmonyTranspiler]
			private static IEnumerable<CodeInstruction> AddExtraDoubleFeedChances(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator, MethodBase __originalMethod) {
				var codeMatcher = new SmartCodeMatcher(instructions, iLGenerator);

				var extraDoubleFeedProbaLocal = iLGenerator.DeclareLocal(typeof(float));
				var extraWedgeAmountLocal = iLGenerator.DeclareLocal(typeof(float));

				codeMatcher
					.MatchForward(false,
						new CodeMatch(OpCodes.Ldarg_0),
						new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(GunScript), nameof(GunScript.CanMalfunction))),
						new CodeMatch(OpCodes.Brfalse),
						new CodeMatch(OpCodes.Ldarg_0),
						new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GunScript), nameof(GunScript.gun_type))),
						new CodeMatch(OpCodes.Ldc_I4_1),
						new CodeMatch(OpCodes.Beq)
					)
					.CreateLabel(out var ifNoneLabel)
					.InsertAndAdvance(
						new CodeInstruction(OpCodes.Ldc_R4, 0.0f),
						new CodeInstruction(OpCodes.Stloc_S, extraDoubleFeedProbaLocal.LocalIndex),
						new CodeInstruction(OpCodes.Ldarg_1),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ShellCasingScript), nameof(ShellCasingScript.cartridge_type))),
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CustomRoundTypes), nameof(CustomRoundTypes.GetDefinitionForRound))),
						new CodeInstruction(OpCodes.Brfalse_S, ifNoneLabel),
						new CodeInstruction(OpCodes.Ldarg_1),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ShellCasingScript), nameof(ShellCasingScript.cartridge_type))),
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CustomRoundTypes), nameof(CustomRoundTypes.GetDefinitionForRound))),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CustomRoundDefinition), nameof(CustomRoundDefinition.extra_doublefeed_chance))),
						new CodeInstruction(OpCodes.Stloc_S, extraDoubleFeedProbaLocal.LocalIndex)
					)
					.MatchForward(true,
						new CodeMatch(OpCodes.Ldc_R4, 1f),
						new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Random), nameof(Random.Range), new System.Type[] { typeof(float), typeof(float) })),
						new CodeMatch(OpCodes.Ldarg_0),
						new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GunScript), nameof(GunScript.doubleFeedProbability))),
						new CodeMatch(OpCodes.Blt)
					)
					.InsertAndAdvance(
						new CodeInstruction(OpCodes.Ldloc_S, extraDoubleFeedProbaLocal.LocalIndex),
						new CodeInstruction(OpCodes.Add)
					)
					.MatchForward(false,
						new CodeMatch(OpCodes.Ldloc_S),
						new CodeMatch(OpCodes.Dup),
						new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(ChamberState), nameof(ChamberState.wedged))),
						new CodeMatch(OpCodes.Ldc_R4, 0.0f),
						new CodeMatch(OpCodes.Ldc_R4, 0.5f),
						new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Random), nameof(Random.Range), new System.Type[] { typeof(float), typeof(float) })),
						new CodeMatch(OpCodes.Add),
						new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(ChamberState), nameof(ChamberState.wedged)))
					)
					.CreateLabel(out var ifOtherNoneLabel)
					.InsertAndAdvance(
						new CodeInstruction(OpCodes.Ldc_R4, 0.0f),
						new CodeInstruction(OpCodes.Stloc_S, extraWedgeAmountLocal.LocalIndex),
						new CodeInstruction(OpCodes.Ldarg_1),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ShellCasingScript), nameof(ShellCasingScript.cartridge_type))),
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CustomRoundTypes), nameof(CustomRoundTypes.GetDefinitionForRound))),
						new CodeInstruction(OpCodes.Brfalse_S, ifOtherNoneLabel),
						new CodeInstruction(OpCodes.Ldarg_1),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ShellCasingScript), nameof(ShellCasingScript.cartridge_type))),
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CustomRoundTypes), nameof(CustomRoundTypes.GetDefinitionForRound))),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CustomRoundDefinition), nameof(CustomRoundDefinition.extra_wedged_amount))),
						new CodeInstruction(OpCodes.Stloc_S, extraWedgeAmountLocal.LocalIndex)
					)
					.MatchForward(true,
						new CodeMatch(OpCodes.Ldloc_S),
						new CodeMatch(OpCodes.Dup),
						new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(ChamberState), nameof(ChamberState.wedged))),
						new CodeMatch(OpCodes.Ldc_R4, 0.0f),
						new CodeMatch(OpCodes.Ldc_R4, 0.5f),
						new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Random), nameof(Random.Range), new System.Type[] { typeof(float), typeof(float) })),
						new CodeMatch(OpCodes.Add)
					)
					.Insert(
						new CodeInstruction(OpCodes.Ldloc_S, extraWedgeAmountLocal.LocalIndex),
						new CodeInstruction(OpCodes.Add)
					)
					;

				return codeMatcher.InstructionEnumeration();
			}

			[HarmonyPatch(typeof(GunScript), "UpdateSlide")]
			[HarmonyTranspiler]
			private static IEnumerable<CodeInstruction> ForTheLoveOfGodAddStovePipeProbabilities(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
				var extraStovepipeProbabilityLocal = generator.DeclareLocal(typeof(float));

				return new SmartCodeMatcher(instructions, generator)
					.MatchForward(false,
						new CodeMatch(OpCodes.Ldarg_0),
						new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GunScript), nameof(GunScript.round_in_chamber))),
						new CodeMatch(OpCodes.Ldnull),
						new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Object), "op_Inequality")),
						new CodeMatch((instr) => instr.Branches(out _)),

						new CodeMatch(OpCodes.Ldarg_0),
						new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GunScript), nameof(GunScript.transform_chambered_round))),
						new CodeMatch(OpCodes.Ldnull)
					)
					.Advance(5)
					.CreateLabel(out var ifNoneLabel)
					.Insert(
						//extraStovepipeProbability = 0.0f;
						new CodeInstruction(OpCodes.Ldc_R4, 0.0f),
						new CodeInstruction(OpCodes.Stloc_S, extraStovepipeProbabilityLocal.LocalIndex),

						//if (CustomRoundTypes.GetDefinitionForRound(this.round_in_chamber.cartridge_type) != null)
						new CodeInstruction(OpCodes.Ldarg_0),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(GunScript), nameof(GunScript.round_in_chamber))),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ShellCasingScript), nameof(ShellCasingScript.cartridge_type))),
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CustomRoundTypes), nameof(CustomRoundTypes.GetDefinitionForRound))),
						new CodeInstruction(OpCodes.Brfalse_S, ifNoneLabel),

						//extraStovepipeProbability = CustomRoundTypes.GetDefinitionForRound(this.round_in_chamber.cartridge_type).extra_stovepipe_chance;
						new CodeInstruction(OpCodes.Ldarg_0),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(GunScript), nameof(GunScript.round_in_chamber))),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ShellCasingScript), nameof(ShellCasingScript.cartridge_type))),
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CustomRoundTypes), nameof(CustomRoundTypes.GetDefinitionForRound))),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CustomRoundDefinition), nameof(CustomRoundDefinition.extra_stovepipe_chance))),
						new CodeInstruction(OpCodes.Stloc_S, extraStovepipeProbabilityLocal.LocalIndex)
					)
					.MatchForward(true,
						new CodeMatch(OpCodes.Ldarg_0),
						new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(GunScript), "StovePipeProbability"))
					)
					.Advance(1)
					.Insert(
						new CodeInstruction(OpCodes.Ldloc_S, extraStovepipeProbabilityLocal.LocalIndex),
						new CodeInstruction(OpCodes.Add)
					)
					.MatchForward(true,
						new CodeMatch(OpCodes.Ldarg_0),
						new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(GunScript), "StovePipeProbability"))
					)
					.Advance(1)
					.Insert(
						new CodeInstruction(OpCodes.Ldloc_S, extraStovepipeProbabilityLocal.LocalIndex),
						new CodeInstruction(OpCodes.Add)
					)
					.InstructionEnumeration();
			}

			[HarmonyPatch(typeof(GunScript), nameof(GunScript.FireBullet))]
			[HarmonyPostfix]
			private static void AddExtraRecoil(GunScript __instance, ShellCasingScript round) {
				var def = GetDefinitionForRound(round.cartridge_type);

				if (def != null) {
					__instance.rotation_transfer_x += Random.Range(0, def.extra_rotation_x * Mathf.Sign(__instance.rotation_transfer_x));
					__instance.rotation_transfer_y += def.extra_rotation_y;
					__instance.recoil_transfer_x -= def.extra_recoil_x;
					__instance.recoil_transfer_y += Random.Range(0, def.extra_recoil_y * Mathf.Sign(__instance.recoil_transfer_y));
					__instance.slide.vel += def.extra_slide_fire_speed;
				}
			}

			[HarmonyPatch(typeof(LocalAimHandler), "GetLastMatchingLooseBullet")]
			[HarmonyPrefix]
			private static void AddCompatibleDimensions(ref CartridgeSpec.Preset[] cartridge_dimensions) {
				var original_size = cartridge_dimensions.Length;

				var extra = GetSiblingCartridgeTypes(cartridge_dimensions);

				if (inventory_selected_cartridge != (CartridgeSpec.Preset)int.MaxValue) {
					//selected cartridge is compatible vanilla cartridge
					foreach (var dimension in cartridge_dimensions) {
						if (dimension == inventory_selected_cartridge) {
							cartridge_dimensions = new CartridgeSpec.Preset[] { dimension };
							return;
						}
					}

					//selected cartridge is compatible child cartridge
					foreach (var extra_dimension in extra) {
						if (extra_dimension == inventory_selected_cartridge) {
							cartridge_dimensions = new CartridgeSpec.Preset[] { extra_dimension };
							return;
						}
					}

					//selected cartridge isn't compatible      
					cartridge_dimensions = System.Array.Empty<CartridgeSpec.Preset>();
					return;
				}

				if (extra != null && extra.Length > 0) {
					System.Array.Resize(ref cartridge_dimensions, cartridge_dimensions.Length + extra.Length);

					for (int i = original_size; i < cartridge_dimensions.Length; i++) {
						cartridge_dimensions[i] = extra[i - original_size];
					}
				}
			}

			//FUCKED: I think this method might have gotten inlined? can't fucking patch it for shit?
			//we're guaranteed to have a round in the chamber for stovepipes
			// [HarmonyPatch(typeof(GunScript), "StovePipeProbability")]
			// [HarmonyPostfix]
			// [HarmonyDebug]
			// private static void AddExtraStovePipeProbability(GunScript __instance, ref float __result) {
			// 	Debug.LogError("stovepipe proba being checked");

			// 	if (__instance.round_in_chamber != null) {
			// 		Debug.Log("round in chamber isn't null");

			// 		var round_type = GetDefinitionForRound(__instance.round_in_chamber.cartridge_type);

			// 		if (round_type != null) {
			// 			Debug.Log("definition for round isn't null");
			// 			__result += round_type.extra_stovepipe_chance;
			// 		}
			// 	}

			// 	Debug.Log("stovepipe chance: " + __result.ToString());
			// }

			[HarmonyPatch(typeof(GunScript), "Update")]
			[HarmonyTranspiler]
			private static IEnumerable<CodeInstruction> AddExtraWronglySeatedMagProbability(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
				var extraWronglySeatedmagProbabilityLocal = generator.DeclareLocal(typeof(float));

				return new SmartCodeMatcher(instructions, generator)
					.MatchForward(false,
						new CodeMatch(OpCodes.Ldarg_0),
						new CodeMatch(OpCodes.Ldarg_0),
						new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(GunScript), "WronglySeatedMagProbability"))
					)
					.CreateLabel(out var ifNoneLabel)
					.InsertAndAdvance(
						//extraStovepipeProbability = 0.0f;
						new CodeInstruction(OpCodes.Ldc_R4, 0.0f),
						new CodeInstruction(OpCodes.Stloc_S, extraWronglySeatedmagProbabilityLocal.LocalIndex),

						//magazine_instance_in_gun was already nullchecked in an outer scope
						//if (this.magazine_instance_in_gun.NumRounds() > 0)
						new CodeInstruction(OpCodes.Ldarg_0),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(GunScript), nameof(GunScript.magazine_instance_in_gun))),
						new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(MagazineScript), nameof(MagazineScript.NumRounds))),
						new CodeInstruction(OpCodes.Brfalse_S, ifNoneLabel),

						//if (GetDefinitionForRound(this.magazine_instance_in_gun.rounds[0]) != null)
						new CodeInstruction(OpCodes.Ldarg_0),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(GunScript), nameof(GunScript.magazine_instance_in_gun))),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(MagazineScript), nameof(MagazineScript.rounds))),
						new CodeInstruction(OpCodes.Ldc_I4_0),
						new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(List<ShellCasingScript>), "Item")),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ShellCasingScript), nameof(ShellCasingScript.cartridge_type))),
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CustomRoundTypes), nameof(CustomRoundTypes.GetDefinitionForRound))),
						new CodeInstruction(OpCodes.Brfalse_S, ifNoneLabel),

						//extraWronglySeatedMagProbability = CustomRoundTypes.GetDefinitionForRound(this.magazine_instance_in_gun.rounds[0].cartridge_type).extra_wrongly_seated_mag_chance;
						new CodeInstruction(OpCodes.Ldarg_0),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(GunScript), nameof(GunScript.magazine_instance_in_gun))),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(MagazineScript), nameof(MagazineScript.rounds))),
						new CodeInstruction(OpCodes.Ldc_I4_0),
						new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(List<ShellCasingScript>), "Item")),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ShellCasingScript), nameof(ShellCasingScript.cartridge_type))),
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CustomRoundTypes), nameof(CustomRoundTypes.GetDefinitionForRound))),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CustomRoundDefinition), nameof(CustomRoundDefinition.extra_wrongly_seated_mag_chance))),
						new CodeInstruction(OpCodes.Stloc_S, extraWronglySeatedmagProbabilityLocal.LocalIndex)
					)
					.Advance(3)
					.Insert(
						new CodeInstruction(OpCodes.Ldloc_S, extraWronglySeatedmagProbabilityLocal.LocalIndex),
						new CodeInstruction(OpCodes.Add)
					)
					.InstructionEnumeration();
			}

			[HarmonyPatch(typeof(GunScript), "UpdateSlide")]
			[HarmonyTranspiler]
			private static IEnumerable<CodeInstruction> AddOutOfBatteryChance(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
				var extraOutOfBatteryProbabilityLocal = generator.DeclareLocal(typeof(float));

				var codeMatcher = new SmartCodeMatcher(instructions, generator);

				var labels = codeMatcher
					.MatchForward(false,
						new CodeMatch(OpCodes.Ldarg_0),
						new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(GunScript), nameof(GunScript.CanMalfunction))),
						new CodeMatch(instr => instr.Branches(out _)),

						new CodeMatch(OpCodes.Ldarg_0),
						new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GunScript), nameof(GunScript.malfunction))),
						new CodeMatch(instr => instr.Branches(out _)),

						new CodeMatch(OpCodes.Ldc_R4, 0.0f),
						new CodeMatch(OpCodes.Ldc_R4, 1.0f),
						new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Random), nameof(Random.Range), new System.Type[] { typeof(float), typeof(float) })),
						new CodeMatch(OpCodes.Ldarg_0),
						new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(GunScript), "OutOfBatteryProbability"))
					).Instruction.ExtractLabels();

				codeMatcher
					.CreateLabel(out var ifNoneLabel)
					.Insert(
						//extraOutOfBatteryProbability = 0.0f;
						new CodeInstruction(OpCodes.Ldc_R4, 0.0f),
						new CodeInstruction(OpCodes.Stloc_S, extraOutOfBatteryProbabilityLocal.LocalIndex),

						//if (this.magazine_instance_in_gun != null)
						new CodeInstruction(OpCodes.Ldarg_0),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(GunScript), nameof(GunScript.magazine_instance_in_gun))),
						new CodeInstruction(OpCodes.Ldnull),
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Object), "op_Inequality")),
						new CodeInstruction(OpCodes.Brfalse, ifNoneLabel),

						//if (this.magazine_instance_in_gun.NumRounds() > 0)
						new CodeInstruction(OpCodes.Ldarg_0),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(GunScript), nameof(GunScript.magazine_instance_in_gun))),
						new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(MagazineScript), nameof(MagazineScript.NumRounds))),
						new CodeInstruction(OpCodes.Brfalse_S, ifNoneLabel),

						//if (GetDefinitionForRound(this.magazine_instance_in_gun.rounds[0].cartridge_type) != null)
						new CodeInstruction(OpCodes.Ldarg_0),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(GunScript), nameof(GunScript.magazine_instance_in_gun))),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(MagazineScript), nameof(MagazineScript.rounds))),
						new CodeInstruction(OpCodes.Ldc_I4_0),
						new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(List<ShellCasingScript>), "Item")),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ShellCasingScript), nameof(ShellCasingScript.cartridge_type))),
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CustomRoundTypes), nameof(CustomRoundTypes.GetDefinitionForRound))),
						new CodeInstruction(OpCodes.Brfalse_S, ifNoneLabel),

						//extraOutOfBatteryProbability = CustomRoundTypes.GetDefinitionForRound(this.magazine_instance_in_gun.rounds[0].cartridge_type).extra_out_of_battery_chance;
						new CodeInstruction(OpCodes.Ldarg_0),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(GunScript), nameof(GunScript.magazine_instance_in_gun))),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(MagazineScript), nameof(MagazineScript.rounds))),
						new CodeInstruction(OpCodes.Ldc_I4_0),
						new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(List<ShellCasingScript>), "Item")),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ShellCasingScript), nameof(ShellCasingScript.cartridge_type))),
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CustomRoundTypes), nameof(CustomRoundTypes.GetDefinitionForRound))),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CustomRoundDefinition), nameof(CustomRoundDefinition.extra_out_of_battery_chance))),
						new CodeInstruction(OpCodes.Stloc_S, extraOutOfBatteryProbabilityLocal.LocalIndex))
					.AddLabels(labels)
					;

				codeMatcher
					.MatchForward(true,
						new CodeMatch(OpCodes.Ldarg_0),
						new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(GunScript), nameof(GunScript.CanMalfunction))),
						new CodeMatch(instr => instr.Branches(out _)),

						new CodeMatch(OpCodes.Ldarg_0),
						new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GunScript), nameof(GunScript.malfunction))),
						new CodeMatch(instr => instr.Branches(out _)),

						new CodeMatch(OpCodes.Ldc_R4, 0.0f),
						new CodeMatch(OpCodes.Ldc_R4, 1.0f),
						new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Random), nameof(Random.Range), new System.Type[] { typeof(float), typeof(float) })),
						new CodeMatch(OpCodes.Ldarg_0),
						new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(GunScript), "OutOfBatteryProbability"))
					)
					.Advance(1)
					.Insert(
						new CodeInstruction(OpCodes.Ldloc_S, extraOutOfBatteryProbabilityLocal.LocalIndex),
						new CodeInstruction(OpCodes.Add)
					)
					;

				return codeMatcher.InstructionEnumeration();
			}

			[HarmonyPatch(typeof(GunScript), "UpdateSlide")]
			[HarmonyTranspiler]
			private static IEnumerable<CodeInstruction> AddSlamfireChance(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
				var extraSlamfireProbabilityLocal = generator.DeclareLocal(typeof(float));

				var codeMatcher = new SmartCodeMatcher(instructions, generator);

				var labels = codeMatcher
					.MatchForward(false,
						new CodeMatch(OpCodes.Ldloc_S),
						new CodeMatch(OpCodes.Ldc_R4, 0.8f),
						new CodeMatch(instr => instr.Branches(out _)),

						new CodeMatch(OpCodes.Ldarg_0),
						new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GunScript), nameof(GunScript.malfunction))),
						new CodeMatch(instr => instr.Branches(out _)),

						new CodeMatch(OpCodes.Ldarg_0),
						new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GunScript), nameof(GunScript.round_in_chamber))),
						new CodeMatch(OpCodes.Ldnull),
						new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Object), "op_Inequality")),
						new CodeMatch(instr => instr.Branches(out _)),

						new CodeMatch(OpCodes.Ldarg_0),
						new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GunScript), nameof(GunScript.slamfire_probability)))
					)
					.Instruction.ExtractLabels()
					;

				codeMatcher
					.CreateLabel(out var ifNoneLabel)
					.Insert(
						//extraOutOfBatteryProbability = 0.0f;
						new CodeInstruction(OpCodes.Ldc_R4, 0.0f),
						new CodeInstruction(OpCodes.Stloc_S, extraSlamfireProbabilityLocal.LocalIndex),

						//if (this.round_in_chamber != null)
						new CodeInstruction(OpCodes.Ldarg_0),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(GunScript), nameof(GunScript.round_in_chamber))),
						new CodeInstruction(OpCodes.Ldnull),
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Object), "op_Inequality")),
						new CodeInstruction(OpCodes.Brfalse, ifNoneLabel),

						//if (GetDefinitionForRound(this.round_in_chamber.cartridge_type) != null)
						new CodeInstruction(OpCodes.Ldarg_0),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(GunScript), nameof(GunScript.round_in_chamber))),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ShellCasingScript), nameof(ShellCasingScript.cartridge_type))),
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CustomRoundTypes), nameof(CustomRoundTypes.GetDefinitionForRound))),
						new CodeInstruction(OpCodes.Brfalse_S, ifNoneLabel),

						//extraOutOfBatteryProbability = CustomRoundTypes.GetDefinitionForRound(this.magazine_instance_in_gun.rounds[0].cartridge_type).extra_out_of_battery_chance;
						new CodeInstruction(OpCodes.Ldarg_0),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(GunScript), nameof(GunScript.round_in_chamber))),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ShellCasingScript), nameof(ShellCasingScript.cartridge_type))),
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CustomRoundTypes), nameof(CustomRoundTypes.GetDefinitionForRound))),
						new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CustomRoundDefinition), nameof(CustomRoundDefinition.extra_slamfire_chance))),
						new CodeInstruction(OpCodes.Stloc_S, extraSlamfireProbabilityLocal.LocalIndex))
					.AddLabels(labels)
					;

				Debug.Log($"labels: {codeMatcher.Instruction.labels.Count}");

				codeMatcher
					.MatchForward(true,
						new CodeMatch(OpCodes.Ldloc_S),
						new CodeMatch(OpCodes.Ldc_R4, 0.8f),
						new CodeMatch(instr => instr.Branches(out _)),

						new CodeMatch(OpCodes.Ldarg_0),
						new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GunScript), nameof(GunScript.malfunction))),
						new CodeMatch(instr => instr.Branches(out _)),

						new CodeMatch(OpCodes.Ldarg_0),
						new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GunScript), nameof(GunScript.round_in_chamber))),
						new CodeMatch(OpCodes.Ldnull),
						new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Object), "op_Inequality")),
						new CodeMatch(instr => instr.Branches(out _)),

						new CodeMatch(OpCodes.Ldarg_0),
						new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GunScript), nameof(GunScript.slamfire_probability)))
					)
					.Advance(1)
					.Insert(
						new CodeInstruction(OpCodes.Ldloc_S, extraSlamfireProbabilityLocal.LocalIndex),
						new CodeInstruction(OpCodes.Add)
					)
					;

				return codeMatcher.InstructionEnumeration();
			}

			//FUCKED: original method gets inlined
			// [HarmonyPatch(typeof(GunScript), "WronglySeatedMagProbability")]
			// [HarmonyPostfix]
			// private static void AddExtraWronglySeatedMagProbability(GunScript __instance, ref float __result) {
			// 	if (__instance.magazine_instance_in_gun != null) {
			// 		if (__instance.magazine_instance_in_gun.NumRounds() > 0) {
			// 			var round_type = GetDefinitionForRound(__instance.magazine_instance_in_gun.rounds[0].cartridge_type);

			// 			if (round_type != null) {
			// 				__result += round_type.extra_wrongly_seated_mag_chance;
			// 			}
			// 		}
			// 	}
			// }

			[HarmonyPatch(typeof(GunScript), "GetFailureToFeedProbability")]
			[HarmonyPostfix]
			private static void AddExtraFailureToFeedProbability(GunScript __instance, ref float __result) {
				if (__instance.magazine_instance_in_gun != null) {
					if (__instance.magazine_instance_in_gun.NumRounds() > 0) {
						var round_type = GetDefinitionForRound(__instance.magazine_instance_in_gun.rounds[0].cartridge_type);

						if (round_type != null) {
							__result += round_type.extra_ftf_chance;
						}
					}
				}
			}

			readonly static FieldInfo _lah_loose_bullets = typeof(LocalAimHandler).GetField("loose_bullets", BindingFlags.NonPublic | BindingFlags.Instance);
			readonly static MethodInfo _list_get_count_info = _lah_loose_bullets.FieldType.GetProperty("Count", BindingFlags.Public | BindingFlags.Instance).GetMethod;
			readonly static MethodInfo _list_get_item_info = _lah_loose_bullets.FieldType.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance).GetMethod;
			readonly static FieldInfo _boolet_display_spring_info = _lah_loose_bullets.FieldType.GetGenericArguments()[0].GetField("spring", BindingFlags.Public | BindingFlags.Instance);
			readonly static FieldInfo _boolet_display_item_info = _lah_loose_bullets.FieldType.GetGenericArguments()[0].GetField("item", BindingFlags.Public | BindingFlags.Instance);

			const EquipmentType k_CustomRoundEquipmentType = (EquipmentType)1000;
			const EquipmentType k_CustomChamberEquipmentType = (EquipmentType)1001;

			[HarmonyPatch(typeof(PlayerLoadout), nameof(PlayerLoadout.Serialize))]
			[HarmonyPostfix]
			private static void SerializeIndividualRoundTypesFuckYou(JSONObject __result) {
				if (LocalAimHandler.player_instance != null) {
					JSONArray jsonarray = __result["equipment"].AsArray;
					if (LocalAimHandler.player_instance.LooseBulletCount > 0) {
						Debug.Log(LocalAimHandler.player_instance.LooseBulletCount);


						var loose_bullets = _lah_loose_bullets.GetValue(LocalAimHandler.player_instance);

						for (int roundIndex = 0; roundIndex < (int)_list_get_count_info.Invoke(loose_bullets, null); roundIndex++) {
							if (_boolet_display_item_info.GetValue(_list_get_item_info.Invoke(loose_bullets, new object[] { roundIndex })) is ShellCasingScript shellCasingScript) {
								Debug.Log(shellCasingScript.cartridge_type);

								var player_equipment = new PlayerLoadoutEquipment
								{
									//random number lol, equipment doesn't get deserialized by the game unless it's a "valid" type, otherwise it just silently skips
									equipment_type = k_CustomRoundEquipmentType,
									magazine_class = (MagazineClass)shellCasingScript.cartridge_type
								};

								JSONObject jsonobject = player_equipment.Serialize();

								jsonarray.Add(jsonobject);
							}
						}
					}

					if (LocalAimHandler.player_instance.TryGetGun(out var gun)) {
						if (gun.gun_type == GunType.Automatic) {
							EquipmentSlot slot;
							MagazineClass magClass;
							
							if (gun.round_in_chamber != null)
							{
								if (gun.round_in_chamber.IsSpent())
								{
									slot = (EquipmentSlot)PlayerLoadout.ChamberState.Spent;
								}
								else
								{
									slot = (EquipmentSlot)PlayerLoadout.ChamberState.Loaded;
								}

								magClass = (MagazineClass)gun.round_in_chamber.cartridge_type;
							}
							else
							{
								slot = (EquipmentSlot)PlayerLoadout.ChamberState.Empty;
								magClass = 0;
							}

							var player_equipment = new PlayerLoadoutEquipment {
								equipment_type = k_CustomChamberEquipmentType,
								slot = slot,
								magazine_class = magClass
							};

							jsonarray.Add(player_equipment.Serialize());
						}
						else if (gun.gun_type == GunType.Revolver) {
							foreach (ChamberState cylinder in gun.cylinder) {
								if (cylinder == null) {
									Debug.Log("a cylindr was null?????? lol!!!");

									continue;
								}

								JSONObject jsonobject;
								if (!cylinder.HasBullet()) {
									var player_equipment = new PlayerLoadoutEquipment {
										equipment_type = k_CustomChamberEquipmentType,
										slot = (EquipmentSlot)PlayerLoadout.ChamberState.Empty
									};

									jsonobject = player_equipment.Serialize();
								}
								else if (cylinder.IsDisabled()) {
									var player_equipment = new PlayerLoadoutEquipment {
										equipment_type = k_CustomChamberEquipmentType,
										slot = (EquipmentSlot)PlayerLoadout.ChamberState.Blocked,
										magazine_class = (MagazineClass)cylinder.GetBullet().cartridge_type
									};

									jsonobject = player_equipment.Serialize();
								}
								else if (cylinder.IsSpent()) {
									var player_equipment = new PlayerLoadoutEquipment {
										equipment_type = k_CustomChamberEquipmentType,
										slot = (EquipmentSlot)PlayerLoadout.ChamberState.Spent,
										magazine_class = (MagazineClass)cylinder.GetBullet().cartridge_type
									};

									jsonobject = player_equipment.Serialize();
								}
								else
								{
									var player_equipment = new PlayerLoadoutEquipment {
										equipment_type = k_CustomChamberEquipmentType,
										slot = (EquipmentSlot)PlayerLoadout.ChamberState.Loaded,
										magazine_class = (MagazineClass)cylinder.GetBullet().cartridge_type
									};

									jsonobject = player_equipment.Serialize();
								}

								jsonarray.Add(jsonobject);
							}
						}
					}
				}
			}

			[HarmonyPatch(typeof(ReceiverCoreScript), nameof(ReceiverCoreScript.SpawnGun))]
			[HarmonyPostfix]
			private static void HandleChamberCompatStuff(ReceiverCoreScript __instance, GunScript __result, PlayerLoadout loadout) {
				Debug.Log("chamber compat yay");

				if (__result.gun_type == GunType.Automatic)
				{
					foreach (var equipment in loadout.equipment)
					{
						if (equipment.equipment_type == k_CustomChamberEquipmentType)
						{
							Debug.Log("automatic");
							Debug.Log((CartridgeSpec.Preset)equipment.magazine_class);

							if (__result.round_in_chamber != null)
							{
								Object.Destroy(__result.round_in_chamber.gameObject);
							}

							switch ((PlayerLoadout.ChamberState)equipment.slot)
							{
								case PlayerLoadout.ChamberState.Spent:
									SpawnRoundInChamber(__result, (CartridgeSpec.Preset)equipment.magazine_class);
									__result.round_in_chamber.MakeSpent();
									break;
								case PlayerLoadout.ChamberState.Loaded:
									SpawnRoundInChamber(__result, (CartridgeSpec.Preset)equipment.magazine_class);
									break;
								case PlayerLoadout.ChamberState.Empty:
									break;
							}

							break;
						}
					}
				}
				else if (__result.gun_type == GunType.Revolver) {
					var chambers = new List<PlayerLoadoutEquipment>();

					foreach (var equipment in loadout.equipment) {
						if (equipment.equipment_type == k_CustomChamberEquipmentType) {
							chambers.Add(equipment);
						}
					}

					Debug.Log(chambers.Count);

					for (int chamberIndex = 0; chamberIndex < chambers.Count && chamberIndex < __result.cylinder.GetChamberCount(); chamberIndex++) {
						var chamber = __result.cylinder.GetChamber(chamberIndex);

						var cartridge_type = (CartridgeSpec.Preset)chambers[chamberIndex].magazine_class;

						Debug.Log(cartridge_type);

						switch ((PlayerLoadout.ChamberState)chambers[chamberIndex].slot) {
							case PlayerLoadout.ChamberState.Blocked:
								if (chamber.HasBullet()) {
									Object.Destroy(chamber.GetBullet().gameObject);
								}

								ShellCasingScript round;
								if (ModdingKitCorePlugin.GetRoundPrefab(cartridge_type).transform.Find("plug")) {
									round = FillChamber(chamber, cartridge_type);
								}
								else {
									round = Object.Instantiate(__result.loaded_cartridge_prefab).GetComponent<ShellCasingScript>();
								}

								round.MakeSpent();

								foreach (var meshRenderer in round.GetComponentsInChildren<MeshRenderer>()) {
									meshRenderer.enabled = false;
								}

								round.transform.Find("plug").gameObject.SetActive(true);

								if (__result.plug_use_parent_transform) {
									round.transform.parent = round.transform.parent.parent;
								}

								round.transform.localScale = Vector3.one;
								
								typeof(ChamberState).GetField("disabled", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(chamber, true);

								break;
							case PlayerLoadout.ChamberState.Loaded:
								if (chamber.HasBullet()) {
									Object.Destroy(chamber.GetBullet().gameObject);
								}

								FillChamber(chamber, cartridge_type);

								break;
							case PlayerLoadout.ChamberState.Spent:
								if (chamber.HasBullet()) {
									Object.Destroy(chamber.GetBullet().gameObject);
								}

								FillChamber(chamber, cartridge_type).MakeSpent();
								chamber.wedged += global::UnityEngine.Random.Range(0f, 0.5f);
								break;
							case PlayerLoadout.ChamberState.Empty:
								if (chamber.HasBullet()) {
									Object.Destroy(chamber.GetBullet().gameObject);
								}

								break;
						}
					}
				}

				ShellCasingScript FillChamber(ChamberState chamber, CartridgeSpec.Preset cartridge_type) {
					var round = Object.Instantiate(ModdingKitCorePlugin.GetRoundPrefab(cartridge_type));
					chamber.SetBullet(round);
					chamber.GetBullet().transform.position = chamber.transform.position;
					chamber.GetBullet().transform.rotation = chamber.transform.rotation;
					chamber.GetBullet().transform.parent = chamber.transform.parent;
					chamber.GetBullet().transform.localScale = new Vector3(1f, 1f, 1f);
					chamber.wedged = global::UnityEngine.Random.Range(0.05f, 0.1f);
					chamber.seated = 0.01f;
					chamber.seated_vel = 10f;
					PoseEvent poseEvent = new PoseEvent
					{
						parent = chamber.transform,
						position = chamber.transform.InverseTransformPoint(chamber.GetBullet().transform.position),
						rotation = Quaternion.Inverse(chamber.transform.rotation) * chamber.GetBullet().transform.rotation,
						scale = 1f,
						time = Time.time - 10f,
						transition = InventorySlot.fast_spring_baked
					};
					chamber.GetBullet().pose_events.Add(poseEvent);
					chamber.UpdateBulletPosition();
					round.Move(__result.GetComponent<InventorySlot>());
					chamber.seated = 1.0f;
					chamber.UpdateBulletPosition();
					return round;
				}
			}

			[HarmonyPatch(typeof(ReceiverCoreScript), "SpawnPlayer")]
			[HarmonyPrefix]
			private static void RoundCompatStuff(ReceiverCoreScript __instance) {
				Debug.Log("prefix spawnplayer");

				foreach (var equipment in __instance.CurrentLoadout.equipment) {
					Debug.Log(equipment.equipment_type);
					if (equipment.equipment_type == k_CustomRoundEquipmentType) {

						__instance.CurrentLoadout.ammo_count = 0;

						Debug.Log($"current loadout ammo count: {__instance.CurrentLoadout.ammo_count}");
					}
				}
			}

			[HarmonyPatch(typeof(ReceiverCoreScript), "SpawnPlayer")]
			[HarmonyPostfix]
			private static void RoundCompatStuffTwo(ReceiverCoreScript __instance) {
				Debug.Log("postfix spawnplayer");

				var player_equipment_component = Object.FindObjectOfType<PlayerScript>().equipment.GetComponent<PlayerEquipment>();

				Debug.Log(LocalAimHandler.player_instance.loadout);

				Debug.Log(LocalAimHandler.player_instance.loadout.equipment);

				if (LocalAimHandler.player_instance.loadout != null) {
					foreach (var equipment in LocalAimHandler.player_instance.loadout.equipment) {
						Debug.Log(equipment.equipment_type);

						if (equipment.equipment_type == k_CustomRoundEquipmentType) {
							Debug.Log((CartridgeSpec.Preset)equipment.magazine_class);
							var roundPrefab = Object.Instantiate(ModdingKitCorePlugin.GetRoundPrefab((CartridgeSpec.Preset)equipment.magazine_class).gameObject);
							player_equipment_component.AddRound(roundPrefab);
						}
					}
				}
			}

			// [HarmonyPatch(typeof(ReceiverCoreScript), nameof(ReceiverCoreScript.SpawnMagazine))]
			// [HarmonyPostfix]
			// private static void DoTheSwitcheroo(ReceiverCoreScript __instance, MagazineScript __result, PlayerLoadoutEquipment ple) {
			// 	if (ple.persistent_data != null) {
			// 		if (ple.persistent_data["rounds"] != null) {
			// 			__result.SetRoundCount(0);

			// 			var rounds = ple.persistent_data["rounds"].AsArray;

			// 			for (int i = 0; i < rounds.Count; i++) {
			// 				var cartridge_type = (CartridgeSpec.Preset)rounds[i]["type"].AsInt;

			// 				var roundPrefab = Object.Instantiate(__instance.generic_prefabs.First(item => item is ShellCasingScript shellCasingScript && shellCasingScript.cartridge_type == cartridge_type)) as ShellCasingScript;

			// 				__result.AddRound(roundPrefab);
			// 			}
			// 		}
			// 	}
			// }

			[HarmonyPatch(typeof(MagazineScript), nameof(MagazineScript.GetPersistentData))]
			[HarmonyPostfix]
			private static void SaveMagRounds(MagazineScript __instance, JSONObject __result) {
				JSONArray rounds = new JSONArray();

				for (int i = 0; i < __instance.rounds.Count; i++) {
					Debug.Log(__instance.rounds[i].cartridge_type);

					JSONObject jsonObject = new JSONObject();

					jsonObject.Add("type", (int)__instance.rounds[i].cartridge_type);

					rounds.Add(jsonObject);
				}

				__result.Add("rounds", rounds);
			}

			[HarmonyPatch(typeof(MagazineScript), nameof(MagazineScript.SetPersistentData))]
			[HarmonyPostfix]
			private static void AddMagRounds(MagazineScript __instance, JSONObject data) {
				if (data["rounds"] != null) {
					var rounds = data["rounds"].AsArray;

					//if we have no actual save data for this, use the vanilla round count
					if (__instance.rounds_in_mag != rounds.Count) {
						return;
					}

					__instance.rounds_in_mag = 0;

					Debug.Log("adding rounds 2 mag");

					Debug.Log(rounds.Count);

					for (int i = 0; i < rounds.Count; i++) {
						var cartridge_type = (CartridgeSpec.Preset)rounds[i]["type"].AsInt;

						Debug.Log(cartridge_type);

						var prefurb = ModdingKitCorePlugin.GetRoundPrefab(cartridge_type);

						Debug.Log(prefurb.name);

						var roundPrefab = Object.Instantiate(prefurb);

						__instance.AddRound(roundPrefab);
					}
				}
			}

			[HarmonyPatch(typeof(LocalAimHandler), "UpdateLooseBulletDisplay")]
			[HarmonyPostfix]
			private static void UpdateBulletSelectDisplay(object ___loose_bullets) {
				var distinct_presets = GetPresetsInInventory();

				if (Input.GetKeyDown(KeyCode.UpArrow) || distinct_presets.Count <= 1)
				{
					inventory_selected_cartridge = (CartridgeSpec.Preset)int.MaxValue;

					UpdateSelectedCartridge();

					return;
				}

				if (Input.GetKeyDown(KeyCode.LeftArrow))
				{
					var roundIndex = distinct_presets.IndexOf(inventory_selected_cartridge);

					if (roundIndex == distinct_presets.Count - 1) {
						inventory_selected_cartridge = distinct_presets[0];
					}
					else {
						inventory_selected_cartridge = distinct_presets[roundIndex + 1];
					}		

					UpdateSelectedCartridge();
				}

				if (Input.GetKeyDown(KeyCode.RightArrow))
				{
					var roundIndex = distinct_presets.IndexOf(inventory_selected_cartridge);

					if (roundIndex <= 0) {
						inventory_selected_cartridge = distinct_presets[distinct_presets.Count - 1];
					}
					else {
						inventory_selected_cartridge = distinct_presets[roundIndex - 1];
					}		

					UpdateSelectedCartridge();
				}

				void UpdateSelectedCartridge()
				{
					var item_count = (int)_list_get_count_info.Invoke(___loose_bullets, null);

					for (int itemIndex = 0; itemIndex < item_count; itemIndex++)
					{
						var boolet_inventory_item = _list_get_item_info.Invoke(___loose_bullets, new object[] { itemIndex });

						var item = _boolet_display_item_info.GetValue(boolet_inventory_item);

						if (item is ShellCasingScript shellCasingScript)
						{
							var spring = (Spring)_boolet_display_spring_info.GetValue(boolet_inventory_item);

							if (shellCasingScript.cartridge_type == inventory_selected_cartridge) {
								spring.target_state = 0.4f;
							}
							else
							{
								spring.target_state = 0.3f;
							}
						}
					}
				}

				List<CartridgeSpec.Preset> GetPresetsInInventory() {
					var boolet_count = (int)_list_get_count_info.Invoke(___loose_bullets, null);

					var presets = new HashSet<CartridgeSpec.Preset>();

					//sort top to bottom, more intuitive
					for (int booletIndex = boolet_count - 1; booletIndex >= 0; booletIndex--) {
						var item = _boolet_display_item_info.GetValue(_list_get_item_info.Invoke(___loose_bullets, new object[] { booletIndex }));

						if (item is ShellCasingScript shellCasingScript) {
							presets.Add(shellCasingScript.cartridge_type);
						}
					}

					return presets.ToList();
				}
			}

			[HarmonyPatch(typeof(ReceiverCoreScript), "SpawnPlayer")]
			[HarmonyPostfix]
			private static void AddExtenderToSRAB() {
            	if (ReceiverCoreScript.Instance().game_mode.GetGameMode() != GameMode.ReceiverMall) return;

            	var shootingRangeAmmoBoxes = GameObject.Find("Shooting Range/Gameplay/Ammo Tables");
				if (shootingRangeAmmoBoxes != null) {
					foreach (var ammoBox in shootingRangeAmmoBoxes.GetComponentsInChildren<ShootingRangeAmmoBoxScript>()) {
						Debug.Log(ammoBox.name);
						
						ammoBox.gameObject.AddComponent<ShootingRangeAmmoBoxCustomRoundExtender>();
					}
				}

            	var shootingDomeAmmoBoxes = GameObject.Find("Challenge Room/Challenge Room Geometry/AmmoTable");
				if (shootingDomeAmmoBoxes != null) {
					foreach (var ammoBox in shootingDomeAmmoBoxes.GetComponentsInChildren<ShootingRangeAmmoBoxScript>()) {
						Debug.Log(ammoBox.name);
						
						ammoBox.gameObject.AddComponent<ShootingRangeAmmoBoxCustomRoundExtender>();
					}
				}

				var weaponStorageRoomAmmoBoxes = GameObject.Find("Weapon Storage Room/NewGunsLocation");
				if (weaponStorageRoomAmmoBoxes != null) {
					foreach (var ammoBox in weaponStorageRoomAmmoBoxes.GetComponentsInChildren<ShootingRangeAmmoBoxScript>()) {
						Debug.Log(ammoBox.name);
						
						ammoBox.gameObject.AddComponent<ShootingRangeAmmoBoxCustomRoundExtender>();
					}
				}
			}
		}
	}
}