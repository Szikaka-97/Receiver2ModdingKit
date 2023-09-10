using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using BepInEx.Configuration;
using Receiver2;

namespace Receiver2ModdingKit {
	public static class SettingsMenuManager {

		public class SettingsMenuEntry <T> {
			protected ConfigEntry<T> entry;
			public T default_value {
				get { return (T) entry.DefaultValue; }
			}
			public T value {
				get { return entry.Value; }
			}
			public GameObject label { get; }
			public GameObject control {	get; }

			internal SettingsMenuEntry(ConfigEntry<T> entry, GameObject label, GameObject control) {
				this.entry = entry;
				this.label = label;
				this.control = control;
			}
		}
		
		public class SettingsMenuButton {
			public GameObject label { get; }
			public Button button { get; }

			internal SettingsMenuButton(GameObject label, Button button) {
				this.label = label;
				this.button = button;
			}
		}

		private static readonly string left_column_menu_path = "ReceiverCore/Menus/Overlay Menu Canvas/Aspect Ratio Fitter/New Pause Menu/Backdrop1/Sub-Menu Layout Group/New Settings Menu/ScrollableContent Variant/Viewport/Content/Left Column/";
		private static readonly string right_column_menu_path = "ReceiverCore/Menus/Overlay Menu Canvas/Aspect Ratio Fitter/New Pause Menu/Backdrop1/Sub-Menu Layout Group/New Settings Menu/ScrollableContent Variant/Viewport/Content/Right Column/";
		
		private static GameObject m_keybinds_menu = null;
		private static GameObject keybinds_menu {
			get {
				if (m_keybinds_menu == null) {
					m_keybinds_menu = GameObject.Find("ReceiverCore/Menus/Overlay Menu Canvas/Aspect Ratio Fitter/New Pause Menu/Backdrop1/Sub-Menu Layout Group/New Keybinding Menu/ScrollableContent Variant/Viewport/Content");
				}

				return m_keybinds_menu;
			}
		}

		private static GameObject m_keybinds_selection_field = null;
		private static GameObject keybinds_selection_field {
			get {
				if (m_keybinds_selection_field == null) {
					m_keybinds_selection_field = keybinds_menu.GetComponentInChildren<KeybindingComponent>().gameObject;
				}

				return m_keybinds_selection_field;
			}
		}

		private static GameObject m_label_prefab = null;
		private static GameObject label_prefab {
			get {
				if (m_label_prefab != null) return m_label_prefab;

				m_label_prefab = GameObject.Instantiate(GameObject.Find(left_column_menu_path).GetComponentInChildren<CanvasRenderer>().gameObject);

				return m_label_prefab;
			}
		}

		private static GameObject m_button_setting_prefab = null;
		private static GameObject button_setting_prefab {
			get {
				if (m_button_setting_prefab != null) return m_button_setting_prefab;

				m_button_setting_prefab = GameObject.Instantiate(GameObject.Find(right_column_menu_path).GetComponentInChildren<Button>().transform.parent.gameObject);
				m_button_setting_prefab.GetComponentInChildren<Button>().gameObject.name = "Button";
				m_button_setting_prefab.GetComponentInChildren<Button>().onClick.RemoveAllListeners();

				return m_button_setting_prefab;
			}
		}

		private static GameObject m_toggle_setting_prefab = null;
		private static GameObject toggle_setting_prefab {
			get {
				if (m_toggle_setting_prefab != null) return m_toggle_setting_prefab;

				m_toggle_setting_prefab = GameObject.Instantiate(GameObject.Find(right_column_menu_path).GetComponentInChildren(Type.GetType("Receiver2.ToggleComponent, Wolfire.Receiver2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")).gameObject);


				return m_toggle_setting_prefab;
			}
		}

		private static GameObject m_dropdown_setting_prefab = null;
		private static GameObject dropdown_setting_prefab {
			get {
				if (m_dropdown_setting_prefab != null) return m_dropdown_setting_prefab;

				m_dropdown_setting_prefab = GameObject.Instantiate(GameObject.Find(right_column_menu_path).GetComponentInChildren<DropdownComponent>().gameObject);

				return m_dropdown_setting_prefab;
			}
		}

		private static GameObject m_slider_setting_prefab = null;
		private static GameObject slider_setting_prefab {
			get {
				if (m_slider_setting_prefab != null) return m_slider_setting_prefab;

				m_slider_setting_prefab = GameObject.Instantiate(GameObject.Find(right_column_menu_path).GetComponentInChildren<SliderComponent>().gameObject);

				return m_slider_setting_prefab;
			}
		}

