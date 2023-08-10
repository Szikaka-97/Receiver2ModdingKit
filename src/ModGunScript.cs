using System;
using UnityEngine;
using Receiver2;
using SimpleJSON;
using BepInEx;

namespace Receiver2ModdingKit {
	/// <summary>
	/// Base class for all gun mods
	/// </summary>
	[RequireComponent(typeof(InventorySlot), typeof(LevelItem))]
	public abstract class ModGunScript : GunScript, ISerializationCallbackReceiver {
		[Serializable]
		public class ModLocaleTactics {
			public string title;
			[TextArea]
			public string description;

			public LocaleTactics GetLocaleTactics(GunScript gun) {
				return new LocaleTactics() {
					gun_internal_name = gun.InternalName,
					title = this.title.IsNullOrWhiteSpace() ? gun.InternalName : this.title,
					text = description
				};
			}
		}

		protected bool _disconnector_needs_reset {
			get { return (bool) ReflectionManager.GS_disconnector_needs_reset.GetValue(this); }
			set { ReflectionManager.GS_disconnector_needs_reset.SetValue(this, value); }
		}
		protected float _hammer_halfcocked {
			get { return (float) ReflectionManager.GS_hammer_halfcocked.GetValue(this); }
			set { ReflectionManager.GS_hammer_halfcocked.SetValue(this, value); }
		}
		protected float _hammer_cocked_val {
			get { return (float) ReflectionManager.GS_hammer_cocked_val.GetValue(this); }
			set { ReflectionManager.GS_hammer_cocked_val.SetValue(this, value); }
		}
		protected int _hammer_state {
			get { return (int) ReflectionManager.GS_hammer_state.GetValue(this); }
			set { ReflectionManager.GS_hammer_state.SetValue(this, value); }
		}
		protected bool _slide_stop_locked {
			get { return (bool) ReflectionManager.GS_slide_stop_locked.GetValue(this); }
			set { ReflectionManager.GS_slide_stop_locked.SetValue(this, value); }
		}
		protected LinearMover _select_fire {
			get { return (LinearMover) ReflectionManager.GS_select_fire.GetValue(this); }
			set { ReflectionManager.GS_select_fire.SetValue(this, value); }
		}
		protected float _yoke_open {
			get { return (float) ReflectionManager.GS_yoke_open.GetValue(this); }
			set { ReflectionManager.GS_yoke_open.SetValue(this, value); }
		}

		protected int _current_firing_mode_index {
			get { return (int) ReflectionManager.GS_current_firing_mode_index.GetValue(this); }
			set { ReflectionManager.GS_current_firing_mode_index.SetValue(this, value); }
		}

		public bool visible_in_spawnmenu = true;
		public bool spawns_in_dreaming = true;
		public ModHelpEntry help_entry;
		public ModLocaleTactics locale_tactics;

		[SerializeField]
		[HideInInspector]
		private bool help_entry_generate;
		[SerializeField]
		[HideInInspector]
		private string help_entry_name;
		[SerializeField]
		[HideInInspector]
		private Sprite help_entry_info_sprite;
		[SerializeField]
		[HideInInspector]
		private string help_entry_title;
		[SerializeField]
		[HideInInspector]
		private string help_entry_description;
		[SerializeField]
		[HideInInspector]
		private string locale_tactics_title;
		[SerializeField]
		[HideInInspector]
		private string locale_tactics_description;

		[Tooltip("List of custom audio events used by the gun, created via RMB -> Create -> Receiver 2 Modding -> Custom Sounds List")]
		public Editor.CustomSoundsList audio;
		
		public PlayerInput player_input {
			get;
			protected set;
		}

		/// <summary>
		/// Linearly interpolates between time-value pairs provided in curve parameter
		/// </summary>
		/// <param name="curve"> Array of timestamp-value pairs </param>
		/// <param name="time"> Time value to interpolate with </param>
		/// <returns>
		/// Interpolated value
		/// </returns>
		protected static float InterpCurve(in float[] curve, float time) {
			if (curve == null || curve.Length < 2) {
				Debug.LogError("ModGunScript.InterpCurve(): curve parameter must contain at least 2 elements");
				return 0;
			}

			if (time <= curve[0]) return curve[1];
			else if (time >= curve[curve.Length - 2]) return curve[curve.Length - 1];
			else {
				for (int i = 0; i < curve.Length - 2; i += 2) {
					if (time <= curve[i + 2]) {
						float t = Mathf.InverseLerp(curve[i], curve[i + 2], time);
						return Mathf.Lerp(curve[i + 1], curve[i + 3], t);
					}
				}
			}
			return curve[1];
		}

