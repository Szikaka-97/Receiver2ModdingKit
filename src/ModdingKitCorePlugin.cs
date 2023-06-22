using System;
using System.Collections.Generic;
using UnityEngine;
using BepInEx;
using Receiver2;
using TMPro;
using Receiver2ModdingKit.ModInstaller;
using System.IO;
using System.Threading;

namespace Receiver2ModdingKit {
	[BepInPlugin("pl.szikaka.receiver_2_modding_kit", "Receiver 2 Modding Kit", "1.1.0")]
	[BepInProcess("Receiver2")]
	public class ModdingKitCorePlugin : BaseUnityPlugin {
		public static ModdingKitCorePlugin instance {
			get;
			private set;
		}

		private static ModHelpEntryManager mod_help;
		internal static Dictionary<uint, CartridgeSpec> custom_cartridges = new Dictionary<uint, CartridgeSpec>(); 

		public static readonly string supportedVersion = "2.2.4";

		public delegate void StartupAction();

		/// <summary>
		/// Add a task to be executed after ReceiverCoreScript awakes, eliminating the need for a patch
		/// </summary>
		/// <param name="action"> Function to be executed after ReceiverCoreScript awakes </param>
		public static void AddTaskAtCoreStartup(StartupAction action) {
			ExecuteOnStartup += action;
		}
		internal static StartupAction ExecuteOnStartup = new StartupAction(() => { });

		internal static void UpdateModGuns(GunScript gun) {
			if (gun is ModGunScript) {
				try {
					((ModGunScript) gun).UpdateGun();
				} catch (Exception e) {
					Debug.LogException(e);
				}
			}
		}

		private System.Collections.IEnumerator SetErrorState() {
			while (ReceiverCoreScript.Instance() == null) yield return null;

			GameObject error_object = new GameObject("Modding Kit Error");

			var error_transform = error_object.AddComponent<RectTransform>();
			var error_text = error_object.AddComponent<TextMeshProUGUI>();

			error_transform.SetParent(GameObject.Find("ReceiverCore/Menus/Overlay Menu Canvas/Aspect Ratio Fitter").transform);
			error_transform.pivot = Vector2.zero;
			error_transform.localScale = Vector3.one;
			error_transform.anchoredPosition = Vector2.zero;
			error_transform.pivot = Vector2.one;
			error_transform.anchorMax = Vector2.one;
			error_transform.anchorMin = Vector2.one;
			error_transform.sizeDelta = new Vector2(400, 300);

			error_text.fontSize = 20;
			error_text.faceColor = new Color32(255, 50, 50, 255);
			error_text.text = 
				"An error happened within the Modding Kit:\n" +
				"   Modding Kit version " + PluginInfo.PLUGIN_VERSION + "\n" +
				"   Was made to support game version " + supportedVersion + "\n" +
				"   And will not work for this version (" + ReceiverCoreScript.Instance().build_info.version + ")\n" +
				"   Please update your game and/or plugin"
			;
		}

		private void Awake() {
			instance = this;

			#if THUNDERSTORE

			try {
				if (Thunderstore.Thunderstore.LaunchedWithR2ModMan) {
					Debug.Log("Launched Receiver 2 Modding Kit with r2modman");

					Thunderstore.Thunderstore.InstallMods();
				}
			} catch (Exception e) {
				Debug.LogError("Error while loading mods");
				Debug.LogException(e);
			} finally {
				if (!Thunderstore.Thunderstore.LaunchedWithR2ModMan) {
					Debug.Log("Launched Receiver 2 Modding Kit standalone");	
				}
			}

			#else

			Debug.Log("Launched Receiver 2 Modding Kit");

			#endif

			try {
				HarmonyManager.Initialize();
				ReflectionManager.Initialize();
			} catch (HarmonyLib.HarmonyException e) {
				Debug.LogError(e);

				StartCoroutine(SetErrorState());
			} catch (ReflectionManager.MissingFieldException e) {
				Debug.LogError(e);

				StartCoroutine(SetErrorState());
			}

			mod_help = gameObject.AddComponent<ModHelpEntryManager>();

			CustomSounds.ModAudioManager.Initialize();

			if (!Directory.Exists(Path.Combine(Application.persistentDataPath, "GlobalBaseConfiguration"))) Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "GlobalBaseConfiguration"));
			if (!Directory.Exists(Path.Combine(Application.persistentDataPath, "PlayerLoadouts"))) Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "PlayerLoadouts"));
			if (!Directory.Exists(Path.Combine(Application.persistentDataPath, "RankingProgressionCampaigns"))) Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "RankingProgressionCampaigns"));
			if (!Directory.Exists(Path.Combine(Application.persistentDataPath, "WorldGenerationConfigurations"))) Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "WorldGenerationConfigurations"));
			if (!Directory.Exists(Path.Combine(Application.persistentDataPath, "Guns"))) Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "Guns"));
			if (!Directory.Exists(Path.Combine(Application.persistentDataPath, "Tiles"))) Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "Tiles"));
			if (!Directory.Exists(Path.Combine(Application.persistentDataPath, "Tapes"))) Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "Tapes"));
		}

		private void Update() {
			CustomSounds.ModAudioManager.Update();
		}

		private void OnDestroy() {
			HarmonyManager.UnpatchAll();

			if (mod_help != null) DestroyImmediate(mod_help);

			if (Directory.Exists(ScriptEngine.ScriptDirectory)) Directory.Delete(ScriptEngine.ScriptDirectory, true);

			if (ModLoader.mod_installer != null) DestroyImmediate(ModLoader.mod_installer);

			CustomSounds.ModAudioManager.Release();

			#if THUNDERSTORE

			Thunderstore.Thunderstore.CleanupMods();

			#endif
		}
	}
}
