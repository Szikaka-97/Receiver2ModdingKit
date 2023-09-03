#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using Receiver2;

namespace Receiver2ModdingKit.Editor {
	[CustomEditor(typeof(TubeMagazineScript))]
	public class TubeMagazineEditor : UnityEditor.Editor {
		public override void OnInspectorGUI() {
			TubeMagazineScript mag = target as TubeMagazineScript;

			serializedObject.Update();

			DrawDefaultInspector();

			EditorGUILayout.Space(20);

			if (Application.isPlaying) {
				if (serializedObject.ApplyModifiedProperties()) {
					mag.Awake();
				}

				if (mag.is_valid) {
					float width = Mathf.Clamp(EditorGUIUtility.currentViewWidth - 100, 100, 400);

					GUILayout.Label("Round Count: " + mag.round_count);
					mag.round_count = (int) GUILayout.HorizontalSlider(mag.round_count, 0, mag.capacity, GUILayout.Width(width));

					EditorGUILayout.Space(40);

					if (GUILayout.Button("Add Round", GUILayout.Width(width))) {
						mag.InsertRound(Instantiate(serializedObject.FindProperty("round_prefab").objectReferenceValue as GameObject).GetComponent<ShellCasingScript>());
					}

					EditorGUILayout.Space(40);

					if (GUILayout.Button("Start Remove Round", GUILayout.Width(width))) {
						mag.StartRemoveRound();
					}
				
					EditorGUILayout.Space(40);
				
					if (GUILayout.Button("End Remove Round", GUILayout.Width(width))) {
						var shell = mag.RetrieveRound();

						if (shell != null) Destroy(shell.gameObject);
					}
				}
				else {
					GUILayout.Label("Not all required fields are filled");
				}
			}
			else {
				GUILayout.Label("Enter play mode to test round positions");
			}
		}
	}
}

#endif