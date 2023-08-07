#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using Receiver2;

namespace Receiver2ModdingKit.Editor.Tools {
	[EditorTool("Check Gun Transforms", typeof(GunScript))]
	public class CheckGunTransforms : EditorTool {
		[SerializeField]
		Texture2D icon;

		private bool basics_menu_open = false;
		private bool slide_menu_open = false;
		private bool load_menu_open = false;

		private string[] basics = new string[] {
			"center_of_mass",
			"point_recoil_rotate",
			"point_bullet_fire",
			"point_muzzleflash"
		};

		private string[] slide_positions = new string[] {
			"point_slide_end",
			"point_slide_unstopped",
			"point_slide_stopped"
		};

		private string[] load_positions = new string[] {
			"load_progression/1",
			"load_progression/2",
			"load_progression/3",
			"barrel/point_round_entering",
			"barrel/point_chambered_round"
		};

		GUIContent guiContent;
		void OnEnable() {
			guiContent = new GUIContent()
			{
				image = icon,
				text = "Check Gun Transforms",
				tooltip = "Use this tool to check if gun transforms are aligned properly"
			};
		}

		void CheckBasics() {
			GunScript gun = (GunScript) target;

			var correct_style = new GUIStyle();
			correct_style.normal = new GUIStyleState() {
				background = Texture2D.whiteTexture,
				textColor = new Color(0, 0.75f, 0)
			};
			correct_style.fontSize = 12;

			var missing_style = new GUIStyle(correct_style);
			missing_style.normal.textColor = Color.red;

			foreach (string pos in basics) {
				if (gun.transform.Find(pos) != null) {
					GUILayout.Label("✓ " + pos, correct_style);
				}
				else {
					GUILayout.Label("✕ " + pos, missing_style);
				}
			}
		}

		void CheckSlide() {
			GunScript gun = (GunScript) target;

			var correct_style = new GUIStyle();
			correct_style.normal = new GUIStyleState() {
				background = Texture2D.whiteTexture,
				textColor = new Color(0, 0.75f, 0)
			};
			correct_style.fontSize = 12;

			var wrong_style = new GUIStyle(correct_style);
			wrong_style.normal.textColor = new Color(1, 0.7f, 0.3f);

			var missing_style = new GUIStyle(correct_style);
			missing_style.normal.textColor = Color.red;

			float prev_dot = -Mathf.Infinity;

			foreach (string pos in slide_positions) {
				if (gun.transform.Find(pos) != null) {
					float dot = Vector3.Dot(gun.transform.forward, gun.transform.Find(pos).position);

					if (dot >= prev_dot) {
						GUILayout.Label("✓ " + pos, correct_style);
					}
					else {
						GUILayout.Label("! " + pos, wrong_style);
					}

					prev_dot = dot;
				}
				else {
					GUILayout.Label("✕ " + pos, missing_style);
				}
			}

			var furthest_dot = prev_dot;

			var point_slide_doublefeed = gun.transform.Find("point_slide_doublefeed");
			if (point_slide_doublefeed != null) {
				float dot = Vector3.Dot(gun.transform.forward, point_slide_doublefeed.position);

				if (dot >= prev_dot) {
					GUILayout.Label("✓ " + "point_slide_doublefeed", correct_style);
				}
				else {
					GUILayout.Label("! " + "point_slide_doublefeed", wrong_style);
				}

				if (dot > furthest_dot) furthest_dot = dot;
			}
			else {
				GUILayout.Label("✕ " + "point_slide_doublefeed", missing_style);
			}

			var point_slide_stovepipe = gun.transform.Find("point_slide_stovepipe");
			if (point_slide_stovepipe != null) {
				float dot = Vector3.Dot(gun.transform.forward, point_slide_stovepipe.position);

				if (dot >= prev_dot) {
					GUILayout.Label("✓ " + "point_slide_stovepipe", correct_style);
				}
				else {
					GUILayout.Label("! " + "point_slide_stovepipe", wrong_style);
				}

				if (dot > furthest_dot) furthest_dot = dot;
			}
			else {
				GUILayout.Label("✕ " + "point_slide_stovepipe", missing_style);
			}

			var point_slide_start = gun.transform.Find("point_slide_start");
			if (point_slide_start != null) {
				float dot = Vector3.Dot(gun.transform.forward, point_slide_start.position);

				if (dot >= furthest_dot) {
					GUILayout.Label("✓ " + "point_slide_start", correct_style);
				}
				else {
					GUILayout.Label("! " + "point_slide_start", wrong_style);
				}
			}
			else {
				GUILayout.Label("✕ " + "point_slide_start", missing_style);
			}
		}

		void CheckLoadPositions() {
			GunScript gun = (GunScript) target;

			var correct_style = new GUIStyle();
			correct_style.normal = new GUIStyleState() {
				background = Texture2D.whiteTexture,
				textColor = new Color(0, 0.75f, 0)
			};
			correct_style.fontSize = 12;

			var wrong_style = new GUIStyle(correct_style);
			wrong_style.normal.textColor = new Color(1, 0.7f, 0.3f);

			var missing_style = new GUIStyle(correct_style);
			missing_style.normal.textColor = Color.red;

			float prev_dot = -Mathf.Infinity;

			foreach (string pos in load_positions) {
				if (gun.transform.Find(pos) != null) {
					float dot = Vector3.Dot(gun.transform.forward, gun.transform.Find(pos).position);

					if (dot >= prev_dot) {
						GUILayout.Label("✓ " + pos, correct_style);
					}
					else {
						GUILayout.Label("! " + pos, wrong_style);
					}

					prev_dot = dot;
				}
				else {
					GUILayout.Label("✕ " + pos, missing_style);
				}
			}
		}

		public override void OnToolGUI(EditorWindow window) {
			GunScript gun = (GunScript) target;

			var background_style = new GUIStyle();
			background_style.normal = new GUIStyleState() { 
				background = Texture2D.whiteTexture,
			};
			background_style.fontStyle = FontStyle.Bold;
			background_style.fontSize = 15;

			Handles.BeginGUI();

			float height = 85;
			if (basics_menu_open) height += 60;
			if (slide_menu_open) height += 90;
			if (load_menu_open) height += 80;

			GUILayout.BeginArea(new Rect(10, 10, 170, height), background_style);
			GUILayout.Label("Gun Checker", background_style);
        
			if (basics_menu_open) {
					CheckBasics();

					basics_menu_open = !GUILayout.Button("Close");
				}
				else basics_menu_open = GUILayout.Button("Basics");

			if (gun.transform.Find("slide") != null) {
				if (slide_menu_open) {
					CheckSlide();

					slide_menu_open = !GUILayout.Button("Close");
				}
				else slide_menu_open = GUILayout.Button("Slide");

				if (gun.transform.Find("barrel") != null) {
					if (load_menu_open) {
						CheckLoadPositions();

						load_menu_open = !GUILayout.Button("Close");
					}
					else load_menu_open = GUILayout.Button("Load Progression");
				}
				else {
					GUILayout.Label("Your gun doesn't have a barrel object");
				}
			}
			else {
				GUILayout.Label("Your gun doesn't have a slide object");
			}

			GUILayout.EndArea();

			Handles.EndGUI();
		}
	}
}

#endif