		/// <summary>
		/// Try to fire a round using the default method
		/// </summary>
		/// <param name="dry_fire_volume"> How loud should the dry fire click be </param>
		/// <returns>
		/// True if the bullet was fired
		/// False if not
		/// </returns>
		protected void TryFireBullet(float dry_fire_volume = 1f) {
			if (round_in_chamber && !round_in_chamber.IsSpent() && slide.amount == 0f) {
				FireBullet(round_in_chamber);
				return;
			}
			recoil_transfer_x -= UnityEngine.Random.Range(15f, 30f);
			recoil_transfer_y += UnityEngine.Random.Range(-20f, 20f);
			dry_fired = true;
			ReceiverEvents.TriggerEvent(ReceiverEventTypeInt.PlayerDryFire, (round_in_chamber != null) ? 2 : 1);
			AudioManager.PlayOneShotAttached(sound_dry_fire, gameObject, dry_fire_volume);
			return;
		}

		/// <summary>
		/// Try to fire a round using the supplied method. Useful if your gun fires different kinds of projectiles
		/// </summary>
		/// <param name="dry_fire_volume"> How loud should the dry fire click be </param>
		/// <param name="on_fire"> Function firing the bullet </param>
		/// <returns>
		/// True if the bullet was fired
		/// False if not
		/// </returns>
		protected void TryFireBullet(float dry_fire_volume, Action<ShellCasingScript> on_fire) {
			if (round_in_chamber && !round_in_chamber.IsSpent() && slide.amount == 0f) {
				on_fire.Invoke(round_in_chamber);
				return;
			}
			recoil_transfer_x -= UnityEngine.Random.Range(15f, 30f);
			recoil_transfer_y += UnityEngine.Random.Range(-20f, 20f);
			dry_fired = true;
			ReceiverEvents.TriggerEvent(ReceiverEventTypeInt.PlayerDryFire, (round_in_chamber != null) ? 2 : 1);
			AudioManager.PlayOneShotAttached(sound_dry_fire, gameObject, dry_fire_volume);
			return;
		}

		/// <summary>
		/// Update the position of all components in animated_components list
		/// </summary>
		protected void UpdateAnimatedComponents() {
			foreach (var component in this.animated_components) {
				ApplyTransform(component.anim_path, component.mover.amount, component.component);
			}
		}

		/// <summary>
		/// Set whether the gun should pierce basic turret armor
		/// </summary>
		/// <param name="armor_piercing"> AP value to be set </param>
		protected void SetArmorPiercing(bool armor_piercing) {
			if (armor_piercing != this.armor_piercing) {
				this.armor_piercing = armor_piercing;

				foreach (var armor_component in FindObjectsOfType<TurretArmor>()) armor_component.SendMessage("UpdateMaterial", this);
			}
		}

		new private void Awake() {
			player_input = new PlayerInput(this);

			if (this.spawn_info_sprite == null) {
				Debug.LogError("Your gun doesn't have a spawn_info_sprite assigned, it may cause problems later");
				this.spawn_info_sprite = Sprite.Create(Texture2D.blackTexture, Rect.zero, Vector2.zero);
			}

			using (var debug_scope = new TransformDebugScope()) {
				try {
					base.Awake();
				} catch (NullReferenceException e) {
					Debug.LogError(String.Format("Catched exception during {0}'s Awake", this.InternalName));

					if (!String.IsNullOrEmpty(TransformDebugScope.last_target)) {
						Debug.LogError("You seem to be missing the " + TransformDebugScope.last_target + " transform.");
					}
					else {
						Debug.LogError("Carefully check if you've assigned all the fields properly and if there aren't any missing references");
					}

					Debug.LogError(e);
				}
			}

			foreach (var spring in this.update_springs) {
				if (spring.spring == null) {
					Debug.LogError("Null spring in gun \"" + InternalName + "\"");
					continue;
				}
				spring.spring.orig_center = (spring.spring.transform.InverseTransformPoint(spring.spring.new_top.position) + spring.spring.transform.InverseTransformPoint(spring.spring.new_bottom.position)) / 2;
				spring.spring.orig_dist = Vector3.Distance(spring.spring.new_top.position, spring.spring.new_bottom.position);
			}

			if (this.audio != null) {
				audio.SetGun(this);
				
				ModdingKitConfig.AddConfigEventListener(delegate(object sender, BepInEx.Configuration.SettingChangedEventArgs args) {
					if (args.ChangedSetting.Definition.Key == "Use Custom Sounds") this.audio.SetSoundEvents();
				});

				this.audio.SetSoundEvents();
			}

			try {
				AwakeGun();
			} catch (Exception e) {
				Debug.LogError(String.Format("Catched exception during {0}'s AwakeGun", this.InternalName));
				Debug.LogException(e);
			}
		}
		new protected void Update() {
			using (var debug_scope = new TransformDebugScope()) { 
				try {
					base.Update();
				} catch (NullReferenceException e) {
					Debug.LogError(String.Format("Catched exception during {0}'s Update", this.InternalName));

					if (!String.IsNullOrEmpty(TransformDebugScope.last_target)) {
						Debug.LogError("You seem to be missing the " + TransformDebugScope.last_target + " transform.");
					}
					else {
						Debug.LogError("Carefully check if you've assigned all the fields properly and if there aren't any missing references");
					}

					Debug.LogError(e);
				}
			}

			UpdateAnimatedComponents();
			if (safety.transform) safety.UpdateDisplay();
		}

