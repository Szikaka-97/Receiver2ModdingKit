#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using Receiver2;
using BepInEx;

namespace Receiver2ModdingKit.Editor {
	[CustomEditor(typeof(ModGunScript), true)]
	public class ModGunScriptEditor : UnityEditor.Editor{
		private enum ModGunType {
			Automatic,
			Revolver,
			Custom
		}

		string search_string = "";
		static string[] animation_indices = null;

		static bool collider_menu_open = false;

		private static string[] GetUniqueAnimationPaths(ModGunScript gun) {
			List<string> paths = new List<string>() { "" };

			if (gun == null || gun.gun_animations == null || gun.gun_animations.indices == null) {
				return new string[0];
			}

			foreach (var index in gun.gun_animations.indices) {
				if (index.path.LastIndexOf('/') < 0) {
					continue;
				}

				var path = index.path.Substring(0, index.path.LastIndexOf('/'));

				if (!paths.Contains(path)) {
					paths.Add(path);
				}
			}

			return paths.ToArray();
		}

		public static void PopulateAnimationData(SerializedProperty property) {
			EditorGUI.BeginChangeCheck();
			
			EditorGUILayout.PropertyField(property);

			if (EditorGUI.EndChangeCheck()) {
				animation_indices = GetUniqueAnimationPaths(property.serializedObject.targetObject as ModGunScript);
			}			
		}

		private static void HandleKeyframeFields(SerializedProperty property) {
			if (animation_indices.Length == 0) {
				return;
			}

			int currentIndex = 0;

			if (!property.stringValue.IsNullOrWhiteSpace()) {
				bool wasnt_found = true;

				while (currentIndex < animation_indices.Length) {
					if (property.stringValue == animation_indices[currentIndex]) {
						wasnt_found = false;

						break;
					}

					currentIndex++;
				}

				if (wasnt_found) {
					currentIndex = 0;
				}

			}

			currentIndex = EditorGUILayout.Popup(property.displayName, currentIndex, animation_indices);

			property.stringValue = animation_indices[currentIndex];
		}

		private static void HandleColliders(SerializedProperty property) {
			if (collider_menu_open = EditorGUILayout.Foldout(collider_menu_open, "Colliders")) {
				EditorGUI.indentLevel++;

				if (GUILayout.Button("Populate colliders")) {
					var gun = property.serializedObject.targetObject as ModGunScript;

					property.ClearArray();

					foreach (var collider in gun.GetComponentsInChildren<Collider>()) {
						property.InsertArrayElementAtIndex(0);
						property.GetArrayElementAtIndex(0).objectReferenceValue = collider;


						var collider_owner = collider.gameObject.TryGetComponent<ItemColliderOwner>(out var collider_owner_script) ? new SerializedObject(collider_owner_script) : new SerializedObject(collider.gameObject.AddComponent<ItemColliderOwner>());

						collider_owner.FindProperty("item_owner").objectReferenceValue = gun;

						collider_owner.ApplyModifiedProperties();

						if (collider.gameObject.layer != 8) {
							var serialized_collider_go = new SerializedObject(collider.gameObject);

							serialized_collider_go.Update();

							serialized_collider_go.FindProperty("m_Layer").intValue = 8;

							serialized_collider_go.ApplyModifiedProperties();
						}


					}
				}

				property.arraySize = EditorGUILayout.DelayedIntField("Size", property.arraySize);

				foreach (SerializedProperty collider_reference in property) {
					EditorGUILayout.PropertyField(collider_reference);
				}

				EditorGUI.indentLevel--;
			}
		}

		private static void HandleGunModel(SerializedProperty property) {
			EditorGUILayout.DelayedIntField(property);
		}

		private static void HandleGunType(SerializedProperty property) {
			property.intValue = (int) (ModGunType) EditorGUILayout.EnumPopup("Gun_type", (ModGunType) property.intValue);
		}

		private static void HandleGunAnimations(SerializedProperty property) {
			

			PopulateAnimationData(property);
		}

		private static Dictionary<string, Action<SerializedProperty>> special_functions_dict = new Dictionary<string, Action<SerializedProperty>>() {
			{ "gun_animations", HandleGunAnimations },
			{ "colliders", HandleColliders },
			{ "gun_model", HandleGunModel },
			{ "gun_type", HandleGunType },
			{ "anim_path_safety", HandleKeyframeFields },
			{ "extractor_rod_animation_path", HandleKeyframeFields },
			{ "firing_pin_safety_animation_path", HandleKeyframeFields },
			{ "loaded_chamber_indicator_animation_path", HandleKeyframeFields },
			{ "magazine_catch_animation_path", HandleKeyframeFields },
			{ "magazine_catch_mag_slide_animation", HandleKeyframeFields },
			{ "magazine_disconnect_animation_path", HandleKeyframeFields },
			{ "magazine_insert_animation_path", HandleKeyframeFields },
			{ "select_fire_animation_path", HandleKeyframeFields },
		};

		public void OnEnable() {
			ModGunScript gun = target as ModGunScript;

			if (gun.gun_animations != null) {
				animation_indices = GetUniqueAnimationPaths(gun);
			}
			else {
				animation_indices = new string[0];
			}

			// foreach (var property_name in gun.GetAnimationProperties()) {
			// 	special_functions_dict[property_name] = HandleKeyframeFields;
			// }
		}

		public override void OnInspectorGUI() {
			serializedObject.Update();

			bool searching = !search_string.IsNullOrWhiteSpace();

			search_string = EditorGUILayout.TextField("Search", search_string);

			EditorGUILayout.Space(20);

			var property = serializedObject.GetIterator();

			if (property.NextVisible(true)) {
				
				while (property.NextVisible(false)) {
					if (!searching || Regex.IsMatch(property.name, search_string)) {
						if (special_functions_dict.ContainsKey(property.name)) {
							special_functions_dict[property.name].Invoke(property);
						}
						else {
							EditorGUILayout.PropertyField(property);
						}
					}
				}
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}

#endif