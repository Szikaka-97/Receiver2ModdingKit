using System;
using System.Collections.Generic;
using FMODUnity;
using Receiver2;
using Receiver2ModdingKit.CustomRounds;
using UnityEngine;
using UnityEngine.Events;

namespace Receiver2ModdingKit {
	public class ModShellCasingScript : ShellCasingScript, ISerializationCallbackReceiver {
		internal static Dictionary<CartridgeSpec.Preset, CartridgeSpec> mod_cartridges = new Dictionary<CartridgeSpec.Preset, CartridgeSpec>();

		[Serializable]
		public class ModCartridgeSpec {
			[Tooltip("Mass of the empty casing, in grams")]
			public float casing_mass;
			[Tooltip("Mass of the bullet, in grams")]
			public float bullet_mass;
			[Tooltip("Muzzle speed of the projectile, in meters / second")]
			public float speed;
			[Tooltip("Diameter, or caliber, of the bullet")]
			public float diameter;
			[Tooltip("Density of the material, in grams / cc")]
			public float density = 11.34f;

			public CartridgeSpec CreateSpec() {
				return new CartridgeSpec {
					mass = this.bullet_mass,
					extra_mass = this.casing_mass,
					speed = this.speed,
					cylinder_length = (this.bullet_mass / this.density) / (Mathf.PI * this.diameter * this.diameter * 0.25f),
					diameter = this.diameter,
					density = this.density,
					gravity = true
				};
			}
		}

		[EventRef]
		public string sound_shell_casing_impact_hard = "event:/bullets/shell_casing_impact_hard";

		[EventRef]
		public string sound_bullet_fall_hard = "event:/bullets/bullet_fall_hard";

		[EventRef]
		public string sound_shell_casing_impact_soft = "event:/bullets/shell_casing_impact_soft";

		public ModCartridgeSpec spec;

		public CustomRoundDefinition customRoundTypeParameters;

		[SerializeField, HideInInspector] float _round_type_parameters_extra_rotation_x;
		[SerializeField, HideInInspector] float _round_type_parameters_extra_rotation_y;
		[SerializeField, HideInInspector] float _round_type_parameters_extra_recoil_x;
		[SerializeField, HideInInspector] float _round_type_parameters_extra_recoil_y;
		[SerializeField, HideInInspector] float _round_type_parameters_extra_stovepipe_chance;
		[SerializeField, HideInInspector] float _round_type_parameters_extra_doublefeed_chance;
		[SerializeField, HideInInspector] float _round_type_parameters_extra_ftf_chance;
		[SerializeField, HideInInspector] float _round_type_parameters_extra_wrongly_seated_mag_chance;
		[SerializeField, HideInInspector] float _round_type_parameters_extra_out_of_battery_chance;
		[SerializeField, HideInInspector] float _round_type_parameters_extra_slamfire_chance;
		[SerializeField, HideInInspector] float _round_type_parameters_extra_slide_fire_speed;
		[SerializeField, HideInInspector] float _round_type_parameters_extra_wedged_amount;
		[SerializeField, HideInInspector] float _round_type_parameters_spawn_chance;
		[SerializeField, HideInInspector] CartridgeSpec.Preset _round_type_parameters_baseVariant;
		[SerializeField, HideInInspector] CartridgeSpec.Preset _round_type_parameters_cartridge;
		[SerializeField, HideInInspector] string _round_type_parameters_clean_name;
		[SerializeField, HideInInspector] UnityAction<GunScript> _round_type_parameters_onRoundFired;
		[SerializeField, HideInInspector] Func<bool> _round_type_parameters_checkUnlockCondition;
		[SerializeField, HideInInspector] Texture2D _round_type_parameters_shootingRangeAmmoBoxTexture;

		[SerializeField]
		[HideInInspector]
		private float spec_casing_mass;
		[SerializeField]
		[HideInInspector]
		private float spec_bullet_mass;
		[SerializeField]
		[HideInInspector]
		private float spec_speed;
		[SerializeField]
		[HideInInspector]
		private float spec_diameter;
		[SerializeField]
		[HideInInspector]
		private float spec_density;

