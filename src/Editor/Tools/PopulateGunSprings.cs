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
	}

	public override GUIContent toolbarIcon {
		get { return guiContent; }
	}

	public override void OnToolGUI(EditorWindow window) {
		GunScript gun = (GunScript) target;

		var springs = gun.GetComponentsInChildren<SpringCompressInstance>();

		gun.update_springs = (
			from springInstance 
			in springs 
			select new UpdatedSpring {
				update_direction = false,
				spring = springInstance
			}
		).ToArray(); //SQL BITCH
	}
}
