using HarmonyLib;
using Receiver2;
using UnityEngine;
using ImGuiNET;
using Rewired;
using System.IO;
using System.Collections.Generic;
using SimpleJSON;

namespace Receiver2ModdingKit.Gamemodes {
	public class ModGameModeManager : MonoBehaviour {
		public class ModGameModeLoadData {
			public ModGameModeBase game_mode;
			public FileInfo scene_bundle_file;
			public AssetBundle scene_bundle;
		}

		public static ModGameModeManager Instance { get; private set; }

		public static bool DisplayGameModeMenu = false;
		public static bool SuppressMenu = false;

		public static Dictionary<string, ModGameModeLoadData> GameModes { get; private set; } = new Dictionary<string, ModGameModeLoadData>();

		public static ModGameModeBase CurrentGameMode { 
			get {
				if (current_game_mode != null) {
					return current_game_mode.game_mode;
				}
				return null;
			}
		}

		private static ModGameModeBase chosen_gamemode;

		private static SensoryTank mod_gamemode_tank;

		private static ModGameModeLoadData current_game_mode;

		private System.Collections.IEnumerator FindGamemodes() {
			DirectoryInfo game_mode_dir = new DirectoryInfo(Path.Combine(Application.persistentDataPath, "Gamemodes"));

			if (!game_mode_dir.Exists) {
				game_mode_dir.Create();
			}

			foreach (FileInfo game_mode_file in game_mode_dir.EnumerateFiles("*." + SystemInfo.operatingSystemFamily.ToString().ToLower(), SearchOption.AllDirectories)) {
				Debug.Log("Searching file " + game_mode_file.Name);

				var bundle_request = AssetBundle.LoadFromFileAsync(game_mode_file.FullName);

				while (!bundle_request.isDone) {
					yield return null;
				}

				AssetBundle bundle = bundle_request.assetBundle;

				if (bundle.isStreamedSceneAssetBundle) {
					bundle.Unload(true);

					continue;
				}

				GameObject[] gobjects = bundle.LoadAllAssets<GameObject>();

				foreach (GameObject gobject in gobjects) {
					if (gobject.TryGetComponent<ModGameModeBase>(out ModGameModeBase gamemode)) {
						FileInfo assets_file = new FileInfo(Path.Combine(game_mode_file.Directory.FullName, gamemode.SceneAssetBundleName + "." + SystemInfo.operatingSystemFamily.ToString().ToLower()));

						if (assets_file.Exists) {
							GameModes[gamemode.GameModeName] = new ModGameModeLoadData() {
								game_mode = gamemode,
								scene_bundle_file = assets_file
							};
							
							ReceiverCoreScript.Instance().level_manager.level_list.scenes.Add(new SceneReferenceInstance() {
								id = gamemode.GameModeName,
								name = gamemode.SceneName
							});
						}
						else {
							Debug.LogError("Could not load file " + assets_file.FullName);
						}
					}
				}

				bundle.Unload(false);
			}

			Debug.Log("Found gamemodes: " + GameModes.Count);

			yield break;
		}

		private GameModeBase PrepareDummyGameMode() {
			GameObject game_mode_container = new GameObject("Dummy Game Mode Prefab");
			Object.DontDestroyOnLoad(game_mode_container);

			return game_mode_container.AddComponent<DummyGameMode>();
		}

		private void Awake() {
			Instance = this;

			ModdingKitEvents.AddTaskAtCoreStartup(() => {
				StartCoroutine(FindGamemodes());

				ReceiverCoreScript.Instance().game_mode_prefabs.Add(PrepareDummyGameMode());
			});
		}

		private void Update() {
			if (DisplayGameModeMenu) {
				Cursor.visible = true;
				Cursor.lockState = CursorLockMode.None;

				if (ReInput.players.SystemPlayer.GetButtonDown(510)) {
					mod_gamemode_tank.ToggleDoor();

					return;
				}

				ImGui.SetNextWindowSize(new Vector2(400, 300f), ImGuiCond.FirstUseEver);
				ImGui.SetNextWindowPos(new Vector2(Screen.currentResolution.width / 2 - 200, Screen.currentResolution.height / 2 - 150));

				if (ImGui.Begin("Select Target Gamemode", ImGuiWindowFlags.NoCollapse)) {
					foreach (var game_mode_pair in GameModes) {
						ModGameModeBase game_mode = game_mode_pair.Value.game_mode;

						bool selected = game_mode == chosen_gamemode;

						if (game_mode != null && ImGui.Checkbox(game_mode.GameModeName, ref selected)) {
							chosen_gamemode = game_mode;
						}
					}

					ImGui.Separator();

					if (ImGui.Button("Launch")) {
						ReceiverCoreScript.Instance().StartGameMode(ModGameModeBase.ModGameMode);
						DisplayGameModeMenu = false;
						SuppressMenu = false;
					}
					if (ImGui.Button("Exit")) {
						chosen_gamemode = null;
						mod_gamemode_tank.ToggleDoor();
					}

					ImGui.End();
				};
			}
			else {
				SuppressMenu = false;
			}
		}

