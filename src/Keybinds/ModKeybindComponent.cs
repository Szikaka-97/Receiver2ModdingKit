using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Receiver2;

namespace Receiver2ModdingKit {
	public class ModKeybindComponent : MonoBehaviour {
		public Keybind keybind;

		public Button keyboard_binding;
		public TextMeshProUGUI keyboard_binding_text;

		public DropdownComponent redirect_binding;

		private IEnumerator PickKeyCoroutine() {
			SettingsMenuManager.ShowKeyBindDialog(this.name);
			
			while (!Input.GetKeyDown(KeyCode.Escape)) {
				foreach (KeyCode key in Enum.GetValues(typeof(KeyCode))) {
					if (Input.GetKeyDown(key)) {
						this.redirect_binding.Select(0);

						this.keybind.key = new Keybind.KeyboardKey(key);

						this.keyboard_binding_text.text = key.ToString();

						SettingsMenuManager.HideKeyBindDialog();

						ModdingKitConfig.UpdateKeybindValue(keybind);

						yield break;
					}
				}

				yield return null;
			}

			SettingsMenuManager.HideKeyBindDialog();
		}

		public void Awake() {
			if (this.keybind.key.IsRedirect()) {
				for (int index = 0; index < this.redirect_binding.dropdown.options.Count; index++) {
					string action_name = this.redirect_binding.dropdown.options[index].text;

					if (KeybindsManager.rewired_actions[action_name] == this.keybind.key.GetKey()) {
						string key = KeybindsManager.rewired_actions.Keys.ElementAt(index);

						this.keybind.key = new Keybind.KeyRedirect(KeybindsManager.rewired_actions[key]);

						this.keyboard_binding_text.text = "";

						break;
					}
				}
			}
			else {
				this.keyboard_binding_text.text = ((KeyCode) this.keybind.key.GetKey()).ToString();
			}

			this.keyboard_binding.onClick.AddListener( () => {
				ModdingKitCorePlugin.instance.StartCoroutine(PickKeyCoroutine());
			} );

			this.redirect_binding.OnChange.AddListener( index => {
				string key = KeybindsManager.rewired_actions.Keys.ElementAt(index);

				this.keybind.key = new Keybind.KeyRedirect(KeybindsManager.rewired_actions[key]);

				this.keyboard_binding_text.text = "";

				ModdingKitConfig.UpdateKeybindValue(keybind);
			} );
		}
	}
}