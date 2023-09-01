#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using Receiver2;
using System;

namespace Receiver2ModdingKit.Editor {
	[EditorTool("Populate Gun Lists", typeof(GunScript))]
	public class MultiPopulateTool : EditorTool {
		[SerializeField]
		Texture2D icon;

		GUIContent guiContent;

		private GunScript target_gun;
		private SerializedObject serialized_gun;

		private bool x_ray_on;

		void OnEnable() {
			guiContent = new GUIContent() {
				image = icon,
				text = "Populate Gun Lists",
				tooltip = "Combination tool for filling up various gun lists"
			};

			target_gun = target as GunScript;
			serialized_gun = new SerializedObject(target_gun);

			x_ray_on = 
				target_gun != null
				&&
				target_gun.gun_part_materials != null
				&&
				target_gun.gun_part_materials.Length > 0
				&&
				target_gun.gun_part_materials[0].xray_material != null
				&&
				target_gun.gun_part_materials[0].GetComponent<Renderer>() != null
				&&
				target_gun.gun_part_materials[0].GetComponent<Renderer>().sharedMaterial == target_gun.gun_part_materials[0].xray_material;
		}

		private void PopulateSprings() {
			serialized_gun.FindProperty("update_springs").ClearArray();
			
			foreach(var spring in target_gun.GetComponentsInChildren<SpringCompressInstance>()) {
				serialized_gun.FindProperty("update_springs").InsertArrayElementAtIndex(0);

				var serialized_spring = serialized_gun.FindProperty("update_springs").GetArrayElementAtIndex(0);

				serialized_spring.FindPropertyRelative("update_direction").boolValue = false;
				serialized_spring.FindPropertyRelative("spring").objectReferenceValue = spring;
			}
		}

		private float[] KeyframeArrayToFloatArray(AnimatedComponent.Keyframe[] keyframes) {
			List<float> float_array = new List<float>();

			foreach (var keyframe in keyframes) {
				float_array.Add(keyframe.time);
				float_array.Add(keyframe.value);
			}

			return float_array.ToArray();
		}

		private void PopulateKeyframes() {
			GunScript gun = target as GunScript;

			Dictionary<string, float[]> animations = new Dictionary<string, float[]>();
			List<AnimatedMover> mover_components = new List<AnimatedMover>();

			foreach (var anim_index in gun.gun_animations.indices) {
				float[] keyframes = new float[anim_index.length * 2];

				Array.Copy(gun.gun_animations.keyframes, anim_index.index, keyframes, 0, anim_index.length * 2);

				animations[anim_index.path] = keyframes;
			}

			mover_components.AddRange(gun.animated_components);

			foreach (var animated_component in gun.GetComponentsInChildren<AnimatedComponent>()) {
				string base_path = animated_component.path + " / ";

				if (animated_component.X_pos_keyframes.Length > 0) {
					animations[base_path + "m_LocalPosition.x"] = KeyframeArrayToFloatArray(animated_component.X_pos_keyframes);
				}
				if (animated_component.Y_pos_keyframes.Length > 0) {
					animations[base_path + "m_LocalPosition.y"] = KeyframeArrayToFloatArray(animated_component.Y_pos_keyframes);
				}
				if (animated_component.Z_pos_keyframes.Length > 0) {
					animations[base_path + "m_LocalPosition.z"] = KeyframeArrayToFloatArray(animated_component.Z_pos_keyframes);
				}
				
				if (animated_component.X_rot_keyframes.Length > 0) {
					animations[base_path + "localEulerAnglesRaw.x"] = KeyframeArrayToFloatArray(animated_component.X_rot_keyframes);
				}
				if (animated_component.Y_rot_keyframes.Length > 0) {
					animations[base_path + "localEulerAnglesRaw.y"] = KeyframeArrayToFloatArray(animated_component.Y_rot_keyframes);
				}
				if (animated_component.Z_rot_keyframes.Length > 0) {
					animations[base_path + "localEulerAnglesRaw.z"] = KeyframeArrayToFloatArray(animated_component.Z_rot_keyframes);
				}

				if (!string.IsNullOrEmpty(animated_component.mover_name)) {
					bool flag = true;

					foreach (var component in mover_components) {
						if (component.anim_path == animated_component.path && component.mover_name == animated_component.mover_name && (component.component == animated_component.transform || component.component_name == animated_component.name)) {
							flag = false;

							break;
						}
					}

					if (flag) {
						mover_components.Add(new AnimatedMover() {
							anim_path = animated_component.path,
							mover_name = animated_component.mover_name,
							component = animated_component.transform,
						});
					}
				}
			}

			int keyframes_index = 0;
			SerializedObject serialized_gun = new SerializedObject(gun);

			SerializedProperty gun_animations = serialized_gun.FindProperty("gun_animations");

			gun_animations.FindPropertyRelative("keyframes").arraySize = 0;
			gun_animations.FindPropertyRelative("indices").arraySize = 0;

			foreach (var anim_pair in animations) {
				int index = gun_animations.FindPropertyRelative("indices").arraySize++;

				gun_animations.FindPropertyRelative("indices").GetArrayElementAtIndex(index).FindPropertyRelative("path").stringValue = anim_pair.Key;
				gun_animations.FindPropertyRelative("indices").GetArrayElementAtIndex(index).FindPropertyRelative("index").intValue = keyframes_index;
				gun_animations.FindPropertyRelative("indices").GetArrayElementAtIndex(index).FindPropertyRelative("length").intValue = anim_pair.Value.Length / 2;

				gun_animations.FindPropertyRelative("keyframes").arraySize += anim_pair.Value.Length;

				foreach (float keyframe in anim_pair.Value) {
					gun_animations.FindPropertyRelative("keyframes").GetArrayElementAtIndex(keyframes_index++).floatValue = keyframe;
				}

				serialized_gun.FindProperty("animated_components").arraySize = mover_components.Count;

				for (int i = 0; i < mover_components.Count; i++) {
					SerializedProperty mover_property = serialized_gun.FindProperty("animated_components").GetArrayElementAtIndex(i);
					AnimatedMover animated_mover = mover_components[i];

					mover_property.FindPropertyRelative("anim_path").stringValue = animated_mover.anim_path;
					mover_property.FindPropertyRelative("mover_name").stringValue = animated_mover.mover_name;
					mover_property.FindPropertyRelative("component").objectReferenceValue = animated_mover.component;
					mover_property.FindPropertyRelative("component_name").stringValue = animated_mover.component_name;
				}
			}
		}        

