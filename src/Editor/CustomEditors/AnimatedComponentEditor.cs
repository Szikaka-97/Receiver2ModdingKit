#if UNITY_EDITOR

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Receiver2ModdingKit.Editor {
	[CustomEditor(typeof(AnimatedComponent))]
	public class AnimatedComponentEditor : UnityEditor.Editor {
		private Pose original_pose;
		private bool moved_by_slider = false;
		private float time = 0;
		private bool propagate;

		private bool x_pos_dropdown = false;
		private bool y_pos_dropdown = false;
		private bool z_pos_dropdown = false;
		private bool x_rot_dropdown = false;
		private bool y_rot_dropdown = false;
		private bool z_rot_dropdown = false;

		private Tuple<string, string>[] mover_names = new Tuple<string, string>[] {
			new Tuple<string, string>("None", ""),
			new Tuple<string, string>("Firing Pin", "firing_pin"),
			new Tuple<string, string>("Hammer", "hammer"),
			new Tuple<string, string>("Safety Switch", "safety"),
			new Tuple<string, string>("Sear", "sear"),
			new Tuple<string, string>("Slide", "slide"),
			new Tuple<string, string>("Trigger", "trigger"),
			new Tuple<string, string>("Magazine Catch", "magazine_catch"),
			new Tuple<string, string>("Extractor Rod", "extractor_rod"),
			new Tuple<string, string>("Fire Mode Selector", "select_fire"),
			new Tuple<string, string>("Loaded Chamber Indicator", "loaded_chamber_indicator"),
		};

		private void DrawKeyframe(ref bool dropdown, SerializedProperty keyframes_array, string array_name) {
			if (dropdown = EditorGUILayout.Foldout(dropdown, new GUIContent(array_name))) {
				EditorGUI.indentLevel++;

				using (new EditorGUILayout.HorizontalScope()) {
					EditorGUILayout.LabelField("Time:");
					EditorGUILayout.LabelField("Value:");
				}

				foreach (SerializedProperty keyframe in keyframes_array) {
					using (new EditorGUILayout.HorizontalScope()) {
						EditorGUILayout.PropertyField(keyframe.FindPropertyRelative("time"), new GUIContent());
						EditorGUILayout.PropertyField(keyframe.FindPropertyRelative("value"), new GUIContent());
					}
				}

				using (new EditorGUILayout.HorizontalScope()) {
					if (GUILayout.Button("-") && keyframes_array.arraySize > 0) {
						keyframes_array.arraySize--;
					}
					if (GUILayout.Button("+")) {
						keyframes_array.arraySize++;
					}
				}

				EditorGUI.indentLevel--;
			}
		}

		public void HandleMovement() {
			EditorGUI.BeginChangeCheck();
			
			propagate = EditorGUILayout.Toggle("Propagate animation", propagate);

			time = EditorGUILayout.Slider("Animation Progress:", time, 0, 1);

			var anim = target as AnimatedComponent;
			List<AnimatedComponent> animated_components = new List<AnimatedComponent>() { anim };

			if (propagate) animated_components.AddRange(anim.GetComponentsInChildren<AnimatedComponent>());

			if (EditorGUI.EndChangeCheck()) {
				Undo.RecordObject(target, "Move point");

				foreach (var animated_component in animated_components) {
					if (animated_component.mover_name != anim.mover_name) continue;

					ApplyMovement(animated_component, out var new_position, out var new_euler_angles);

					animated_component.transform.localPosition = new_position;
					animated_component.transform.localEulerAngles = new_euler_angles;
				}

				moved_by_slider = true;
			}

			if (GUILayout.Button("Apply")) {
				moved_by_slider = false;

				PrefabUtility.RecordPrefabInstancePropertyModifications(anim.transform);
			}
		}

		public void ApplyMovement(AnimatedComponent animated_component, out Vector3 new_position, out Vector3 new_euler_angles) {
			propagate = EditorGUILayout.Toggle("Propagate animation", propagate);

			EditorGUI.BeginChangeCheck();

			time = EditorGUILayout.Slider("Animation Progress:", time, 0, 1);

			new_position = new Vector3(
				animated_component.X_pos_keyframes.Length > 0 ? ApplyKeyframe(animated_component.X_pos_keyframes) : animated_component.transform.localPosition.x,
				animated_component.Y_pos_keyframes.Length > 0 ? ApplyKeyframe(animated_component.Y_pos_keyframes) : animated_component.transform.localPosition.y,
				animated_component.Z_pos_keyframes.Length > 0 ? ApplyKeyframe(animated_component.Z_pos_keyframes) : animated_component.transform.localPosition.z
			);

			new_euler_angles = new Vector3(
				animated_component.X_rot_keyframes.Length > 0 ? ApplyKeyframe(animated_component.X_rot_keyframes) : animated_component.transform.localEulerAngles.x,
				animated_component.Y_rot_keyframes.Length > 0 ? ApplyKeyframe(animated_component.Y_rot_keyframes) : animated_component.transform.localEulerAngles.y,
				animated_component.Z_rot_keyframes.Length > 0 ? ApplyKeyframe(animated_component.Z_rot_keyframes) : animated_component.transform.localEulerAngles.z
			);
		}

		public float ApplyKeyframe(AnimatedComponent.Keyframe[] keyframes_array) {
			if (time < keyframes_array[0].time) return keyframes_array[0].value;

			for (int i = 0; i < keyframes_array.Length - 1; i++) {
				if (time < keyframes_array[i].time || time > keyframes_array[i + 1].time) continue;

				float timeA = keyframes_array[i].time;
				float valA = keyframes_array[i].value;

				float timeB = keyframes_array[i + 1].time;
				float valB = keyframes_array[i + 1].value;

				float alpha = (time - timeA) / (timeB - timeA);

				return Mathf.Lerp(valA, valB, alpha);
			}

			return keyframes_array[keyframes_array.Length - 1].value;
		}

		public void OnEnable() {
			original_pose = new Pose(((AnimatedComponent) target).transform.localPosition, ((AnimatedComponent) target).transform.localRotation);
		}

		public override void OnInspectorGUI() {
			serializedObject.Update();

			EditorGUILayout.PropertyField(serializedObject.FindProperty("path"), new GUIContent("Path:"));

			int index;

			for (index = 0; index <= mover_names.Length; index++) {
				if (index == mover_names.Length) {
					index = 0;
					break;
				}
				if (serializedObject.FindProperty("mover_name").stringValue == mover_names[index].Item2) break;
			}

			index = EditorGUILayout.Popup("Mover", index, mover_names.Select( t => t.Item1 ).ToArray());

			serializedObject.FindProperty("mover_name").stringValue = mover_names[index].Item2;

			HandleMovement();

			DrawKeyframe(ref x_pos_dropdown, serializedObject.FindProperty("X_pos_keyframes"), "X Position");
			DrawKeyframe(ref y_pos_dropdown, serializedObject.FindProperty("Y_pos_keyframes"), "Y Position");
			DrawKeyframe(ref z_pos_dropdown, serializedObject.FindProperty("Z_pos_keyframes"), "Z Position");

			DrawKeyframe(ref x_rot_dropdown, serializedObject.FindProperty("X_rot_keyframes"), "X Rotation");
			DrawKeyframe(ref y_rot_dropdown, serializedObject.FindProperty("Y_rot_keyframes"), "Y Rotation");
			DrawKeyframe(ref z_rot_dropdown, serializedObject.FindProperty("Z_rot_keyframes"), "Z Rotation");

			serializedObject.ApplyModifiedProperties();
		}

		public void OnDestroy() {

		}

		public void OnDisable() {
			if (target != null && moved_by_slider) {
				((AnimatedComponent) target).transform.localPosition = original_pose.position;
				((AnimatedComponent) target).transform.localRotation = original_pose.rotation;
			}
		}
	}
}

#endif