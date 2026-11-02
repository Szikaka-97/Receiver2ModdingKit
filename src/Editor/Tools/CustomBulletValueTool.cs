#if UNITY_EDITOR

using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using Receiver2;
using Receiver2ModdingKit;

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

		var mscs = round as ModShellCasingScript;

		using (new Handles.DrawingScope()) {
			Handles.Label(Tools.handlePosition, 
				"cartridge_type: " + (int) round.cartridge_type + "\t (" + round.cartridge_type.ToString() + ")"
			);

			if (mscs != null)
			{
				if (mscs.customRoundTypeParameters != null)
				{
					Handles.Label(Tools.handlePosition,
						"\nBase Cartridge: " + (int)mscs.customRoundTypeParameters.baseVariant + "\t (" + mscs.customRoundTypeParameters.baseVariant.ToString() + ")"
					);

					Handles.Label(Tools.handlePosition,
						"\n\nVariant Cartridge Type: " + (int)mscs.customRoundTypeParameters.cartridge + "\t (" + mscs.customRoundTypeParameters.cartridge.ToString() + ")"
					);
				}
			}
		}

		Handles.BeginGUI();

		GUILayout.BeginArea(new Rect(10, 10, 150, 42), background_style);
		GUILayout.Label("Cartridge Type", background_style);

		var cartridge_type = serializedObject.FindProperty("cartridge_type");
		int new_cartridge_type = EditorGUILayout.DelayedIntField(cartridge_type.intValue);

		if (new_cartridge_type >= 0) cartridge_type.intValue = new_cartridge_type;

		GUILayout.EndArea();

		if (mscs != null)
		{
			var custom_round_def_property = serializedObject.FindProperty("customRoundTypeParameters");
			
			{
				GUILayout.BeginArea(new Rect(10, 62, 150, 42), background_style);
				GUILayout.Label("Base Cartridge", background_style);

				var custom_round_base_type = custom_round_def_property.FindPropertyRelative("baseVariant");

				int new_round_base_type = EditorGUILayout.DelayedIntField(custom_round_base_type.intValue);

				if (new_round_base_type >= 0) custom_round_base_type.intValue = new_round_base_type;

				var round_base_type = custom_round_base_type.intValue;

				//this might be a problem if for some reason someone makes a custom variant of 9mm with a whole ass round and shit but, who's going to do it? LOL
				var custom_round_cartridge_type = custom_round_def_property.FindPropertyRelative("cartridge");
				int new_custom_round_cartridge_type = (int)round.cartridge_type;

				if (new_custom_round_cartridge_type >= 0) custom_round_cartridge_type.intValue = round_base_type == 0 ? round_base_type : new_custom_round_cartridge_type;

				GUILayout.EndArea();
			}

			// {
			// 	GUILayout.BeginArea(new Rect(10, 114, 150, 42), background_style);
			// 	GUILayout.Label("Variant Type", background_style);

			// 	var custom_round_cartridge_type = custom_round_def_property.FindPropertyRelative("cartridge");
			// 	int new_custom_round_cartridge_type = EditorGUILayout.DelayedIntField(custom_round_cartridge_type.intValue);

			// 	if (new_custom_round_cartridge_type >= 0) custom_round_cartridge_type.intValue = new_custom_round_cartridge_type;

			// 	GUILayout.EndArea();
			// }
		}

		Handles.EndGUI();

		serializedObject.ApplyModifiedProperties();
	}
}

#endif