using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.CSharp;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.Events;
using Receiver2;

namespace Receiver2ModdingKit {
	public class ModdingKitConfig {
		public static ConfigEntry<bool> use_custom_sounds;

		public static void Initialize() {
			use_custom_sounds = ModdingKitCorePlugin.instance.Config.Bind("Guns settings", "Use Custom Sounds", true, "Should modded guns use custom sounds.");

			Transform menu = GameObject.Find("ReceiverCore/Menus/Overlay Menu Canvas/Aspect Ratio Fitter/New Pause Menu/Backdrop1/Sub-Menu Layout Group/New Settings Menu/ScrollableContent Variant/Viewport/Content/").transform;

			GameObject label = GameObject.Instantiate(menu.Find("Left Column/Audio Device Label").gameObject);
			label.name = "Custom Sounds Label";

			GameObject.DestroyImmediate(label.transform.Find("Text").GetComponent<LocalizedTextMesh>());
			label.transform.Find("Text").GetComponent<TMPro.TextMeshProUGUI>().text = "Custom sounds for mod guns";

			label.transform.SetParent(menu.Find("Left Column"));
			label.transform.localScale = Vector3.one;
			label.GetComponent<RectTransform>().SetSiblingIndex(9);

			GameObject toggle = GameObject.Instantiate(menu.Find("Right Column/Enable Analytics Toggle").gameObject);
			toggle.name = "Custom Sounds Toggle";

			var toggle_component = toggle.GetComponent(Type.GetType("Receiver2.ToggleComponent, Wolfire.Receiver2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
			UnityEvent<bool> toggle_event = toggle_component.GetType().GetField("OnChange").GetValue(toggle_component) as UnityEvent<bool>;
			toggle_event.m_Calls.Clear();
			toggle_event.AddListener(value => { use_custom_sounds.Value = value; });
			toggle.transform.Find("Toggle").GetComponent<UnityEngine.UI.Toggle>().isOn = use_custom_sounds.Value;

			toggle.transform.SetParent(menu.Find("Right Column"));
			toggle.transform.localScale = Vector3.one;
			toggle.GetComponent<RectTransform>().SetSiblingIndex(9);
		}

		public static void AddConfigEventListener(EventHandler<SettingChangedEventArgs> settings_changed_listener) {
			ModdingKitCorePlugin.instance.Config.SettingChanged += settings_changed_listener;
		}
	}
}
