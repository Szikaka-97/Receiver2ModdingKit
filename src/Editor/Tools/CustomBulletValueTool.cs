using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using Receiver2;

[EditorTool("Custom Enum Value", typeof(ShellCasingScript))]
public class CustomBulletValueTool : EditorTool {

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
		ShellCasingScript round = (ShellCasingScript) target;
		var serializedObject = new SerializedObject(target);

		var background_style = new GUIStyle();
		background_style.normal = new GUIStyleState() { 
			background = Texture2D.whiteTexture,
		};
		background_style.fontStyle = FontStyle.Bold;
		background_style.fontSize = 15;

		using (new Handles.DrawingScope()) {
			Handles.Label(Tools.handlePosition, 
				"cartridge_type: " + (int) round.cartridge_type + "\t (" + round.cartridge_type.ToString() + ")"
			);
		}

		Handles.BeginGUI();

		GUILayout.BeginArea(new Rect(10, 10, 150, 42), background_style);
		GUILayout.Label("Cartridge Type", background_style);

		var cartridge_type = serializedObject.FindProperty("cartridge_type");
		int prev_cartridge_type = cartridge_type.intValue;
		int new_cartridge_type = EditorGUILayout.DelayedIntField(cartridge_type.intValue);

		if (new_cartridge_type >= 0) cartridge_type.intValue = new_cartridge_type;

		GUILayout.EndArea();

		Handles.EndGUI();

		serializedObject.ApplyModifiedProperties();
	}
}
