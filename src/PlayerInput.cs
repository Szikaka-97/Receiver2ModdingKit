using Receiver2;

namespace Receiver2ModdingKit {
	public class PlayerInput {
		private GunScript gun;
		private LocalAimHandler lah;

		public PlayerInput(GunScript gun) {
			this.gun = gun;
			this.lah = LocalAimHandler.player_instance;
		}

		/// <summary>
		/// Check if a button was pressed this frame
		/// </summary>
		/// <param name="button"> Numerical id of the button, available through RewiredConsts namespace </param>
		/// <returns>
		/// True if the button was pressed this frame, false if not
		/// </returns>
		public bool GetButtonDown(int button) {
			return (
				(lah.hands[1].state == LocalAimHandler.Hand.State.HoldingGun && gun.slot == lah.hands[1].slot)
				&& lah.character_input.GetButtonDown(button)
			);
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
				ignore_lah || (lah.hands[1].state == LocalAimHandler.Hand.State.HoldingGun && gun.slot == lah.hands[1].slot)
				&& lah.character_input.GetButtonDown(button)
			);
		}

		/// <summary>
		/// Check if a button is being held at this frame
		/// </summary>
		/// <param name="button"> Numerical id of the button, available through RewiredConsts namespace </param>
		/// <returns>
		/// True if a button is being held at this frame, false otherwise
		/// </returns>
		public bool GetButton(int button) {
			return (
				(lah.hands[1].state == LocalAimHandler.Hand.State.HoldingGun && gun.slot == lah.hands[1].slot)
				&& lah.character_input.GetButton(button)
			);
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
				ignore_lah || (lah.hands[1].state == LocalAimHandler.Hand.State.HoldingGun && gun.slot == lah.hands[1].slot)
				&& lah.character_input.GetButton(button)
			);
		}

		/// <summary>
		/// Check if a button was released this frame
		/// </summary>
		/// <param name="button"> Numerical id of the button, available through RewiredConsts namespace </param>
		/// <returns>
		/// True if the button was released this frame, false if not 
		/// </returns>
		public bool GetButtonUp(int button) {
			return (
				(lah.hands[1].state == LocalAimHandler.Hand.State.HoldingGun && gun.slot == lah.hands[1].slot)
				&& lah.character_input.GetButtonUp(button)
			);
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
				ignore_lah || (lah.hands[1].state == LocalAimHandler.Hand.State.HoldingGun && gun.slot == lah.hands[1].slot)
				&& lah.character_input.GetButtonUp(button)
			);
		}
	}
}
