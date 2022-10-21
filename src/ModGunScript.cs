using System;
using System.Linq;
using UnityEngine;
using Receiver2;
using SimpleJSON;
using static Receiver2.Constants;
using System.Reflection;

namespace Receiver2ModdingKit {
	public abstract class ModGunScript : GunScript {

		private class AwakePatcher {

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

		public bool generate_settings_button;
		public Editor.CustomSoundsList audio;
		public PlayerInput player_input {
			get;
			protected set;
		}

		protected static float InterpCurve(in float[] curve, float time) {
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

		protected void TryFireBullet(float dry_fire_volume) {
			if (round_in_chamber && !round_in_chamber.IsSpent() && slide.amount == 0f) {
				FireBullet(round_in_chamber);
				return;
			}
			recoil_transfer_x -= UnityEngine.Random.Range(15f, 30f);
			recoil_transfer_y += UnityEngine.Random.Range(-20f, 20f);
			dry_fired = true;
			ReceiverEvents.TriggerEvent(ReceiverEventTypeInt.PlayerDryFire, (round_in_chamber != null) ? 2 : 1);
			AudioManager.PlayOneShotAttached(sound_dry_fire, gameObject, dry_fire_volume);
		}

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
		}

		protected void UpdateAnimatedComponents() {
			foreach (var component in this.animated_components) {
				ApplyTransform(component.anim_path, component.mover.amount, component.component);
			}
		}

		new public void Awake() {
			player_input = new PlayerInput(this);

			try {
				base.Awake();
			} catch (NullReferenceException e) {
				Debug.LogError(e.StackTrace);
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
		new public void Update() {
			base.Update();

			if (safety.transform) safety.UpdateDisplay();
		}

		public virtual void InitializeGun() { }
		public virtual void AwakeGun() { base.Awake(); }
		public virtual ModHelpEntry GetGunHelpEntry() {	return null; }
		public virtual CartridgeSpec GetCustomCartridgeSpec() { 
			CartridgeSpec spec = new CartridgeSpec();
			spec.SetFromPreset(CartridgeSpec.Preset._9mm);
			return spec;
		}
		public virtual LocaleTactics GetGunTactics() {
			return new LocaleTactics() {
				gun_internal_name = InternalName,
				title = gameObject.name
			};
		}

		public abstract void UpdateGun();

		public override string TypeName() { return "ModGunScript"; }
		public override JSONObject GetPersistentData() { return base.GetPersistentData(); }
		public override void SetPersistentData(JSONObject data) { base.SetPersistentData(data); }
	}
}
