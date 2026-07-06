using UnityEngine;
using Receiver2;

#if UNITY_EDITOR

using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEditor;

namespace Receiver2ModdingKit.Editor {
	public class GunTester : MonoBehaviour {
		[CustomEditor(typeof(GunTester))]
		private class EditorImpl : UnityEditor.Editor {
			public override void OnInspectorGUI() {
				GunTester tester = target as GunTester;

				tester.UpdateKeys();
			}
		}

		private struct InputKey {
			public Keybind keybind;
			public int action;

			public InputKey(Keybind key) {
				this.keybind = key;
				this.action = -1;
			}

			public InputKey(int action) {
				this.keybind = null;
				this.action = action;
			}
		}

		private enum KeyState {
			None,
			Down,
			Pressed,
			Up
		}

		private Dictionary<int, string> input_name_map = new Dictionary<int, string>();

		private Dictionary<InputKey, bool> gui_states = new Dictionary<InputKey, bool>();
		private Dictionary<InputKey, KeyState> states = new Dictionary<InputKey, KeyState>();
		
		protected GunScript gun;

		protected void UpdateKeys() {
			if (Application.isPlaying && ModdingKitCorePlugin.instance == null) {
				GUIStyle errorStyle = GUI.skin.label;

				errorStyle.normal.textColor = Color.red;

				GUILayout.Label("Please include the ModdingKitCorePlugin component in your scene", errorStyle);

				return;
			}

			var keys = this.states.Keys.ToArray();

			foreach (var input in keys) {
				string input_name = input.keybind != null ? input.keybind.name : this.input_name_map[input.action];

				this.gui_states[input] = GUILayout.RepeatButton(input_name);
			}
		}

		private KeyState Poll(InputKey key) {
			if (states.ContainsKey(key)) {
				return states[key];
			}

			states[key] = KeyState.None;
			gui_states[key] = false;

			return KeyState.None;
		}

		public bool ButtonDown(int action) {
			return Poll(new InputKey(action)) == KeyState.Down;
		}
		
		public bool ButtonDown(Keybind key) {
			return Poll(new InputKey(key)) == KeyState.Down;
		}

		public bool ButtonPressed(int action) {
			return Poll(new InputKey(action)) == KeyState.Pressed;
		}
		
		public bool ButtonPressed(Keybind key) {
			return Poll(new InputKey(key)) == KeyState.Pressed;
		}

		public bool ButtonUp(int action) {
			return Poll(new InputKey(action)) == KeyState.Up;
		}
		
		public bool ButtonUp(Keybind key) {
			return Poll(new InputKey(key)) == KeyState.Up;
		}

		public void Awake() {
			foreach (var field in typeof(RewiredConsts.Action).GetFields(BindingFlags.Static | BindingFlags.Public)) {
				input_name_map[(int) field.GetValue(null)] = field.Name;
			}

			this.gun = this.GetComponent<GunScript>();
		}

		private float trigger_button_time;
		private float hammer_button_time;
		private float close_cylinder_time;
		private bool PullingTrigger;

