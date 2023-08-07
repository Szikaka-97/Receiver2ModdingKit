#if UNITY_EDITOR

using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using Receiver2;

[EditorTool("Populate Gun Update Springs list", typeof(GunScript))]
public class PopulateGunSprings : EditorTool {
    [SerializeField]
	Texture2D icon;

	GUIContent guiContent;

	void OnEnable() {
		guiContent = new GUIContent() {
			image = icon,
			text = "Populate Gun Update Springs",
			tooltip = "Use this tool to populate gun's Update Springs list"
		};

		GunScript gun = (GunScript) target;
		SerializedObject serializedObject = new SerializedObject(target);

		serializedObject.FindProperty("update_springs").ClearArray();
		
		foreach(var spring in gun.GetComponentsInChildren<SpringCompressInstance>()) {
			serializedObject.FindProperty("update_springs").InsertArrayElementAtIndex(0);

			var serialized_spring = serializedObject.FindProperty("update_springs").GetArrayElementAtIndex(0);

			serialized_spring.FindPropertyRelative("update_direction").boolValue = false;
			serialized_spring.FindPropertyRelative("spring").objectReferenceValue = spring;
		}
	}

	public override GUIContent toolbarIcon {
		get { return guiContent; }
	}
}

#endif