using System.Collections.Generic;
using UnityEngine;
using BepInEx;
using Receiver2;
using TMPro;

namespace Receiver2ModdingKit {
	[BepInPlugin("pl.szikaka.receiver_2_modding_kit", "Receiver 2 Modding Kit", "0.2.0")]
	[BepInProcess("Receiver2")]
	internal class ModdingKitCorePlugin : BaseUnityPlugin {
		public static ModdingKitCorePlugin instance {
			get;
			private set;
		}

		internal static Dictionary<uint, CartridgeSpec> custom_cartridges = new Dictionary<uint, CartridgeSpec>();
		public static string supportedVersion = "2.2.4";

		public static void UpdateModGuns(GunScript gun) {
			if (gun is ModGunScript) ((ModGunScript) gun).UpdateGun();
		}

		private System.Collections.IEnumerator SetErrorState() {
			while (ReceiverCoreScript.Instance() == null) yield return null;

			GameObject errorObject = new GameObject("Modding Kit Error");

			errorObject.AddComponent<RectTransform>();
			errorObject.AddComponent<TextMeshProUGUI>();

			errorObject.GetComponent<RectTransform>().SetParent(GameObject.Find("ReceiverCore/Menus/Overlay Menu Canvas/Aspect Ratio Fitter").transform);
			errorObject.GetComponent<RectTransform>().pivot = Vector2.zero;
			errorObject.GetComponent<RectTransform>().localScale = Vector3.one;
			errorObject.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
			errorObject.GetComponent<RectTransform>().pivot = Vector2.one;
			errorObject.GetComponent<RectTransform>().anchorMax = Vector2.one;
			errorObject.GetComponent<RectTransform>().anchorMin = Vector2.one;
			errorObject.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 300);

			errorObject.GetComponent<TextMeshProUGUI>().fontSize = 20;
			errorObject.GetComponent<TextMeshProUGUI>().faceColor = new Color32(255, 50, 50, 255);
			errorObject.GetComponent<TextMeshProUGUI>().text = 
				"An error happened within the Modding Kit:\n" +
				"   Modding Kit version " + PluginInfo.PLUGIN_VERSION + "\n" +
				"   Was made to support game version " + supportedVersion + "\n" +
				"   And will not work for this version (" + ReceiverCoreScript.Instance().build_info.version + ")\n" +
				"   Please update your game and/or plugin"
			;
		}

		private void Awake() {
			instance = this;

			try {
				HarmonyManager.Initialize();
				ReflectionManager.Initialize();
			} catch (HarmonyLib.HarmonyException) {
				StartCoroutine(SetErrorState());
			} catch (ReflectionManager.MissingFieldException) {
				StartCoroutine(SetErrorState());
			}

			gameObject.AddComponent<ModHelpEntryManager>();
		}
	}
}
