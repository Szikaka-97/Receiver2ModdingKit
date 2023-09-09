using UnityEngine;

namespace Receiver2ModdingKit {
	public class KeybindContainer {
		public string gun_name;
		public bool active {
			get;
			private set;
		}

		public GameObject category_object;
		public ModKeybindComponent[] keybind_components;

		public void SetActive(bool active) {
			this.active = active;

			category_object.SetActive(active);

			foreach (var keybind_component in keybind_components) {
				keybind_component.gameObject.SetActive(active);
			}
		}
	}
}