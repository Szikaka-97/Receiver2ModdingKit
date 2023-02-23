using UnityEngine;
using UnityEditor;
using Receiver2ModdingKit.Editor;
using System.Collections.Generic;
using ImGuiNET;
using System.Linq;

[CustomEditor(typeof(CustomSoundsList))]
public class CustomSoundsListEditor : Editor {
    private readonly bool[] toggles = new bool[31];

	private Stack<bool> custom_toggles = new Stack<bool>();

	public void OnEnable() {
		custom_toggles = new Stack<bool>();

		for (int i = 0; i < serializedObject.FindProperty("custom_event_names").arraySize; i++) custom_toggles.Push(false);
	}

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

		EditorGUILayout.Separator();

		GUILayout.Label("Custom events");

		using (new EditorGUILayout.HorizontalScope()) {
			if (GUILayout.Button("-") && serializedObject.FindProperty("custom_event_names").arraySize > 0) {
				serializedObject.FindProperty("custom_event_names").arraySize--;
				serializedObject.FindProperty("custom_event_values").arraySize--;
				serializedObject.FindProperty("custom_event_fallback_values").arraySize--;
				custom_toggles.Pop();
			}
			if (GUILayout.Button("+")) {
				serializedObject.FindProperty("custom_event_names").arraySize++;
				serializedObject.FindProperty("custom_event_values").arraySize++;
				serializedObject.FindProperty("custom_event_fallback_values").arraySize++;
				custom_toggles.Push(true);
			}

			serializedObject.ApplyModifiedProperties();
		} 

		var new_toggles = custom_toggles.ToArray();

		for (int i = 0; i < serializedObject.FindProperty("custom_event_names").arraySize; i++) {
			var name = serializedObject.FindProperty("custom_event_names").GetArrayElementAtIndex(i);

			if (new_toggles[i] = EditorGUILayout.BeginFoldoutHeaderGroup(new_toggles[i], name.stringValue)) {
				EditorGUILayout.PropertyField(name, new GUIContent("Name:"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("custom_event_values").GetArrayElementAtIndex(i), new GUIContent("Default:"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("custom_event_fallback_values").GetArrayElementAtIndex(i), new GUIContent("Fallback:"));
			}

			EditorGUILayout.EndFoldoutHeaderGroup();
		}

		custom_toggles = new Stack<bool>(new_toggles);

		serializedObject.ApplyModifiedProperties();
	}
}
