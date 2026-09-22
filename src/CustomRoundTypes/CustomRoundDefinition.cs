using System;
using System.Security.Policy;
using Receiver2;
using UnityEngine;
using UnityEngine.Events;

namespace Receiver2ModdingKit.CustomRounds {
	[Serializable]
	public class CustomRoundDefinition {
		/// <summary>
		/// The gun's vertical kick.
		/// </summary>
		[Tooltip("The gun's vertical kick.")]
		public float extra_rotation_x;

		/// <summary>
		/// The gun's horizontal kick.
		/// </summary>
		[Tooltip("The gun's horizontal kick.")]
		public float extra_rotation_y;

		/// <summary>
		/// The gun's horizontal wobble.
		/// </summary>
		[Tooltip("The gun's horizontal wobble.")]
		public float extra_recoil_x;

		/// <summary>
		/// The gun's vertical wobble.
		/// </summary>
		[Tooltip("The gun's vertical wobble.")]
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

		public Action<GunScript> onRoundFired;
		public Func<bool> checkUnlockCondition;
		public Texture2D shootingRangeAmmoBoxTexture;

		public bool IsUnlocked => checkUnlockCondition == null || (checkUnlockCondition != null && checkUnlockCondition());
	}
}