		public static void StartModGameMode(ModGameModeBase game_mode, JSONObject checkpoint) {
			if (game_mode != null && GameModes.ContainsKey(game_mode.GameModeName)) {
				current_game_mode = GameModes[game_mode.GameModeName];

				if (current_game_mode.scene_bundle == null && current_game_mode.scene_bundle_file.Exists) {
					try {
						current_game_mode.scene_bundle = AssetBundle.LoadFromFile(current_game_mode.scene_bundle_file.FullName);
					} catch {}
				}

				ReceiverCoreScript.Instance().level_manager.LoadLevel(current_game_mode.game_mode.GameModeName);
			}
		}

		internal static JSONObject ChooseGameModeFromJSON(JSONObject game_mode_data) {
			if (game_mode_data["id"].AsInt == (int) ModGameModeBase.ModGameMode) {
				string game_mode_name = game_mode_data["name"];

				if (!GameModes.ContainsKey(game_mode_name)) {
					Debug.LogError("Attempting to load into a nonexisting mod game mode, falling back on the campaign");

					game_mode_data.Add("id", (int) GameMode.RankingCampaign);
					game_mode_data.Add("checkpoint", null);

					return game_mode_data;
				}

				chosen_gamemode = GameModes[game_mode_name].game_mode;
			}
			
			return game_mode_data;
		}

		internal static string GetGameModeName() {
			GameMode game_mode = ReceiverCoreScript.Instance().game_mode.GetGameMode();

			if (game_mode != ModGameModeBase.ModGameMode) {
				return game_mode.ToString();
			}

			if (CurrentGameMode == null) {
				return "";
			}

			return CurrentGameMode.GameModeName;
		}

		private static void UnloadModGameModeBundle() {
			if (CurrentGameMode != null) {
				foreach (AssetBundle bundle in AssetBundle.GetAllLoadedAssetBundles()) {
					Debug.Log(bundle.name);

					if (bundle.name == CurrentGameMode.SceneAssetBundleName + "." + SystemInfo.operatingSystemFamily.ToString().ToLower()) {
						Debug.Log("Bundle found" + bundle.name);
					}
				}
			}
		}

		[HarmonyPatch(typeof(ReceiverCoreScript), nameof(ReceiverCoreScript.StartGameMode), new System.Type[] { typeof(GameMode), typeof(JSONObject) })]
		[HarmonyPostfix]
		private static void StartModGameModePatch(GameMode mode, JSONObject checkpoint) {
			if (mode != ModGameModeBase.ModGameMode) {
				if (current_game_mode != null && current_game_mode.scene_bundle != null) {
					current_game_mode.scene_bundle.Unload(true);
					current_game_mode.scene_bundle = null;
				}
				chosen_gamemode = null;
				current_game_mode = null;

				return;
			}

			StartModGameMode(chosen_gamemode, checkpoint);
		}

		[HarmonyPatch(typeof(SensoryTank), "Start")]
		[HarmonyPostfix]
		private static void SetSensoryTankTarget(SensoryTank __instance) {
			if (__instance.name == "SensoryTank (2)") {
				mod_gamemode_tank = __instance;

				__instance.GetComponent<GameModeTransitionUtility>().target_gamemode = ModGameModeBase.ModGameMode;
			}
		}

		[HarmonyPatch(typeof(SensoryTank), nameof(SensoryTank.OnPlayerStay))]
		[HarmonyPrefix]
		private static bool SensoryTankPlayerStay(SensoryTank __instance, bool ___is_open, float ___closed_time) {
			if (__instance.GetComponent<GameModeTransitionUtility>().target_gamemode == ModGameModeBase.ModGameMode) {
				if (!___is_open && ___closed_time + 4.1f < Time.time) {
					
					DisplayGameModeMenu = true;
					SuppressMenu = true;

					LocalAimHandler.player_instance.enabled = false;
				}

				return false;
			}

			return true;
		}

		[HarmonyPatch(typeof(SensoryTank), nameof(SensoryTank.ToggleDoor))]
		[HarmonyPrefix]
		private static void SensoryTankPlayerOpenDoor() {
			if (DisplayGameModeMenu) {
				DisplayGameModeMenu = false;

				LocalAimHandler.player_instance.enabled = true;
			}
		}

		[HarmonyPatch(typeof(Rewired.Player), nameof(Rewired.Player.GetButtonDown), new System.Type[] { typeof(int) })]
		[HarmonyPrefix]
		private static bool SuppressMenuWhenInPod(ref int actionId, ref bool __result) {
			if (SuppressMenu) {
				if (actionId == 51) {
					__result = false;

					return false;
				}
				else if (actionId == 510) {
					actionId = 51;
				}
			}

			return true;
		}
	}
}