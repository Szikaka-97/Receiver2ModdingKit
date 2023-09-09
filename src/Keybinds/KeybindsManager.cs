using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using Receiver2;

namespace Receiver2ModdingKit {
	internal static class KeybindsManager {
		internal static GameObject keybinds_menu_banner = null;
		internal static GameObject keybinds_menu_space = null;
		internal static List<KeybindContainer> custom_keybinds = new List<KeybindContainer>();

		private static Dictionary<string, int> m_rewired_actions = null;
		internal static Dictionary<string, int> rewired_actions {
			get {
				if (m_rewired_actions == null) {
					m_rewired_actions = new Dictionary<string, int>() {
						{"None", -1}
					};

					foreach (var field in typeof(RewiredConsts.Action).GetFields(BindingFlags.Public | BindingFlags.Static)) {
						int action_id = (int) field.GetValue(null);

						LocaleUIString label = ControllerBindingScript.RewiredActionToButtonLabel(action_id);

						if (label != LocaleUIString.M_CBM_UNKNOWN) {
							m_rewired_actions.Add (
								Locale.GetUIString(label),
								action_id
							);
						}
					}
				}

				return m_rewired_actions;
			}
		}

		public static bool IsKeybindRegistered(string weapon_group_name) {
			return custom_keybinds.Any( keybind => keybind.gun_name == weapon_group_name );
		}

		public static KeybindContainer GetKeybinds(string weapon_group_name) {
			return custom_keybinds.First( keybind => keybind.gun_name == weapon_group_name );
		}

		public static bool TryGetKeybinds(string weapon_group_name, out KeybindContainer keybind_container) {
			foreach (var keybind in custom_keybinds) {
				if (keybind.gun_name == weapon_group_name) {
					keybind_container = keybind;

					return true;
				}
			}

			keybind_container = null;

			return false;
		}

		public static void DeactivateAllKeybinds() {
			if (keybinds_menu_banner != null) {
				keybinds_menu_banner.SetActive(false);
			}

			if (keybinds_menu_space != null) {
				keybinds_menu_space.SetActive(false);
			}

			foreach (var keybind in custom_keybinds) {
				keybind.SetActive(false);
			}
		}

		public static void SetKeybindsActive(string weapon_group_name) {
			bool any_match = false;

			foreach (var keybind in custom_keybinds) {
				if (keybind.gun_name == weapon_group_name) {
					keybind.SetActive(true);

					any_match = true;
				}
				else {
					keybind.SetActive(false);
				}
			}

			if (keybinds_menu_banner != null) {
				keybinds_menu_banner.SetActive(any_match);
			}

			if (keybinds_menu_space != null) {
				keybinds_menu_space.SetActive(any_match);
			}
		}
	}
}