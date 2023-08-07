using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.IO;
using UnityEngine;
using FMODUnity;
using Receiver2ModdingKit.CustomSounds;
using Receiver2;

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

		[Tooltip("Prefix used with your sounds")]
		public string prefix;

		[Tooltip("Path to the folder where your sounds are located starting in game's AppData folder")]
		public string sound_path;

		public string[] sound_events = new string[31];
		public string[] fallback_events = new string[31];

		public List<string> custom_event_names = new List<string>();
		public List<string> custom_event_values = new List<string>();
		public List<string> custom_event_fallback_values = new List<string>();

		public ForceOptions force_options = ForceOptions.Nothing;

		private ModGunScript gun;
		private FieldInfo[] sound_fields;
		private List<FieldInfo> custom_sound_fields = new List<FieldInfo>();
		private Dictionary<FieldInfo, string> custom_sound_events_queue = new Dictionary<FieldInfo, string>();

		public void Initialize(ModGunScript gun) {
			sound_fields = typeof(ModGunScript).GetFields(BindingFlags.Instance | BindingFlags.Public).Where(field => field.GetCustomAttributes<EventRefAttribute>().Count() > 0).ToArray();

			if (string.IsNullOrEmpty(this.prefix)) {
				Debug.LogError("Prefix for gun " + gun.InternalName + "is empty, falling back to gun's name");
				this.prefix = gun.InternalName.Substring(this.prefix.IndexOf(".") + 1);
			}

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

				string old_prefix = this.prefix;

				this.prefix = this.prefix + "_" + iterations;

				foreach(var field in sound_fields) {
					string event_name = (string) field.GetValue(gun);

					if (event_name.Contains(old_prefix)) {
						event_name.Replace(old_prefix, this.prefix);
					}

					field.SetValue(gun, event_name);
				}

				Debug.LogWarning("Tried to load sounds with a duplicate prefix \"" + prev_prefix + "\". They were loaded with \"" + this.prefix + "\" instead");

			}

			if (!string.IsNullOrEmpty(sound_path)) {
				ModAudioManager.LoadCustomEvents(this.prefix, Path.IsPathRooted(sound_path) ? sound_path : Path.Combine(Application.persistentDataPath, sound_path));
			}
			else {
				Debug.LogError("sound_path of audio list for gun " + gun.InternalName + " is empty, falling back on default sounds");

				force_options = ForceOptions.ForceDefaultSounds;
			}

			if (this.custom_event_names.Count > this.custom_event_values.Count) this.custom_event_values.AddRange(new string[this.custom_event_values.Count - this.custom_event_names.Count]);
			if (this.custom_event_names.Count > this.custom_event_fallback_values.Count) this.custom_event_fallback_values.AddRange(new string[this.custom_event_fallback_values.Count - this.custom_event_names.Count]);

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
				sound_fields.First(info => info.Name == sound_pair.Key).SetValue(gun, sound_option ? sound_events[sound_pair.Value] : fallback_events[sound_pair.Value]);
			}

			foreach (var field in custom_sound_fields) {
				field.SetValue(gun, GetCustomSoundEvent(field.Name));
			}
		}

		public string GetCustomSoundEvent(string name) {
			int index = custom_event_names.IndexOf(name);

			if (index < 0) return "";

			return ModdingKitConfig.use_custom_sounds.Value ? custom_event_values[index] : custom_event_fallback_values[index];
		}

		internal void BindField(FieldInfo field) {
			int index = custom_event_names.IndexOf(field.Name);

			if (index >= 0) {
				custom_sound_fields.Add(field);

				custom_sound_events_queue.Add(field, ModdingKitConfig.use_custom_sounds.Value ? custom_event_values[index] : custom_event_fallback_values[index]);
			}
			else {
				Debug.LogError("CustomSoundsList.SetSoundEvents(): No custom sound event with a name \"" + field.Name + "\"");
			}
		}

		public void SetGun(ModGunScript gun) {
			this.gun = gun;
		}
	}
}