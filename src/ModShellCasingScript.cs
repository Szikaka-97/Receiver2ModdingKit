using System;
using System.Collections.Generic;
using FMODUnity;
using Receiver2;
using UnityEngine;

namespace Receiver2ModdingKit {
	public class ModShellCasingScript : ShellCasingScript {
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
		public string sound_shell_casing_impact_hard;

		[EventRef]
        public string sound_bullet_fall_hard;

        [EventRef]
        public string sound_shell_casing_impact_soft;

		public ModCartridgeSpec spec;
	}
}
