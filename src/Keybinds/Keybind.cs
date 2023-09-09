using UnityEngine;

namespace Receiver2ModdingKit {
	public class Keybind {
		public interface KeyMapping {
			int GetKey();
			bool IsRedirect();
		}

		public class KeyboardKey : KeyMapping {
			private KeyCode key;

			public KeyboardKey(KeyCode key) {
				this.key = key;
			}

			public int GetKey() {
				return (int) key;
			}

			public bool IsRedirect() {
				return false;
			}
		}

		public class KeyRedirect : KeyMapping {
			private int action_id;

			public KeyRedirect(int action_id) {
				this.action_id = action_id;
			}

			public int GetKey() {
				return action_id;
			}

			public bool IsRedirect() {
				return true;
			}
		}

		public string name;
		public string gun_name = "";
		public KeyMapping key;
		public int fallback_action_id;

		public Keybind(string keybind_menu_name, int fallback_action_id, KeyMapping default_value = null) {
			this.name = keybind_menu_name;
			this.fallback_action_id = fallback_action_id;

			this.key = default_value;
		}

		public string GetLongName() {
			return string.Concat(this.gun_name, "::", this.name);
		}
	}
}