using UnityEngine;
using UnityEditor;
using Receiver2ModdingKit.Editor;

[CustomEditor(typeof(CustomSoundsList))]
public class CustomSoundsListEditor : Editor {
    private readonly bool[] toggles = new bool[31];

	public override void OnInspectorGUI() {
		EditorGUILayout.PropertyField(serializedObject.FindProperty("prefix"), new GUIContent("Sounds prefix"));
		EditorGUILayout.PropertyField(serializedObject.FindProperty("sound_path"), new GUIContent("Sounds path"));

		EditorGUILayout.Separator();

		foreach (var pair in CustomSoundsList.sound_event_lookup) {
			if (toggles[pair.Value] = EditorGUILayout.BeginFoldoutHeaderGroup(toggles[pair.Value], pair.Key)) {
				var sound_event = serializedObject.FindProperty("sound_events").GetArrayElementAtIndex(pair.Value);
				EditorGUILayout.PropertyField(sound_event, new GUIContent("Default:", sound_event.stringValue));

				var fallback_event = serializedObject.FindProperty("fallback_events").GetArrayElementAtIndex(pair.Value);
				EditorGUILayout.PropertyField(fallback_event, new GUIContent("Fallback:", fallback_event.stringValue));
			}
			
			EditorGUILayout.EndFoldoutHeaderGroup();
		}

		serializedObject.ApplyModifiedProperties();
	}
}
