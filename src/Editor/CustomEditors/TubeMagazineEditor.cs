#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Receiver2;

namespace Receiver2ModdingKit.Editor {
	[CustomEditor(typeof(TubeMagazineScript))]
	public class TubeMagazineEditor : UnityEditor.Editor {
		private bool cartridge_length_override;

		public int IndexOf(string value, string[] array) {
			for (int i = 1; i < array.Length; i++) {
				if (array[i] == value) return i;
			}
			return 0;
		}

		public string[] GetGunAnimations(GunScript gun) {
			var anim_list = new List<string>(gun.gun_animations.indices.Length + 1) { "None" };

			anim_list.AddRange(
				gun.gun_animations.indices.Select(index => index.path.Substring(0, index.path.IndexOf(" / ")))
			);

			return anim_list.ToArray();
		}

		public void OnEnable() {
			cartridge_length_override = (target as TubeMagazineScript).cartridge_length >= 0;
		}

		public override void OnInspectorGUI() {
			GUIStyle error_label = new GUIStyle(GUI.skin.label) {
				fontSize = 10,
				normal = new GUIStyleState() {
					textColor = Color.red
				}
			};

			TubeMagazineScript mag = target as TubeMagazineScript;

			serializedObject.Update();

			EditorGUILayout.PropertyField(serializedObject.FindProperty("round_prefab"), new GUIContent("Round Prefab"));

			GameObject round_prefab = (GameObject) serializedObject.FindProperty("round_prefab").objectReferenceValue;

			if (round_prefab == null) {
				EditorGUILayout.LabelField("This is a mandatory field", error_label);
			}
			else if (round_prefab.GetComponent<ShellCasingScript>() == null) {
				EditorGUILayout.LabelField("round_prefab needs a ShellCasingScript component", error_label);
			}

			EditorGUILayout.ObjectField(serializedObject.FindProperty("follower"), new GUIContent("Follower"));

			if (serializedObject.FindProperty("follower").objectReferenceValue == null) {
				EditorGUILayout.LabelField("This is a mandatory field", error_label);
			}

			EditorGUILayout.PropertyField(serializedObject.FindProperty("first_round_position"), new GUIContent("First Round Position"));

			Transform first_round_pos = serializedObject.FindProperty("first_round_position").objectReferenceValue as Transform;

			if (first_round_pos == null) {
				EditorGUILayout.LabelField("This is a mandatory field", error_label);
			}

			EditorGUILayout.Space(5);

			int capacity = EditorGUILayout.IntField("Capacity", serializedObject.FindProperty("capacity").intValue);

			capacity = Mathf.Clamp(capacity, 0, int.MaxValue);

			serializedObject.FindProperty("capacity").intValue = capacity;

			GunScript gun = mag.GetComponentInParent<GunScript>();

			if (gun != null) {
				string[] gun_animations = GetGunAnimations(gun);
				string current_insert_animation = serializedObject.FindProperty("insert_round_animation_path").stringValue;
				string current_remove_animation = serializedObject.FindProperty("remove_round_animation_path").stringValue;

				int index = 0;

				if (!string.IsNullOrWhiteSpace(current_insert_animation)) {
					index = IndexOf(current_insert_animation, gun_animations);
				}

				index = EditorGUILayout.Popup("Insert round animation", index, gun_animations);

				serializedObject.FindProperty("insert_round_animation_path").stringValue = index != 0 ? gun_animations[index] : "";

				index = 0;

				if (!string.IsNullOrWhiteSpace(current_remove_animation)) {
					index = IndexOf(current_remove_animation, gun_animations);
				}

				index = EditorGUILayout.Popup("Remove round animation", index, gun_animations);

				serializedObject.FindProperty("remove_round_animation_path").stringValue = index != 0 ? gun_animations[index] : "";
			}
			else {
				EditorGUILayout.PropertyField(serializedObject.FindProperty("insert_round_animation_path"), new GUIContent("Insert Round Animation Path"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("remove_round_animation_path"), new GUIContent("Remove Round Animation Path"));
			}

			SerializedProperty cartridge_length_prop = serializedObject.FindProperty("cartridge_length");

			if (cartridge_length_override = EditorGUILayout.Toggle("Override Cartridge Length", cartridge_length_override)) {
				if (cartridge_length_prop.floatValue < 0) {
					cartridge_length_prop.floatValue = -cartridge_length_prop.floatValue;
				}

				EditorGUILayout.PropertyField(cartridge_length_prop, new GUIContent("Cartridge Length"));
			}
			else {
				cartridge_length_prop.floatValue = -Mathf.Abs(cartridge_length_prop.floatValue);
			}

			EditorGUILayout.PropertyField(serializedObject.FindProperty("follower_offset"), new GUIContent("Follower Offset"));

			if (Application.isPlaying) {
				if (serializedObject.ApplyModifiedProperties()) {
					mag.Awake();
				}

				if (mag.is_valid) {
					float width = Mathf.Clamp(EditorGUIUtility.currentViewWidth - 100, 100, 400);

					EditorGUILayout.Space(20);

					GUILayout.Label("Round Count: " + mag.round_count);
					mag.round_count = (int) GUILayout.HorizontalSlider(mag.round_count, 0, mag.capacity, GUILayout.Width(width));

					EditorGUILayout.Space(40);

					if (GUILayout.Button("Add Round", GUILayout.Width(width))) {
						GameObject round = Instantiate(mag.round_prefab);

						if (!mag.TryInsertRound(round.GetComponent<ShellCasingScript>())) {
							Destroy(round);
						};
					}

					EditorGUILayout.Space(40);

					if (GUILayout.Button("Start Remove Round", GUILayout.Width(width))) {
						mag.StartRemoveRound();
					}
				
					EditorGUILayout.Space(40);
				
					if (GUILayout.Button("End Remove Round", GUILayout.Width(width))) {
						if (mag.TryRetrieveRound(out var shell)) {
							Destroy(shell.gameObject);
						}
					}
				}
				else {
					GUILayout.Label("Not all required fields are filled");
				}
			}
			else {
				EditorGUILayout.Space(10);

				serializedObject.ApplyModifiedProperties();

				GUILayout.Label("Enter play mode to test round positions");
			}
		}

		public void OnSceneGUI() {
			if ((target as TubeMagazineScript).first_round_position != null && Event.current.type == EventType.Repaint) {
				Handles.color = new Color(0.8f, 0.8f, 0.8f);
				Handles.ArrowHandleCap(0, (target as TubeMagazineScript).first_round_position.position, (target as TubeMagazineScript).first_round_position.rotation, 0.1f, EventType.Repaint);
			}
		}
	}
}

#endif