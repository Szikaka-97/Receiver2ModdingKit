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

	int cartridgeType = -1;

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

		ShellCasingScript round = (ShellCasingScript) target;

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

		GUILayout.BeginArea(new Rect(10, 10, 150, 62), background_style);
		GUILayout.Label("Cartridge Type", background_style);

		int cartridgeType_prev = cartridgeType;
		string cartridgeType_temp = GUILayout.TextField(cartridgeType.ToString());
		if (!Int32.TryParse(cartridgeType_temp, out cartridgeType) || cartridgeType < 0) cartridgeType = cartridgeType_prev;
		if (cartridgeType == -1) cartridgeType = (int) round.cartridge_type;

		if (GUILayout.Button("Apply")) {
			round.cartridge_type = (CartridgeSpec.Preset) cartridgeType;
		};
		GUILayout.EndArea();


		Handles.EndGUI();

		if (EditorGUI.EndChangeCheck()) {

		}
	}
}
