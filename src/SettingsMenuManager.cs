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

			button_event.RemoveAllListeners();
			// button_event.m_Calls = new InvokableCallList();
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
					toggle_event.RemoveAllListeners();
					toggle_event.RemoveAllListeners();

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

					dropdown_event.RemoveAllListeners();
					dropdown_event.RemoveAllListeners();
					dropdown_event.AddListener(value => { config_entry.Value = (T) Convert.ChangeType(((AcceptableValueList<string>) config_entry.Description.AcceptableValues).AcceptableValues[value], typeof(T)); });

					config_entry.SettingChanged += new EventHandler((caller, args) => {
						dropdown_component.Select(value_list.IndexOf((string) Convert.ChangeType(config_entry.Value, typeof(string))));
					});

					dropdown_component.Select(value_list.IndexOf((string) Convert.ChangeType(config_entry.Value, typeof(string))));

					break;
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
					else if (config_entry.Description.Tags.Any( tag => 
						tag is string && Regex.IsMatch((string) tag, "format: [a-Z0-9]{1,2}$")
					)) {
						slider_component.format = ((string) config_entry.Description.Tags.First( tag => 
							tag is string && Regex.IsMatch((string) tag, "format: [a-Z0-9]{1,2}$")
						)).Substring(8);
					}
					else slider_component.format = "0";

					slider_component.format = config_entry.Description.Tags.Contains("format: percent") ? "P0" : "0";

					slider_component.slider.minValue = (float) value_range.MinValue;
					slider_component.slider.maxValue = (float) value_range.MaxValue;

					var slider_event = slider_component.OnChange;

					slider_event.RemoveAllListeners();
					slider_event.RemoveAllListeners();
					slider_event.AddListener(value => { config_entry.Value = (T) Convert.ChangeType(value, typeof(T)); });

					config_entry.SettingChanged += new EventHandler((caller, args) => {
						slider_component.Value = (float) Convert.ChangeType(config_entry.Value, typeof(float));
					});

					slider_component.Value = (float) Convert.ChangeType(config_entry.Value, typeof(float));

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
    }
}