		/// <summary>
		/// Any setup that has to be done before the gun is ever used. It is called only once, right after a gun is loaded from the AssetBundle
		/// </summary>
		public virtual void InitializeGun() { }

		/// <summary>
		/// Setup that has to be done for every gun individually. It is called when the gun is spawned, whether for a campaign or from spawnmenu
		/// </summary>
		public virtual void AwakeGun() { }

		/// <summary>
		/// Get a help entry to use in the help menu.
		/// See <see cref="ModHelpEntry"/> for additional information
		/// </summary>
		/// 
		/// <returns>
		/// A ModHelpEntry object containing help menu information, or null if help menu entry shouldn't be created
		/// </returns>
		public virtual ModHelpEntry GetGunHelpEntry() {	return this.help_entry != null && this.help_entry.generate ? this.help_entry : null; }

		/// <summary>
		/// If the gun uses a custom round, override this method to define properties of said round
		/// </summary>
		/// <returns>
		/// A new CartridgeSpec defining custom round's properties
		/// </returns>
		[Obsolete("This method makes gun loading unnecessarily complicated, use ModShellCasingScript instead")]
		public virtual CartridgeSpec GetCustomCartridgeSpec() { 
			CartridgeSpec spec = new CartridgeSpec();
			spec.SetFromPreset(CartridgeSpec.Preset._9mm);
			return spec;
		}

		/// <summary>
		/// Get the gun name and trivia visible in the pause menu
		/// </summary>
		/// <returns>
		/// A LocaleTactics object containing info about the gun
		/// </returns>
		public virtual LocaleTactics GetGunTactics() {
			return locale_tactics.GetLocaleTactics(this);
		}

		/// <summary>
		/// A method performed every frame when the gun is active. Control things like firing the bullet within it
		/// </summary>
		public abstract void UpdateGun();

		internal static void UpdateModGun(GunScript gun) {
			if (gun is ModGunScript) {
				try {
					((ModGunScript) gun).UpdateGun();
				} catch (Exception e) {
					Debug.LogException(e);
				}
			}
		}

		public override string TypeName() { return "ModGunScript"; }
		public override JSONObject GetPersistentData() { return base.GetPersistentData(); }
		public override void SetPersistentData(JSONObject data) { base.SetPersistentData(data); }

		// Serialization shenanigans
		// BepInEx injected classes don't get serialized correctly, this is an attempt to remedy it
		public void OnBeforeSerialize() {
			this.help_entry_generate = help_entry.generate;
			this.help_entry_name = help_entry.name;
			this.help_entry_info_sprite = help_entry.info_sprite;
			this.help_entry_title = help_entry.title;
			this.help_entry_description = help_entry.description;

			this.locale_tactics_title = locale_tactics.title;
			this.locale_tactics_description = locale_tactics.description;
		}

		public void OnAfterDeserialize() {
			if (this.help_entry == null) {
				this.help_entry = new ModHelpEntry(help_entry_name) {
					generate = help_entry_generate,
					info_sprite = help_entry_info_sprite,
					title = help_entry_title,
					description = help_entry_description
				};
			}
			if (this.locale_tactics == null) {
				this.locale_tactics = new ModLocaleTactics() {
					title = locale_tactics_title,
					description = locale_tactics_description
				};
			}
		}
	}
}
