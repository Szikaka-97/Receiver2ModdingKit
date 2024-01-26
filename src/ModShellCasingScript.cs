using System;
using System.Collections.Generic;
using FMODUnity;
using Receiver2;
using UnityEngine;

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
		}
	}
}
