using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using TMPro;
using BepInEx;
using Receiver2;
using Receiver2ModdingKit.ModInstaller;
using Receiver2ModdingKit.Assets;
using static UnityEditor.Graphs.Styles;

namespace Receiver2ModdingKit {
#if false
	public class ModdingKitCorePlugin : MonoBehaviour {
		public static ModdingKitCorePlugin instance {
			get;
			private set;
		}

		public PluginInfo Info;

		public static Stream GetResourceStream(string resource_name) { return null; }

		private void Awake() {
			instance = this;

			try {
				ReflectionManager.Initialize();
			} catch (ReflectionManager.MissingFieldException e) {
			} catch (MissingFieldException e) {
				Debug.LogError(e);
			}
		}
	}

#else
	[BepInPlugin("pl.szikaka.receiver_2_modding_kit", "Receiver 2 Modding Kit", "1.4.0")]
	[BepInProcess("Receiver2")]
	public class ModdingKitCorePlugin : BaseUnityPlugin {
		public static ModdingKitCorePlugin instance {
			get;
			private set;
		}

		private static ModHelpEntryManager mod_help;
		private static ModTapeManager mod_tapes;

		public static readonly string supportedVersion = "2.2.4";

		public delegate void StartupAction();

		/// <summary>
		/// Add a task to be executed after ReceiverCoreScript awakes, eliminating the need for a patch
		/// </summary>
		/// <param name="action"> Function to be executed after ReceiverCoreScript awakes </param>
		[Obsolete("Use methods from ModdingKitEvents class instead")]
		public static void AddTaskAtCoreStartup(StartupAction action) {
			ModdingKitEvents.ExecuteOnStartup += () => action.DynamicInvoke();
		}

		public static Stream GetResourceStream(string resource_name) {
			if(Assembly.GetExecutingAssembly().GetManifestResourceInfo("Receiver2ModdingKit.resources." + resource_name) != null) {
				return Assembly.GetExecutingAssembly().GetManifestResourceStream("Receiver2ModdingKit.resources." + resource_name);
			}
			else if (File.Exists(Path.Combine(Path.GetDirectoryName(instance.Info.Location), resource_name))) {
				return File.OpenRead(Path.Combine(Path.GetDirectoryName(ModdingKitCorePlugin.instance.Info.Location), resource_name));
			}
			return Stream.Null;
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
				"   Modding Kit version " + Info.Metadata.Version.ToString() + "\n" +
				"   Was made to support game version " + supportedVersion + "\n" +
				"   And will not work for this version (" + ReceiverCoreScript.Instance().build_info.version + ")\n" +
				"   Please update your game and/or plugin"
			;
		}

		private void Awake() {
			instance = this;

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

			ModdingKitEvents.AddTaskAtCoreStartup( () => {
				AssetDomain.Create(@"D:\Gry\Steam\steamapps\common\Receiver 2\Receiver2_Data");
			} );

			try {
				HarmonyManager.Initialize();
				ReflectionManager.Initialize();
			} catch (HarmonyLib.HarmonyException e) {
				Debug.LogError(e);

				StartCoroutine(SetErrorState());
			} catch (MissingFieldException e) {
				Debug.LogError(e);

				StartCoroutine(SetErrorState());
			}

			mod_help = gameObject.AddComponent<ModHelpEntryManager>();
			mod_tapes = gameObject.AddComponent<ModTapeManager>();

			CustomSounds.ModAudioManager.Initialize();

			foreach (Type type in typeof(BepInPlugin).Assembly.GetTypes())
			{
				if (type.FullName == "BepInEx.ConsoleUtil.Kon")
				{
					Extensions.konType = type;
					break;
				}
			}

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
			if (mod_tapes != null) DestroyImmediate(mod_tapes);

			if (Directory.Exists(ScriptEngine.ScriptDirectory)) Directory.Delete(ScriptEngine.ScriptDirectory, true);

			if (ModLoader.mod_installer != null) DestroyImmediate(ModLoader.mod_installer);

			CustomSounds.ModAudioManager.Release();

			if (Thunderstore.Thunderstore.LaunchedWithR2ModMan) {
				Thunderstore.Thunderstore.CleanupMods();
			}
		}
	}
#endif
}