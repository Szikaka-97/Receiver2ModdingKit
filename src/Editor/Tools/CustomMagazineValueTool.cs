using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using Receiver2;

[EditorTool("Custom Enum Value", typeof(MagazineScript))]
public class CustomMagazineValueTool : EditorTool {
	private enum MagazineClasses {
		LowCapacity = 49,
		LowCapacityGold = 99,
		StandardCapacity = 149,
		StandardCapacityGold = 199
	}

	[SerializeField]
	Texture2D icon;

	GUIContent guiContent;

	void OnEnable() {
		guiContent = new GUIContent()
		{
			image = icon,
			text = "Custom Enum Value",
			tooltip = "Use this tool to set custom enum values"
		};
	}

	public override GUIContent toolbarIcon {
		get { return guiContent; }
	}

	public override void OnToolGUI(EditorWindow window) {
		MagazineScript magazine = (MagazineScript) target;

		var background_style = new GUIStyle();
		background_style.normal = new GUIStyleState() { 
			background = Texture2D.whiteTexture,
		};
		background_style.fontStyle = FontStyle.Bold;
		background_style.fontSize = 15;

		var serializedObject = new SerializedObject(target);

		Handles.BeginGUI();

		GUILayout.BeginArea(new Rect(10, 10, 150, 74), background_style);
		GUILayout.Label("Magazine class", background_style);
		GUILayout.Space(3);

		var mag_class = serializedObject.FindProperty("model");

		int bar_value = (int) GUILayout.HorizontalScrollbar(Mathf.RoundToInt(mag_class.intValue / 50), 1, 0, 6);

		mag_class.intValue = 49 + (bar_value * 50);

		GUILayout.Space(3);

		GUILayout.Label("Type:\n   " + magazine.MagazineClass.ToString(), new GUIStyle(background_style) { fontStyle = FontStyle.Normal, fontSize = 12 });

		GUILayout.EndArea();

		GUILayout.BeginArea(new Rect(10, 94, 150, 42), background_style);
		GUILayout.Label("Gun Model", background_style);

		var gun_model = serializedObject.FindProperty("gun_model");
		int new_gun_model = EditorGUILayout.DelayedIntField(gun_model.intValue);

		if (new_gun_model >= 0) gun_model.intValue = new_gun_model;

		GUILayout.EndArea();

		using (new Handles.DrawingScope()) {
			Handles.Label(Tools.handlePosition, 
				"Magazine class: \t" + magazine.MagazineClass + "\n" +
				"gun_model:    " + (int) magazine.gun_model + " \t(" + magazine.gun_model.ToString() + ")"
			);
		}

		serializedObject.ApplyModifiedProperties();

		Handles.EndGUI();
	}
}