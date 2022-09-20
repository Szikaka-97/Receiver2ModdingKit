using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Receiver2;

namespace Receiver2ModdingKit {
	public class PlayerInput {
		private GunScript gun;
		private LocalAimHandler lah;

		public PlayerInput(GunScript gun) {
			this.gun = gun;
			this.lah = LocalAimHandler.player_instance;
		}

		public bool GetButtonDown(int button) {
			return (
				(lah.hands[1].state == LocalAimHandler.Hand.State.HoldingGun && gun.slot == lah.hands[1].slot)
				&& lah.character_input.GetButtonDown(button)
			);
		}
		public bool GetButtonDown(int button, bool ignore_lah) {
			return (
				ignore_lah || (lah.hands[1].state == LocalAimHandler.Hand.State.HoldingGun && gun.slot == lah.hands[1].slot)
				&& lah.character_input.GetButtonDown(button)
			);
		}

		public bool GetButton(int button) {
			return (
				(lah.hands[1].state == LocalAimHandler.Hand.State.HoldingGun && gun.slot == lah.hands[1].slot)
				&& lah.character_input.GetButton(button)
			);
		}
		public bool GetButton(int button, bool ignore_lah) {
			return (
				ignore_lah || (lah.hands[1].state == LocalAimHandler.Hand.State.HoldingGun && gun.slot == lah.hands[1].slot)
				&& lah.character_input.GetButton(button)
			);
		}

		public bool GetButtonUp(int button) {
			return (
				(lah.hands[1].state == LocalAimHandler.Hand.State.HoldingGun && gun.slot == lah.hands[1].slot)
				&& lah.character_input.GetButtonUp(button)
			);
		}
		public bool GetButtonUp(int button, bool ignore_lah) {
			return (
				ignore_lah || (lah.hands[1].state == LocalAimHandler.Hand.State.HoldingGun && gun.slot == lah.hands[1].slot)
				&& lah.character_input.GetButtonUp(button)
			);
		}
	}
}