		public static SettingsMenuButton CreateSettingsMenuButton(string name, string button_text, UnityAction callback, int index = -1) {
			GameObject button = GameObject.Instantiate(button_setting_prefab);
			button.name = name + " Container";

			Component.DestroyImmediate(button.transform.Find("Button/Text").GetComponent<LocalizedTextMesh>());
			button.transform.Find("Button/Text").GetComponent<TextMeshProUGUI>().text = button_text;

			var button_event = button.transform.Find("Button").GetComponent<Button>().onClick;

			button_event.RemovePersistentListeners();

			button_event.AddListener(callback);

			button.transform.SetParent(GameObject.Find(right_column_menu_path).transform);
			button.transform.localScale = Vector3.one;
			button.transform.localPosition = new Vector3(button.transform.localPosition.x, button.transform.localPosition.y, 0);
			button.transform.Find("Button").GetComponent<RectTransform>().sizeDelta = new Vector2(596, 46);

			if (index >= 0) button.GetComponent<RectTransform>().SetSiblingIndex(index);

			GameObject label = GameObject.Instantiate(label_prefab);

			label.name = name;

			Component.DestroyImmediate(label.transform.Find("Text").GetComponent<LocalizedTextMesh>());
			label.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = name;

			label.transform.SetParent(button.transform.parent.parent.Find("Left Column"));
			label.transform.localScale = Vector3.one;
			label.transform.localPosition = new Vector3(label.transform.localPosition.x, label.transform.localPosition.y, 0);
			if (index >= 0) label.GetComponent<RectTransform>().SetSiblingIndex(index);

			return new SettingsMenuButton(label, button.transform.Find("Button").GetComponent<Button>());
		}

