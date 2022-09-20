using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Receiver2;

namespace Receiver2ModdingKit {
	static class ExtensionManager {
		public static ShellCasingScript GetBullet(this LocalAimHandler lah, CartridgeSpec.Preset[] cartridge_dimensions) {
			var bullet_inventory = ReflectionManager.LAH_Get_Last_Bullet.Invoke(lah, cartridge_dimensions.Cast<object>().ToArray());

			if (bullet_inventory == null) return null;

			return (ShellCasingScript) ReflectionManager.LAH_BulletInventory_item.GetValue(bullet_inventory);
		}

		public static void ShakeBullets(this LocalAimHandler lah) {
			ReflectionManager.LAH_bullet_shake_time.SetValue(lah, Time.time);
		}
	}
}
