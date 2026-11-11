#if UNITY_EDITOR
using Receiver2;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

[EditorTool("Custom Enum Value", typeof(TileScript))]
public class CustomTileValueTool : EditorTool {
	[SerializeField] Texture2D icon;

	GUIContent guiContent;

	void OnEnable() {
		guiContent = new GUIContent() {
			image = icon,
			text = "Custom Enum Value",
			tooltip = "Use this tool to set custom enum values"
		};
	}

	public override GUIContent toolbarIcon => guiContent;

	public override void OnToolGUI(EditorWindow window) {
		var tile = target as TileScript;
		var serializedObject = new SerializedObject(target);

		var background_style = new GUIStyle {
			normal = new GUIStyleState() {
				background = Texture2D.whiteTexture,
			},
			fontStyle = FontStyle.Bold,
			fontSize = 15
		};

		using (new Handles.DrawingScope()) {
			Handles.Label(Tools.handlePosition,
				"tile_type: " + tile.tile_type.ToString()
			);
		}
		
		Handles.BeginGUI();

		GUILayout.BeginArea(new Rect(10, 10, 150, 42), background_style);
		GUILayout.Label("Tile Type", background_style);

		var tile_type = serializedObject.FindProperty("tile_type");
		int new_tile_type = EditorGUILayout.DelayedIntField(tile_type.intValue);

		if (new_tile_type >= 0) tile_type.intValue = new_tile_type;

		GUILayout.EndArea();

		Handles.EndGUI();

		serializedObject.ApplyModifiedProperties();
	}
}
#endif