		public void OnBeforeSerialize() {
			this.spec_casing_mass = spec.casing_mass;
			this.spec_bullet_mass = spec.bullet_mass;
			this.spec_speed = spec.speed;
			this.spec_diameter = spec.diameter;
			this.spec_density = spec.density;

			if (customRoundTypeParameters != null) {
				_round_type_parameters_extra_rotation_x = customRoundTypeParameters.extra_rotation_x;
				_round_type_parameters_extra_rotation_y = customRoundTypeParameters.extra_rotation_y;
				_round_type_parameters_extra_recoil_x = customRoundTypeParameters.extra_recoil_x;
				_round_type_parameters_extra_recoil_y = customRoundTypeParameters.extra_recoil_y;
				_round_type_parameters_extra_stovepipe_chance = customRoundTypeParameters.extra_stovepipe_chance;
				_round_type_parameters_extra_doublefeed_chance = customRoundTypeParameters.extra_doublefeed_chance;
				_round_type_parameters_extra_ftf_chance = customRoundTypeParameters.extra_ftf_chance;
				_round_type_parameters_extra_wrongly_seated_mag_chance = customRoundTypeParameters.extra_wrongly_seated_mag_chance;
				_round_type_parameters_extra_out_of_battery_chance = customRoundTypeParameters.extra_out_of_battery_chance;
				_round_type_parameters_extra_slamfire_chance = customRoundTypeParameters.extra_slamfire_chance;
				_round_type_parameters_extra_slide_fire_speed = customRoundTypeParameters.extra_slide_fire_speed;
				_round_type_parameters_extra_wedged_amount = customRoundTypeParameters.extra_wedged_amount;
				_round_type_parameters_spawn_chance = customRoundTypeParameters.spawn_chance;
				_round_type_parameters_baseVariant = customRoundTypeParameters.baseVariant;
				_round_type_parameters_cartridge = customRoundTypeParameters.cartridge;
				_round_type_parameters_clean_name = customRoundTypeParameters.clean_name;
				_round_type_parameters_onRoundFired = customRoundTypeParameters.onRoundFired;
				_round_type_parameters_checkUnlockCondition = customRoundTypeParameters.checkUnlockCondition;
				_round_type_parameters_shootingRangeAmmoBoxTexture = customRoundTypeParameters.shootingRangeAmmoBoxTexture;
			}
		}

		public void OnAfterDeserialize() {
			if (this.spec == null) {
				this.spec = new ModCartridgeSpec() {
					casing_mass = spec_casing_mass,
					bullet_mass = spec_bullet_mass,
					speed = spec_speed,
					diameter = spec_diameter,
					density = spec_density
				};
			}

			if (this.customRoundTypeParameters == null && _round_type_parameters_cartridge != 0) {
				this.customRoundTypeParameters = new CustomRoundDefinition() {
					extra_rotation_x = _round_type_parameters_extra_rotation_x,
					extra_rotation_y = _round_type_parameters_extra_rotation_y,
					extra_recoil_x = _round_type_parameters_extra_recoil_x,
					extra_recoil_y = _round_type_parameters_extra_recoil_y,
					extra_stovepipe_chance = _round_type_parameters_extra_stovepipe_chance,
					extra_doublefeed_chance = _round_type_parameters_extra_doublefeed_chance,
					extra_ftf_chance = _round_type_parameters_extra_ftf_chance,
					extra_wrongly_seated_mag_chance = _round_type_parameters_extra_wrongly_seated_mag_chance,
					extra_out_of_battery_chance = _round_type_parameters_extra_out_of_battery_chance,
					extra_slamfire_chance = _round_type_parameters_extra_slamfire_chance,
					extra_slide_fire_speed = _round_type_parameters_extra_slide_fire_speed,
					extra_wedged_amount = _round_type_parameters_extra_wedged_amount,
					spawn_chance = _round_type_parameters_spawn_chance,
					baseVariant = _round_type_parameters_baseVariant,
					cartridge = _round_type_parameters_cartridge,
					clean_name = _round_type_parameters_clean_name,
					onRoundFired = _round_type_parameters_onRoundFired,
					checkUnlockCondition = _round_type_parameters_checkUnlockCondition,
					shootingRangeAmmoBoxTexture = _round_type_parameters_shootingRangeAmmoBoxTexture,
				};
			}
		}
	}
}