		public static SettingsMenuEntry<T> CreateSettingsMenuOption <T>(string name, ConfigEntry<T> config_entry, int index = -1) {
			
			if (config_entry == null) {
				throw new ArgumentNullException("SettingsMenuManager.CreateSettingsManuOption(): Config entry cannot be null");
			}

			GameObject control = null;

			switch (typeof(T).Name) {
				case "Boolean":
					control = GameObject.Instantiate(toggle_setting_prefab);
					control.name = name + " Toggle";

					var toggle_component = control.GetComponent(Type.GetType("Receiver2.ToggleComponent, Wolfire.Receiver2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
					UnityEvent<T> toggle_event = toggle_component.GetType().GetField("OnChange").GetValue(toggle_component) as UnityEvent<T>;

					toggle_event.RemovePersistentListeners();

					toggle_event.AddListener(value => { config_entry.Value = value; });
					config_entry.SettingChanged += new EventHandler((caller, args) => {
						toggle_component.GetType().GetField("value", BindingFlags.Public | BindingFlags.Instance).SetValue(toggle_component, config_entry.Value as Boolean?);

						control.transform.Find("Toggle").GetComponent<Toggle>().isOn = (bool) (config_entry.Value as Boolean?);
					});
					control.transform.Find("Toggle").GetComponent<Toggle>().isOn = (bool) (config_entry.Value as Boolean?);

					break;
				case "String":
					if(!(config_entry.Description.AcceptableValues is AcceptableValueList<string>)) {
						throw new ArgumentException("SettingsMenuManager.CreateSettingsManuOption(): Config entry must have an AcceptableValueList<string> in its description to use the dropdown");
					}

					List<string> value_list = ((AcceptableValueList<string>) config_entry.Description.AcceptableValues).AcceptableValues.ToList();

					control = GameObject.Instantiate(dropdown_setting_prefab);
					control.name = name + " Dropdown";

					var dropdown_component = control.GetComponent<DropdownComponent>();

					dropdown_component.SetOptions(value_list);

					var dropdown_event = dropdown_component.OnChange;

					dropdown_event.RemovePersistentListeners();

					dropdown_event.AddListener(value => { config_entry.Value = (T) Convert.ChangeType(((AcceptableValueList<string>) config_entry.Description.AcceptableValues).AcceptableValues[value], typeof(T)); });

					config_entry.SettingChanged += new EventHandler((caller, args) => {
						dropdown_component.Select(value_list.IndexOf((string) Convert.ChangeType(config_entry.Value, typeof(string))));
					});

					dropdown_component.Select(value_list.IndexOf((string) Convert.ChangeType(config_entry.Value, typeof(string))));

					break;
				case "Int32":
				case "Int64":
				case "Double":
				case "Single":
					if(!(config_entry.Description.AcceptableValues is AcceptableValueRange<float>)) {
						throw new ArgumentException("SettingsMenuManager.CreateSettingsManuOption(): Config entry must have an AcceptableValueRange<float> in its description to use the slider");
					}

					AcceptableValueRange<float> value_range = (AcceptableValueRange<float>) config_entry.Description.AcceptableValues;

					control = GameObject.Instantiate(slider_setting_prefab);
					control.name = name + " Slider";

					var slider_component = control.GetComponent<SliderComponent>();

					if (config_entry.Description.Tags.Contains("format: percent")) {
						slider_component.format = "P0";
					}
					else {
						string format = (string) config_entry.Description.Tags.FirstOrDefault( 
							tag => tag is string && Regex.IsMatch((string) tag, "format: [A-z0-9]{1,2}$")
						);

						if (format != null) {
							slider_component.format = format.Substring(8);
						}
						else slider_component.format = "0";
					}

					slider_component.format = config_entry.Description.Tags.Contains("format: percent") ? "P0" : "0";

					slider_component.slider.minValue = value_range.MinValue;
					slider_component.slider.maxValue = value_range.MaxValue;

					var slider_event = slider_component.OnChange;

					slider_event.RemovePersistentListeners();

					slider_event.AddListener(value => { config_entry.Value = (T) Convert.ChangeType(value, typeof(T)); });

					if (typeof(T).Name.StartsWith("Int")) {
						config_entry.SettingChanged += new EventHandler((caller, args) => {
							slider_component.Value = Mathf.Floor((float) Convert.ChangeType(config_entry.Value, typeof(float)));
						});

						slider_component.Value = Mathf.Floor((float) Convert.ChangeType(config_entry.Value, typeof(float)));
					}
					else {
						config_entry.SettingChanged += new EventHandler((caller, args) => {
							slider_component.Value = (float) Convert.ChangeType(config_entry.Value, typeof(float));
						});

						slider_component.Value = (float) Convert.ChangeType(config_entry.Value, typeof(float));
					}

					break;
				default:
					Debug.LogError("Control of type " + typeof(T).Name + " is not supported");
					return null;
			}

			control.transform.SetParent(GameObject.Find(right_column_menu_path).transform);
			control.transform.localScale = Vector3.one;
			control.transform.localPosition = new Vector3(control.transform.localPosition.x, control.transform.localPosition.y, 0);
			if (index >= 0) control.GetComponent<RectTransform>().SetSiblingIndex(index);

			GameObject label = GameObject.Instantiate(label_prefab);

			label.name = name;

			GameObject.DestroyImmediate(label.transform.Find("Text").GetComponent<LocalizedTextMesh>());
			label.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = name;

			label.transform.SetParent(control.transform.parent.parent.Find("Left Column"));
			label.transform.localScale = Vector3.one;
			label.transform.localPosition = new Vector3(label.transform.localPosition.x, label.transform.localPosition.y, 0);
			if (index >= 0) label.GetComponent<RectTransform>().SetSiblingIndex(index);

			return new SettingsMenuEntry<T>(config_entry, label, control);
		}

		public static GameObject AddKeybindsCategory(string category_name, float scale) {
			GameObject category_object = UnityEngine.Object.Instantiate(keybinds_menu.transform.Find("Automatics Category").gameObject, keybinds_menu.transform);

			category_object.transform.Find("Title").GetComponent<TextMeshProUGUI>().text = category_name;

			category_object.transform.localScale = new Vector3(scale, scale, scale);

			category_object.name = category_name + " Category";

			category_object.GetComponent<RectTransform>().SetAsLastSibling();

			Component.DestroyImmediate(category_object.transform.Find("Title").GetComponent<LocalizedTextMesh>());

			return category_object;
		}

		public static ModKeybindComponent AddKeybindField(Keybind keybind) {
			GameObject keybind_field = GameObject.Instantiate(keybinds_selection_field, keybinds_menu.transform);
			keybind_field.name = keybind.name;

			GameObject.DestroyImmediate(keybind_field.GetComponent<KeybindingComponent>());

			keybind_field.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = keybind.name;

			GameObject.DestroyImmediate(keybind_field.transform.Find("Second Binding").gameObject);

			GameObject dropdown = GameObject.Instantiate(dropdown_setting_prefab, keybind_field.transform);
			dropdown.GetComponent<RectTransform>().SetSiblingIndex(2);
			dropdown.name = "Redirect Dropdown";

			var dropdown_component = dropdown.GetComponent<DropdownComponent>();

			var rewired_actions_list = KeybindsManager.rewired_actions.Keys.ToList();

			dropdown_component.SetOptions(rewired_actions_list);

			dropdown_component.OnChange.RemovePersistentListeners();

			if (keybind.key != null && keybind.key.IsRedirect()) {
				dropdown_component.Select(rewired_actions_list.FindIndex( action_name => KeybindsManager.rewired_actions[action_name] == keybind.key.GetKey()));
			}

			(dropdown.GetComponent<RectTransform>().Find("Dropdown") as RectTransform).anchoredPosition = new Vector2(3, 0);
			(dropdown.GetComponent<RectTransform>().Find("Dropdown") as RectTransform).sizeDelta = new Vector2(9, 46);

			var keybind_button = keybind_field.transform.Find("First Binding/Button").GetComponent<Button>();
			keybind_button.GetComponent<RectTransform>().anchoredPosition -= new Vector2(4, 0);

			keybind_button.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = "";

			keybind_button.onClick.RemovePersistentListeners();

			var clear_button = keybind_field.transform.Find("Clear button container/New red button").GetComponent<Button>();

			clear_button.onClick.RemovePersistentListeners();

			var keybind_component = keybind_field.AddComponent<ModKeybindComponent>();

			keybind_component.keybind = keybind;
			keybind_component.keyboard_binding = keybind_button;
			keybind_component.keyboard_binding_text = keybind_button.transform.Find("Text").GetComponent<TextMeshProUGUI>();
			keybind_component.redirect_binding = dropdown_component;
			keybind_component.clear_button = clear_button;

			return keybind_component;
		}

		internal static KeybindContainer GenerateKeybindMenu(ModGunScript gun) {
			if (gun == null || string.IsNullOrEmpty(gun.weapon_group_name)) {
				return null;
			}

			if (KeybindsManager.TryGetKeybinds(gun.weapon_group_name, out var keybinds)) {
				return keybinds;
			}

			if (KeybindsManager.keybinds_menu_banner == null) {
				KeybindsManager.keybinds_menu_banner = AddKeybindsCategory("Modded Gun Keybinds", 1);
			}

			KeybindContainer container = new KeybindContainer() {
				gun_name = gun.weapon_group_name,
				category_object = AddKeybindsCategory(gun.GetGunTactics().title, 0.75f)
			};

			List<Keybind> gun_keybinds = new List<Keybind>();

			foreach (FieldInfo keybind_field in gun.GetType().GetRuntimeFields()) {
				if (keybind_field.FieldType != typeof(Keybind)) {
					continue;
				}

				if (!keybind_field.IsStatic) {
					Debug.LogError("Field " + gun.GetType().Name + "." + keybind_field.Name + " isn't static. Keybind fields have to be static to work");
				}

				Keybind keybind = keybind_field.GetValue(gun) as Keybind;

				keybind.gun_name = gun.weapon_group_name;

				string key = ModdingKitConfig.BindKeybindConfig(keybind);

				if (int.TryParse(key, out int action_id)) {
					keybind.key = new Keybind.KeyRedirect(action_id);
				}
				else if (Enum.TryParse<KeyCode>(key, true, out var keyboard_key)) {
					keybind.key = new Keybind.KeyboardKey(keyboard_key);
				}
				else {
					keybind.key = new Keybind.KeyRedirect(keybind.fallback_action_id);
				}

				gun_keybinds.Add(keybind);
			}

			container.keybind_components = gun_keybinds.Select( AddKeybindField ).ToArray();

			container.SetActive(false);

			KeybindsManager.custom_keybinds.Add(container);

			if (KeybindsManager.keybinds_menu_space == null) {
				KeybindsManager.keybinds_menu_space = AddKeybindsCategory("", 1);

				KeybindsManager.keybinds_menu_space.name = "Keybinds Space";

				KeybindsManager.keybinds_menu_space.GetComponent<HorizontalLayoutGroup>().childControlHeight = true;
				KeybindsManager.keybinds_menu_space.GetComponentInChildren<LayoutElement>().preferredHeight = 200;
			}

			KeybindsManager.keybinds_menu_space.GetComponent<RectTransform>().SetAsLastSibling();
			
			return container;
		}

		internal static void ShowKeyBindDialog(string text) {
			var menu = GameObject.Find("ReceiverCore/Menus/Overlay Menu Canvas/Aspect Ratio Fitter/New Pause Menu/Backdrop1/Sub-Menu Layout Group");

			menu.transform.Find("New Keybinding Menu").gameObject.SetActive(false);

			menu.transform.Find("Bind Key Dialog").gameObject.SetActive(true);

			menu.transform.Find("Bind Key Dialog").GetComponent<BindKeyDialogScript>().title_text.text = Locale.FormatUIString(LocaleUIString.M_CBM_REBIND_TITLE, new object[] { text });
			menu.transform.Find("Bind Key Dialog").GetComponent<BindKeyDialogScript>().main_text.text = Locale.FormatUIString(LocaleUIString.M_CBM_REBIND_TEXT, new object[] { text });
		}

		internal static void HideKeyBindDialog() {
			var menu = GameObject.Find("ReceiverCore/Menus/Overlay Menu Canvas/Aspect Ratio Fitter/New Pause Menu/Backdrop1/Sub-Menu Layout Group");

			menu.transform.Find("New Keybinding Menu").gameObject.SetActive(true);

			menu.transform.Find("Bind Key Dialog").gameObject.SetActive(false);
		}
	}
}