		private void HandleGunControls() {
			if (ButtonDown(RewiredConsts.Action.Trigger)) {
				this.trigger_button_time = Time.realtimeSinceStartup;
				this.gun.CheckGunStatus();
			}

			if (ButtonDown(RewiredConsts.Action.Hammer)) {
				this.hammer_button_time = Time.realtimeSinceStartup;
			}
			
			float num2 = 0f;
			if (ButtonPressed(RewiredConsts.Action.Rotate_Cylinder_CW)) {
				num2 += 5f * Time.deltaTime;
			}
			if (ButtonPressed(RewiredConsts.Action.Rotate_Cylinder_CCW)) {
				num2 -= 5f * Time.deltaTime;
			}

			this.gun.SpinCylinder(Mathf.Clamp(num2, -1f, 1f));
			
			if (this.gun.trigger != null) {
				if (!this.PullingTrigger) {
					if (ButtonDown(RewiredConsts.Action.Trigger)) {
						ReceiverEvents.TriggerEvent(ReceiverEventTypeVoid.OnPlayerStartedPullingTrigger);
						this.PullingTrigger = true;
					}
				}
				else {
					float num3 = this.gun.base_trigger_profile.Evaluate(this.gun.trigger.amount);
					float num4 = Time.deltaTime * 20f / num3 / Time.timeScale;
					this.gun.SetAnalogTriggerPressure(Mathf.MoveTowards(Mathf.Max(this.gun.trigger.amount, this.gun.trigger.target_amount), 1f, num4));

					if (ButtonUp(RewiredConsts.Action.Trigger)) {
						this.PullingTrigger = false;
					}
				}
			}
			
			if (ButtonUp(RewiredConsts.Action.Trigger)) {
				this.gun.SetAnalogTriggerPressure(0f);
			}
			
			bool buttonDown = ButtonDown(RewiredConsts.Action.Slide_Lock);
			bool buttonUp = ButtonUp(RewiredConsts.Action.Slide_Lock);
			bool buttonDown2 = ButtonDown(RewiredConsts.Action.Toggle_Safety_Auto_Mod);
			if (buttonDown) {
				this.gun.StartSlideStop();
			}
			if (buttonUp) {
				this.gun.StopSlideStop();
				this.gun.chamber_check_performed = true;
			}
			if (ButtonPressed(RewiredConsts.Action.Slide_Lock)) {
				this.gun.CheckGunStatus();
			}
			if (this.gun.gun_model == GunModel.Sig226) {
				this.gun.safety.target_amount = (ButtonPressed(RewiredConsts.Action.Toggle_Safety_Auto_Mod) ? 1f : 0f);
			}
			if (buttonDown2) {
				this.gun.SwitchFireMode();
				this.gun.ToggleSafety();
			}
			

			if (this.gun.HasSlide()) {
				this.gun.slide.target_amount = 0f;

				if (ButtonPressed(RewiredConsts.Action.Pull_Back_Slide)) {
					float num5 = 1f;
					if (ButtonPressed(RewiredConsts.Action.Slide_Lock) && this.gun.slide.amount <= this.gun.press_check_amount)
					{
						num5 = this.gun.press_check_amount;
					}
					this.gun.slide.target_amount = Mathf.MoveTowards(this.gun.slide.amount, num5, Time.deltaTime * 10f);
					if (this.gun.slide.target_amount >= this.gun.press_check_amount - 0.05f)
					{
						this.gun.chamber_check_performed = true;
					}
					this.gun.CheckGunStatus();
				}
			}

			if (ButtonDown(RewiredConsts.Action.Close_Cylinder)) {
				this.gun.CloseCylinder();
				this.close_cylinder_time = Time.time + Random.Range(2f, 3f);
			}
			if (ButtonPressed(RewiredConsts.Action.Extractor_Rod)) {
				this.gun.ExtractorRod();
			}

			bool flag3 = this.gun.gun_model == GunModel.SAA && this.hammer_button_time > this.trigger_button_time && this.gun.HammerPartiallyCocked();
			if (ButtonPressed(RewiredConsts.Action.Hammer) || flag3) {
				float num6 = 1f;
				float num7 = 10f;

				if (this.gun.gun_model == GunModel.SAA && !ButtonPressed(RewiredConsts.Action.Trigger)) {
					num7 = 5f;
				}
				if (flag3 && !ButtonPressed(RewiredConsts.Action.Hammer)) {
					num6 = this.gun.GetHammerHalfCockAmount();
				}
				this.gun.SetAnalogHammerPressure(Mathf.MoveTowards(this.gun.hammer.amount, num6, Time.deltaTime * num7));
			}
			if (ButtonUp(RewiredConsts.Action.Hammer)) {
				this.gun.SetAnalogHammerPressure(0f);
			}
			
			if (!ButtonPressed(RewiredConsts.Action.Hammer)) {
				if (this.gun.hammer.amount == 0.01f) {
					this.gun.SetAnalogHammerPressure(Mathf.MoveTowards(this.gun.hammer.amount, 0f, Time.deltaTime * 10f));
					this.gun.hammer.vel = 0f;
				}
				else {
					this.gun.SetAnalogHammerPressure(Mathf.MoveTowards(this.gun.hammer.amount, 0.01f, Time.deltaTime * 10f));
					this.gun.hammer.vel = 0f;
				}
			}
		}

		public void Update() {
			foreach (var input in this.states.Keys.ToArray()) {
				KeyState state = this.states[input];

				if (this.gui_states[input]) {
					if (state == KeyState.None || state == KeyState.Up) {
						this.states[input] = KeyState.Down;
					}
					else {
						this.states[input] = KeyState.Pressed;
					}
				}
				else {
					if (state == KeyState.Pressed || state == KeyState.Down) {
						this.states[input] = KeyState.Up;
					}
					else {
						this.states[input] = KeyState.None;
					}
				}
			}

			HandleGunControls();
		}
	}
}

#else
	
namespace Receiver2ModdingKit.Editor {
	public class GunTester : MonoBehaviour {
		protected GunScript gun;

		public void Awake() { }
		public void Update() { }
	}
}

#endif