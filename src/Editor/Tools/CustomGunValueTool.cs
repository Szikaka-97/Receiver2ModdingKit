#if UNITY_EDITOR

using System;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using Receiver2;

[EditorTool("Custom Enum Value", typeof(GunScript))]
public class CustomGunValueTool : EditorTool {

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
		EditorGUI.BeginChangeCheck();

		GunScript gun = (GunScript) target;

		var serializedObject = new SerializedObject(target);

		var background_style = new GUIStyle();
		background_style.normal = new GUIStyleState() { 
			background = Texture2D.whiteTexture
		};
		background_style.fontStyle = FontStyle.Bold;
		background_style.fontSize = 15;

		using (new Handles.DrawingScope()) {
			StringBuilder gun_info_string = new StringBuilder();

			if (!Int32.TryParse(gun.gun_model.ToString(), out int parsed_gun_model) || parsed_gun_model != (int) gun.gun_model) {
				gun_info_string.AppendLine("gun_model: " + (int) gun.gun_model + "\t(" + gun.gun_model.ToString() + ")");
			}
			else {
				gun_info_string.AppendLine("gun_model: " + (int) gun.gun_model);
			}

			if (!Int32.TryParse(gun.gun_type.ToString(), out int parsed_gun_type) || parsed_gun_type != (int) gun.gun_type) {
				gun_info_string.AppendLine("gun_type:    " + (int) gun.gun_type + "\t(" + gun.gun_type.ToString() + ")");
			}
			else {
				gun_info_string.AppendLine("gun_type:    " + (int) gun.gun_type);
			}

			if (gun.cartridge_dimensions.Length > 0) {
				gun_info_string.AppendLine("cartridge_dimensions [");

				foreach (int type in gun.cartridge_dimensions) {
					if (type < serializedObject.FindProperty("cartridge_dimensions").enumDisplayNames.Length){
						gun_info_string.AppendLine("\t      " + type.ToString() + "\t(" + serializedObject.FindProperty("cartridge_dimensions").enumDisplayNames[type] + ")");
					} 
					else {
						gun_info_string.AppendLine("\t      " + type.ToString());
					}
				}

				gun_info_string.Append("]");
			}

			Handles.Label(Tools.handlePosition, gun_info_string.ToString());
		}

		Handles.BeginGUI();

		GUILayout.BeginArea(new Rect(10, 10, 150, 42), background_style);
		GUILayout.Label("Gun Model", background_style);

		var gun_model = serializedObject.FindProperty("gun_model"); 
		int new_gun_model = EditorGUILayout.DelayedIntField(gun_model.intValue);

		if (new_gun_model >= 0) gun_model.intValue = new_gun_model;

		GUILayout.EndArea();

		GUILayout.BeginArea(new Rect(10, 62, 150, 42), background_style);
		GUILayout.Label("Gun Type", background_style);

		var gun_type = serializedObject.FindProperty("gun_type");
		int new_gun_type = EditorGUILayout.DelayedIntField(gun_type.intValue);
		
		if (new_gun_type >= 0) gun_type.intValue = new_gun_type;

		GUILayout.EndArea();

		var cartridge_dimensions = serializedObject.FindProperty("cartridge_dimensions");

		GUILayout.BeginArea(new Rect(10, 114, 150, 42 + 20 * cartridge_dimensions.arraySize), background_style);
		GUILayout.Label("Cartridge Types", background_style);

		using (new GUILayout.HorizontalScope()) {
			if (GUILayout.Button("+")) cartridge_dimensions.arraySize += 1;
			if (GUILayout.Button("-") && cartridge_dimensions.arraySize >= 0) cartridge_dimensions.arraySize -= 1;
		}

		foreach (SerializedProperty dimension in cartridge_dimensions) {
			int new_dimension = EditorGUILayout.DelayedIntField(dimension.intValue); 
			if (new_dimension >= 0) dimension.intValue = new_dimension;
		}

		GUILayout.EndArea();

		Handles.EndGUI();

		serializedObject.ApplyModifiedProperties();
	}
}

#endif