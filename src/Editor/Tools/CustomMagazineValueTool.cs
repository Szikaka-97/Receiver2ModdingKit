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

	float hSbarValue = -1;
	int gun_model = -1;

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

		Handles.BeginGUI();

		GUILayout.BeginArea(new Rect(10, 10, 150, 92), background_style);
		GUILayout.Label("Magazine class", background_style);
		GUILayout.Space(3);

		if (hSbarValue == -1) {
			if ((int) magazine.model < 50) hSbarValue = 0;
			else if ((int) magazine.model < 100) hSbarValue = 10;
			else if ((int) magazine.model < 150) hSbarValue = 20;
			else hSbarValue = 30;
		}

		hSbarValue = GUILayout.HorizontalScrollbar(hSbarValue, 1, 0, 30, GUILayout.Width(150));

		hSbarValue = Mathf.Round(hSbarValue / 10) * 10;
		int tempValue = (int) hSbarValue / 10;

		float newValue = (float) MagazineClasses.LowCapacity;
		if (49 + (50 * tempValue) >= (float) MagazineClasses.LowCapacityGold) newValue = (float) MagazineClasses.LowCapacityGold;
		if (49 + (50 * tempValue) >= (float) MagazineClasses.StandardCapacity) newValue = (float) MagazineClasses.StandardCapacity;
		if (49 + (50 * tempValue) >= (float) MagazineClasses.StandardCapacityGold) newValue = (float) MagazineClasses.StandardCapacityGold;

		// hSbarValue = newValue;

		GUILayout.Space(0);
        GUILayout.Label("Magazine Type: \n " + (MagazineClasses) newValue);

		if (GUILayout.Button("Apply", GUILayout.Width(100))) {
			magazine.model = (MagazineModel) newValue;
		}

		GUILayout.EndArea();

		GUILayout.BeginArea(new Rect(10, 112, 150, 62), background_style);
		GUILayout.Label("Gun Model", background_style);

		int gun_model_prev = gun_model;
		string gun_model_temp = GUILayout.TextField(gun_model.ToString());
		if (!Int32.TryParse(gun_model_temp, out gun_model) || gun_model < 0) gun_model = gun_model_prev;
		if (gun_model == -1) gun_model = (int) magazine.gun_model;

		if (GUILayout.Button("Apply")) {
			magazine.gun_model = (GunModel) gun_model;
		};
		GUILayout.EndArea();

		using (new Handles.DrawingScope()) {
			Handles.Label(Tools.handlePosition, 
				"Magazine Class: " + magazine.MagazineClass + "\n" +
				"gun_model:    " + (int) magazine.gun_model + " \t (" + magazine.gun_model.ToString() + ")"
			);
		}

		Handles.EndGUI();
	}
}