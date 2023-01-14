using System.Linq;
using UnityEngine;
using Receiver2;

namespace Receiver2ModdingKit {
	static class Extensions {
		/// <summary>
		/// Fetch a round with of a specified type from player's inventory
		/// </summary>
		/// <param name="lah"> Passed in automatically if you call it like LocalAimHandler.player_instance.GetBullet(...) </param>
		/// <param name="cartridge_dimension"> Preset of the cartridge you want to get </param>
		/// <returns>
		/// ShellCasingScript of the first round found
		/// null if none were found or an error accured
		/// </returns>
		public static ShellCasingScript GetBullet(this LocalAimHandler lah, CartridgeSpec.Preset cartridge_dimension) {
			return lah.GetBullet(new CartridgeSpec.Preset[] { cartridge_dimension });
		}

		/// <summary>
		/// Fetch a round with of a specified type from player's inventory, this overload uses uint to be more handy for modded cartridges
		/// </summary>
		/// <param name="lah"> Passed in automatically if you call it like LocalAimHandler.player_instance.GetBullet(...) </param>
		/// <param name="cartridge_dimension"> Number of preset of the cartridge you want to get </param>
		/// <returns>
		/// ShellCasingScript of the first round found
		/// null if none were found or an error accured
		/// </returns>
		public static ShellCasingScript GetBullet(this LocalAimHandler lah, uint cartridge_dimension) {
			return lah.GetBullet(new CartridgeSpec.Preset[] { (CartridgeSpec.Preset) cartridge_dimension });
		}

		/// <summary>
		/// Fetch a round with preset matching one of specified types
		/// </summary>
		/// <param name="lah"> Passed in automatically if you call it like LocalAimHandler.player_instance.GetBullet(...) </param>
		/// <param name="cartridge_dimensions"> Array of cartridge presets to choose from </param>
		/// <returns>
		/// ShellCasingScript of the first round found
		/// null if none were found or an error accured
		/// </returns>
		public static ShellCasingScript GetBullet(this LocalAimHandler lah, CartridgeSpec.Preset[] cartridge_dimensions) {
			if (lah == null) {
				Debug.LogError("(Extension) LocalAimHandler.GetBullet(): This LocalAimHandler instance is null");
				return null;
			}

			if (cartridge_dimensions == null) {
				Debug.LogError("(Extension) LocalAimHandler.GetBullet(): You have to provide a CartridgeSpec array");
				return null;
			}

			var bullet_inventory = ReflectionManager.LAH_Get_Last_Bullet.Invoke(lah, cartridge_dimensions.Cast<object>().ToArray());

			if (bullet_inventory == null) return null;

			return (ShellCasingScript) ReflectionManager.LAH_BulletInventory_item.GetValue(bullet_inventory);
		}

		/// <summary>
		/// Fetch a round with preset matching one of specified types, this overload uses uint to be more handy for modded cartridges
		/// </summary>
		/// <param name="lah"> Passed in automatically if you call it like LocalAimHandler.player_instance.GetBullet(...) </param>
		/// <param name="cartridge_dimensions"> Array of cartridge preset numbers to choose from </param>
		/// <returns>
		/// ShellCasingScript of the first round found
		/// null if none were found or an error accured
		/// </returns>
		public static ShellCasingScript GetBullet(this LocalAimHandler lah, uint[] cartridge_dimensions) {
			return lah.GetBullet(cartridge_dimensions.Cast<CartridgeSpec.Preset>().ToArray());
		}

		/// <summary>
		/// Display a bullet shake effect, like when you cannot load more rounds into the magazine
		/// </summary>
		/// <param name="lah"> Passed in automatically if you call it like LocalAimHandler.player_instance.ShakeBullets() </param>
		public static void ShakeBullets(this LocalAimHandler lah) {
			if (lah != null) ReflectionManager.LAH_bullet_shake_time.SetValue(lah, Time.time);
		}

		/// <summary>
		/// Get a state for specified pose spring, used for controlling gun's position
		/// </summary>
		/// <param name="lah"> Passed in automatically if you call it like LocalAimHandler.player_instance.GetSpringState(...) </param>
		/// <param name="spring"> Spring which state you want to get </param>
		/// <returns>
		/// State of the specified spring, clamped from 0 to 1
		/// </returns>
		public static float GetSpringState(this LocalAimHandler lah, PoseSpring spring) {
			if (lah == null) {
				Debug.LogError("(Extension) LocalAimHandler.GetSpringState(): This LocalAimHandler instance is null");
				return 0;
			}

			return ((Spring) ReflectionManager.LAH_pose_springs[spring].GetValue(lah)).state;
		}
	}
}