		private void PopulateGunPartMaterials() {
			serialized_gun.FindProperty("gun_part_materials").ClearArray();

			foreach (GunPartMaterial material in (target as GunScript).GetComponentsInChildren<GunPartMaterial>()) {
				serialized_gun.FindProperty("gun_part_materials").InsertArrayElementAtIndex(0);
				serialized_gun.FindProperty("gun_part_materials").GetArrayElementAtIndex(0).objectReferenceValue = material;
			}
		}

		private void ToggleXray(bool on) {
			GunScript gun = (GunScript) target;

			foreach (GunPartMaterial gun_part_material in gun.GetComponentsInChildren<GunPartMaterial>()) {
				if (gun_part_material && gun_part_material.TryGetComponent(out Renderer renderer)) {
					renderer.sharedMaterial = on ? gun_part_material.xray_material : gun_part_material.material;
				}
			}
		}

		private void PopulateColliders() {           
			serialized_gun.FindProperty("colliders").ClearArray();

			foreach (var collider in target_gun.GetComponentsInChildren<Collider>()) {
				serialized_gun.FindProperty("colliders").InsertArrayElementAtIndex(0);
				serialized_gun.FindProperty("colliders").GetArrayElementAtIndex(0).objectReferenceValue = collider;

				var collider_owner = new SerializedObject(collider.gameObject.AddComponent<ItemColliderOwner>());

				collider_owner.FindProperty("item_owner").objectReferenceValue = serialized_gun.targetObject;

				collider_owner.ApplyModifiedProperties();

				if (collider.gameObject.layer != 8) {
					var serialized_collider_go = new SerializedObject(collider.gameObject);

					serialized_collider_go.FindProperty("layer").intValue = 8;

					serialized_collider_go.ApplyModifiedProperties();
				}
			}
		}

		public override void OnToolGUI(EditorWindow window) {
			var background_style = new GUIStyle(EditorStyles.helpBox);

			Handles.BeginGUI();

			using (new GUILayout.VerticalScope(background_style, GUILayout.Width(180))) {
				if (GUILayout.Button("Populate Springs")) {
					PopulateSprings();
				}

				GUILayout.Space(2);
				GUILayout.Box(Texture2D.blackTexture, GUILayout.Height(2), GUILayout.Width(180));
				GUILayout.Space(2);

				if (GUILayout.Button("Populate Animations")) {
					PopulateKeyframes();
				}

				GUILayout.Space(2);
				GUILayout.Box(Texture2D.blackTexture, GUILayout.Height(2), GUILayout.Width(180));
				GUILayout.Space(2);

				if (GUILayout.Button("Populate Gun Part Materials")) {
					PopulateGunPartMaterials();
				}

				EditorGUI.BeginChangeCheck();

				x_ray_on = GUILayout.Toggle(x_ray_on, "Toggle X-Ray");

				if (EditorGUI.EndChangeCheck()) {
					ToggleXray(x_ray_on);
				}

				GUILayout.Space(2);
				GUILayout.Box(Texture2D.blackTexture, GUILayout.Height(2), GUILayout.Width(180));
				GUILayout.Space(2);

				if (GUILayout.Button("Populate Gun Colliders")) {
					PopulateColliders();
				}
			}

			serialized_gun.ApplyModifiedProperties();

			Handles.EndGUI();
		}
	}
}

#endif