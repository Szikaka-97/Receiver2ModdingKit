using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using Receiver2;

[EditorTool("Custom Enum Value", typeof(GunScript))]
public class CustomGunValueTool : EditorTool {

	[SerializeField]
	Texture2D icon;

	GUIContent guiContent;

	int gun_model = -1;
	int gun_type = -1;

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
		EditorGUI.BeginChangeCheck();

		GunScript gun = (GunScript) target;

		var background_style = new GUIStyle();
		background_style.normal = new GUIStyleState() { 
			background = Texture2D.whiteTexture,
		};
		background_style.fontStyle = FontStyle.Bold;
		background_style.fontSize = 15;

		using (new Handles.DrawingScope()) {
			Handles.Label(Tools.handlePosition, 
				"gun_model: " + (int) gun.gun_model + "\t (" + gun.gun_model.ToString() + ")\n" +
				"gun_type:    " + (int) gun.gun_type + " \t (" + gun.gun_type.ToString() + ")"
			);
		}

		Handles.BeginGUI();

		GUILayout.BeginArea(new Rect(10, 10, 150, 62), background_style);
		GUILayout.Label("Gun Model", background_style);

		int gun_model_prev = gun_model;
		string gun_model_temp = GUILayout.TextField(gun_model.ToString());
		if (!Int32.TryParse(gun_model_temp, out gun_model) || gun_model < 0) gun_model = gun_model_prev;
		if (gun_model == -1) gun_model = (int) gun.gun_model;

		if (GUILayout.Button("Apply")) {
			gun.gun_model = (GunModel) gun_model;
		};
		GUILayout.EndArea();

		GUILayout.BeginArea(new Rect(10, 82, 150, 62), background_style);
		GUILayout.Label("Gun Type", background_style);

		int gun_type_prev = gun_type;
		string gun_type_temp = GUILayout.TextField(gun_type.ToString());
		if (!Int32.TryParse(gun_type_temp, out gun_type) || gun_type < 0) gun_type = gun_type_prev;
		if (gun_type == -1) gun_type = (int) gun.gun_type;

		if (GUILayout.Button("Apply")) {
			gun.gun_type = (GunType) gun_type;
		};
		GUILayout.EndArea();

		Handles.EndGUI();

		if (EditorGUI.EndChangeCheck()) {

		}
	}
}
