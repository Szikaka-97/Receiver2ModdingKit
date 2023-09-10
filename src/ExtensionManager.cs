using System.Linq;
using UnityEngine;
using Receiver2;
using System.IO;
using SimpleJSON;
using Receiver2ModdingKit.ModInstaller;
using System.Collections.Generic;
using UnityEngine.Events;

namespace Receiver2ModdingKit {
	public static class Extensions {
		internal static JSONObject current_gun_data;
		internal static bool lah_force_bullet_display;

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

			var bullet_inventory = ReflectionManager.LAH_Get_Last_Bullet.Invoke(lah, new object[] { cartridge_dimensions });

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

		public static void ShowBullets(this LocalAimHandler lah, bool show_bullets) {
			lah_force_bullet_display = show_bullets;
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

		/// <summary>
		/// Helper method designed to use for the Thunderstore mod loading system
		/// </summary>
		/// <param name="dir"> The base directory </param>
		/// <param name="child_directory_name"> Name of the child directory </param>
		/// <param name="child_directory"> The child directory info, if it's not present child_directory.Exists will return false </param>
		/// <returns> True if a child directory is present, false otherwise </returns>
		public static bool TryGetChild(this DirectoryInfo dir, string child_directory_name, out DirectoryInfo child_directory) {
			string path = Path.Combine(dir.FullName, child_directory_name);

			child_directory = new DirectoryInfo(path);

			return Directory.Exists(path);
		}

		/// <summary>
		/// Cause the item to not spawn
		/// </summary>
		/// <param name="active_item"> Item to be disabled </param>
		public static void Disable(this ActiveItem active_item) {
			active_item.item_type = InventoryItem.Type.Unused;
		}

		/// <summary>
		/// Cause the item to spawn specified InventoryItem instead
		/// </summary>
		/// <param name="active_item"> Item spawn </param>
		/// <param name="item"> Item to be spawned </param>
		/// <param name="persistentData"> Additional data to be fed to SetPersistentData(), or null if previous data should be used </param>
		public static void SetItemSpawn(this ActiveItem active_item, InventoryItem item, JSONObject persistentData = null) {
			SetItemSpawn(active_item, item != null ? item.InternalName : "", persistentData);
		}

		/// <summary>
		/// Cause the item to spawn specified InventoryItem instead
		/// </summary>
		/// <param name="active_item"> Item spawn </param>
		/// <param name="item_internal_name"> Name of an item to be spawned </param>
		/// <param name="persistentData"> Additional data to be fed to SetPersistentData(), or null if previous data should be used </param>
		public static void SetItemSpawn(this ActiveItem active_item, string item_internal_name, JSONObject persistentData = null) {
			active_item.item_type = InventoryItem.Type.GenericHoldable;
			active_item.internal_name = item_internal_name;

			if (persistentData != null) {
				active_item.persistent_data = persistentData;
			}
		}

		/// <summary>
		/// Add specified items to the debug menu spawn list
		/// </summary>
		/// <param name="core_script"> ReceiverCoreScript instance </param>
		/// <param name="items"> Items to add to the list </param>
		/// <returns> True if items were added immidiately, False if the prefab list is locked and the items will be added once it's unlocked </returns>
		public static bool AddItemsToSpawnList(this ReceiverCoreScript core_script, params InventoryItem[] items) {
			if (ModLoader.rcs_prefab_list_locked) {
				ModLoader.prefab_list_queue.AddRange(items);

				return false;
			}
			else {
				core_script.generic_prefabs = core_script.generic_prefabs.Concat(items).ToArray();

				return true;
			}
		}

		/// <summary>
		/// Get the last checkpoint data that was loaded into the game when the gamemode started
		/// </summary>
		/// <param name="core_script"> ReceiverCoreScript instance </param>
		/// <returns> The checkpoint data used when the gamemode started </returns>
		public static JSONObject GetCheckpointData(this ReceiverCoreScript core_script) {
			return current_gun_data ?? new JSONObject();
		}

		/// <summary>
		/// Clear all persistent (set in the editor) method calls this event has
		/// </summary>
		/// <param name="unity_event"> Event whose method calls are to be cleared </param>
		public static void RemovePersistentListeners(this UnityEventBase unity_event) {
			if (unity_event.GetPersistentEventCount() == 0) return;

			object persistent_calls_group = ReflectionManager.UEB_persistent_calls.GetValue(unity_event);

			ReflectionManager.PCG_Clear.Invoke(persistent_calls_group, new object[] {});
		}

		public static void MoveTo(this FileInfo source_file, string destination_file, bool overwrite) {
			if (overwrite && File.Exists(destination_file)) File.Delete(destination_file);
			source_file.MoveTo(destination_file);
		}
	}
}
