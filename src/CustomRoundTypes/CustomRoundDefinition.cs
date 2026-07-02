using System;
using System.Security.Policy;
using Receiver2;
using UnityEngine;
using UnityEngine.Events;

namespace Receiver2ModdingKit.CustomRounds {
	[Serializable]
	public class CustomRoundDefinition {
		public float extra_rotation_x;
		public float extra_rotation_y;
		public float extra_recoil_x;
		public float extra_recoil_y;
		public float extra_stovepipe_chance;
		public float extra_doublefeed_chance;
		public float extra_ftf_chance;
		public float extra_wrongly_seated_mag_chance;
		public float extra_out_of_battery_chance;
		public float extra_slamfire_chance;
		public float extra_slide_fire_speed;
		public float extra_wedged_amount;
		[Range(0f, 2f)] public float spawn_chance = 1f;

		public CartridgeSpec.Preset baseVariant;

		public CartridgeSpec.Preset cartridge;

		public string clean_name;

		public Action<ShellCasingScript> onRoundFired;
		public Func<bool> checkUnlockCondition;
		public Texture2D shootingRangeAmmoBoxTexture;

		public bool IsUnlocked => checkUnlockCondition == null || (checkUnlockCondition != null && checkUnlockCondition());
	}
}