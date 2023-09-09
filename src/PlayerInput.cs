using Receiver2;
using UnityEngine;
using Wolfire;

namespace Receiver2ModdingKit {
	public class PlayerInput {
		private GunScript gun;
		private LocalAimHandler lah;

		public PlayerInput(GunScript gun) {
			this.gun = gun;
			this.lah = LocalAimHandler.player_instance;
		}

		private bool CheckGun() {
			return lah.hands[1].state == LocalAimHandler.Hand.State.HoldingGun && gun.slot == lah.hands[1].slot;
		}

		/// <summary>
		/// Check if a button was pressed this frame
		/// </summary>
		/// <param name="button"> Numerical id of the button, available through RewiredConsts namespace </param>
		/// <returns>
		/// True if the button was pressed this frame, false if not
		/// </returns>
		public bool GetButtonDown(int button) {
			return GetButtonDown(button, false);
		}

		/// <summary>
		/// Check if a button was pressed this frame, skipping the holding-gun check
		/// </summary>
		/// <param name="button"> Numerical id of the button, available through RewiredConsts namespace </param>
		/// <param name="ignore_lah"> Whether or not to skip the check for the player holding the gun </param>
		/// <returns>
		/// True if the button was pressed this frame, false if not
		/// </returns>
		public bool GetButtonDown(int button, bool ignore_lah) {
			return (
				ignore_lah || CheckGun()
				&& lah.character_input.GetButtonDown(button)
			);
		}

		/// <summary>
		/// Check if a button was pressed this frame
		/// </summary>
		/// <param name="key"> TODO </param>
		/// <returns>
		/// True if the button was pressed this frame, false if not
		/// </returns>
		public bool GetButtonDown(Keybind key) {
			return GetButtonDown(key, false);
		}

		/// <summary>
		/// Check if a button was pressed this frame
		/// </summary>
		/// <param name="key"> TODO </param>
		/// <returns>
		/// True if the button was pressed this frame, false if not
		/// </returns>
		public bool GetButtonDown(Keybind key, bool ignore_lah) {
			if (key == null) return false;

			if (key.key == null) {
				return GetButtonDown(key.fallback_action_id, ignore_lah);
			}

			if (key.key.IsRedirect()) {
				return GetButtonDown(key.key.GetKey());
			}

			return  (ignore_lah || CheckGun())
					&&
					!ImGuiConsole.console_open
					&&
					Input.GetKeyDown((KeyCode) key.key.GetKey());
		}

		/// <summary>
		/// Check if a button is being held at this frame
		/// </summary>
		/// <param name="button"> Numerical id of the button, available through RewiredConsts namespace </param>
		/// <returns>
		/// True if a button is being held at this frame, false otherwise
		/// </returns>
		public bool GetButton(int button) {
			return GetButton(button, false);
		}

		/// <summary>
		/// Check if a button is being held at this frame, skipping the holding-gun check
		/// </summary>
		/// <param name="button"> Numerical id of the button, available through RewiredConsts namespace </param>
		/// /// <param name="ignore_lah"> Whether or not to skip the check for the player holding the gun </param>
		/// <returns>
		/// True if a button is being held at this frame, false otherwise
		/// </returns>
		public bool GetButton(int button, bool ignore_lah) {
			return (
				ignore_lah || CheckGun()
				&& lah.character_input.GetButton(button)
			);
		}

		/// <summary>
		/// Check if a button is being held at this frame
		/// </summary>
		/// <param name="key"> TODO </param>
		/// <returns>
		/// True if a button is being held at this frame, false otherwise
		/// </returns>
		public bool GetButton(Keybind key) {
			return GetButton(key, false);
		}

		/// <summary>
		/// Check if a button is being held at this frame, skipping the holding-gun check
		/// </summary>
		/// <param name="key"> TODO </param>
		/// <returns>
		/// True if a button is being held at this frame, false otherwise
		/// </returns>
		public bool GetButton(Keybind key, bool ignore_lah) {
			if (key == null) return false;

			if (key.key == null) {
				return GetButton(key.fallback_action_id, ignore_lah);
			}

			if (key.key.IsRedirect()) {
				return GetButton(key.key.GetKey());
			}

			return  (ignore_lah || CheckGun())
					&&
					!ImGuiConsole.console_open
					&&
					Input.GetKey((KeyCode) key.key.GetKey());
		}

		/// <summary>
		/// Check if a button was released this frame
		/// </summary>
		/// <param name="button"> Numerical id of the button, available through RewiredConsts namespace </param>
		/// <returns>
		/// True if the button was released this frame, false if not 
		/// </returns>
		public bool GetButtonUp(int button) {
			return GetButtonUp(button, false);
		}

		/// <summary>
		/// Check if a button was released this frame, skipping the holding-gun check
		/// </summary>
		/// <param name="button"> Numerical id of the button, available through RewiredConsts namespace </param>
		/// <param name="ignore_lah"> Whether or not to skip the check for the player holding the gun </param>
		/// <returns>
		/// True if the button was released this frame, false if not 
		/// </returns>
		public bool GetButtonUp(int button, bool ignore_lah) {
			return (
				ignore_lah || CheckGun()
				&& lah.character_input.GetButtonUp(button)
			);
		}

		/// <summary>
		/// Check if a button was released this frame
		/// </summary>
		/// <param name="key"> TODO </param>
		/// <returns>
		/// True if a button was released this frame, false otherwise
		/// </returns>
		public bool GetButtonUp(Keybind key) {
			return GetButtonUp(key, false);
		}

		/// <summary>
		/// Check if a button was released this frame, skipping the holding-gun check
		/// </summary>
		/// <param name="key"> TODO </param>
		/// <returns>
		/// True if a button was released this frame, false otherwise
		/// </returns>
		public bool GetButtonUp(Keybind key, bool ignore_lah) {
			if (key == null) return false;

			if (key.key == null) {
				return GetButtonUp(key.fallback_action_id, ignore_lah);
			}

			if (key.key.IsRedirect()) {
				return GetButtonUp(key.key.GetKey());
			}

			return  (ignore_lah || CheckGun())
					&&
					!ImGuiConsole.console_open
					&&
					Input.GetKeyUp((KeyCode) key.key.GetKey());
		}
	}
}
