using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.IO;
using UnityEngine;
using FMODUnity;

using Receiver2ModdingKit.CustomSounds;

namespace Receiver2ModdingKit.Editor {
	[CreateAssetMenu(fileName = "Custom Sounds list", menuName = "Receiver 2 Modding/Custom Sounds list")]
	public class CustomSoundsList : ScriptableObject {
		public enum ForceOptions {
			Nothing,
			ForceModdedSounds,
			ForceDefaultSounds
		}

		public static readonly Dictionary<string, int> sound_event_lookup = new Dictionary<string, int>() {
			{"sound_event_gunshot", 0},
			{"sound_dry_fire", 1},
			{"sound_safety_on", 2},
			{"sound_safety_off", 3},
			{"sound_start_insert_mag", 4},
			{"sound_insert_mag_empty", 5},
			{"sound_insert_mag_loaded", 6},
			{"sound_eject_mag_empty", 7},
			{"sound_eject_mag_loaded", 8},
			{"sound_double_feed", 9},
			{"sound_double_feed_mag_release", 10},
			{"sound_extractor_rod_extend", 11},
			{"sound_extractor_rod_retract", 12},
			{"sound_round_start_eject", 13},
			{"sound_cylinder_open", 14},
			{"sound_cylinder_close", 15},
			{"sound_cylinder_index", 16},
			{"sound_press_check_start", 17},
			{"sound_press_check_end", 18},
			{"sound_barrel_lock", 19},
			{"sound_barrel_unlock", 20},
			{"sound_eject_bullet", 21},
			{"sound_slide_back", 22},
			{"sound_slide_back_partial", 23},
			{"sound_slide_hit_lock", 24},
			{"sound_slide_released", 25},
			{"sound_cocked", 26},
			{"sound_decock", 27},
			{"sound_trigger_forwards", 28},
			{"sound_trigger_blocked", 29},
			{"sound_trigger_reset", 30}
		};

		public string prefix;
		public string sound_path;

		public string[] sound_events = new string[31];
		public string[] fallback_events = new string[31];

		[NonSerialized]
		public ForceOptions force_options = ForceOptions.Nothing;

		private ModGunScript gun;
		private FieldInfo[] sound_fields;

		public void Initialize(ModGunScript gun) {
			sound_fields = typeof(ModGunScript).GetFields(BindingFlags.Instance | BindingFlags.Public).Where(field => field.GetCustomAttributes<EventRefAttribute>().Count() > 0).ToArray();

			if (ModAudioManager.IsPrefixPresent(this.prefix)) {
				string prev_prefix = this.prefix;
				int iterations = 1;

				while (ModAudioManager.IsPrefixPresent(this.prefix + "_" + iterations)) {
					if (iterations > 32) {
						Debug.LogError("CustomSoundsList.Initialize(): Prefix \"" + this.prefix + "\" was loaded over 32 times and the next attempt was aborted, something probably went wrong");
						return;
					}

					iterations++;
				}

				this.prefix = this.prefix + "_" + iterations;

				Debug.LogWarning("Tried to load sounds with a duplicate prefix \"" + prev_prefix + "\". They were loaded with \"" + this.prefix + "\" instead");

			}

			ModAudioManager.LoadCustomEvents(this.prefix, Path.IsPathRooted(sound_path) ? sound_path : Path.Combine(Application.persistentDataPath, sound_path));

			if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("R2CustomSounds")) {
				BepInEx.Bootstrap.Chainloader
					.PluginInfos["R2CustomSounds"].Instance
					.GetType().Assembly.GetType("R2CustomSounds.ModAudioManager")
					.GetMethod("LoadCustomEvents").Invoke(null, new object[2] {
						this.prefix, Path.IsPathRooted(sound_path) ? sound_path : Path.Combine(Application.persistentDataPath, sound_path)
					});
			}
		}

		public void SetSoundEvents() {
			if (gun == null) {
				Debug.LogError("CustomSoundsList.SetSoundEvents(): Attempted to set sound events before calling Initialize()");
				return;
			}

			bool sound_option = (ModdingKitConfig.use_custom_sounds.Value || force_options == ForceOptions.ForceModdedSounds) && force_options != ForceOptions.ForceDefaultSounds;

			foreach (var sound_pair in sound_event_lookup) {
				sound_fields.Single(info => info.Name == sound_pair.Key).SetValue(gun, sound_option ? sound_events[sound_pair.Value] : fallback_events[sound_pair.Value]);
			}
		}

		public void SetGun(ModGunScript gun) {
			this.gun = gun;
		}
	}
}
