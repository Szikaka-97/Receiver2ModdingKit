#if UNITY_EDITOR

using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using Receiver2;

namespace Receiver2ModdingKit.Editor.Tools {
	[EditorTool("Magazine Round Positions", typeof(MagazineScript))]
	public class CheckMagazineRoundPositionsTool : EditorTool {
		[SerializeField]
		Texture2D icon;

		GUIContent guiContent;

		float follower_pos = 0;

		float round_count = -1;
		bool check_chambering = false;

		void OnEnable() {
			guiContent = new GUIContent()
			{
				image = icon,
				text = "Magazine Round Positions",
				tooltip = "Check round positions in a modded magazine"
			};
		}

		public override GUIContent toolbarIcon {
			get { return guiContent; }
		}

		private void DebugFollower() {
			var background_style = new GUIStyle();
			background_style.normal = new GUIStyleState() { 
				background = Texture2D.whiteTexture,
			};
			background_style.fontStyle = FontStyle.Bold;
			background_style.fontSize = 15;

			GUILayout.BeginArea(new Rect(10, 50, 150, 70), background_style);
			GUILayout.Label("Follower Position", background_style);

			Transform follower_transform = (target as MagazineScript).transform.Find("follower");

			var follower_offset = (target as MagazineScript).transform.Find("round_top").position - (target as MagazineScript).transform.Find("follower_under_round_top").position;

			var start_pos = (target as MagazineScript).transform.Find("round_top").position - follower_offset;
			var end_pos = (target as MagazineScript).transform.Find("round_bottom").position - follower_offset;

			follower_pos = GUILayout.HorizontalSlider(follower_pos, 0, 1);

			GUILayout.Space(20);

			var spring = (target as MagazineScript).transform.Find("magazine/magazine_spring").GetComponent<SpringCompressInstance>();

			if (spring.orig_dist == 0) {
				spring.orig_center = (spring.transform.InverseTransformPoint(spring.new_top.position) + spring.transform.InverseTransformPoint(spring.new_bottom.position)) / 2;
				spring.orig_dist = Vector3.Distance(spring.new_top.position, spring.new_bottom.position);
			}

			follower_transform.position = Vector3.Lerp(start_pos, end_pos, follower_pos);

			spring.UpdateDirection();
			spring.UpdateScale();

			GUILayout.EndArea();
		}

		private void DebugMagazine() {
			EditorGUI.BeginChangeCheck();

			MagazineScript magazine = (MagazineScript) target;

			var background_style = new GUIStyle();
			background_style.normal = new GUIStyleState() { 
				background = Texture2D.whiteTexture,
			};
			background_style.fontStyle = FontStyle.Bold;
			background_style.fontSize = 15;

			Handles.BeginGUI();

			GUILayout.BeginArea(new Rect(10, 10, 150, 202), background_style);
			GUILayout.Label("Magazine", background_style);

			if (round_count == -1) {
				round_count = magazine.rounds_in_mag;
				UpdateRounds();
			}

			round_count *= 10;

			round_count = GUILayout.HorizontalScrollbar(round_count, 1, 0, magazine.kMaxRounds *  10);

			round_count = Mathf.Round(round_count / 10);

			GUILayout.Label("Round Count: " + round_count);

			if (GUILayout.Button("Awake")) {
				magazine.Awake();
			}

			if (GUILayout.Button("Update")) {
				UpdateRounds();
			}

			magazine.round_position_params.param_a = GUILayout.HorizontalSlider(magazine.round_position_params.param_a, 0, 1, GUILayout.Height(20));
			magazine.round_position_params.param_b = GUILayout.HorizontalSlider(magazine.round_position_params.param_b, 0, 1, GUILayout.Height(20));
			magazine.round_position_params.param_c = GUILayout.HorizontalSlider(magazine.round_position_params.param_c, 0, 1, GUILayout.Height(20));
			magazine.round_position_params.param_d = GUILayout.HorizontalSlider(magazine.round_position_params.param_d, 0, 1, GUILayout.Height(20));
			magazine.round_position_params.param_e = GUILayout.HorizontalSlider(magazine.round_position_params.param_e, 0, 1, GUILayout.Height(20));

			GUILayout.EndArea();

			if (magazine.transform.parent && magazine.transform.parent.parent && magazine.transform.parent.parent.GetComponent<GunScript>()) {

				GUILayout.BeginArea(new Rect(10, 122, 150, 20), background_style);
				check_chambering = GUILayout.Toggle(check_chambering, "Check Chambering path");

				GUILayout.EndArea();

				if (check_chambering) {
					GunScript gun = magazine.transform.parent.parent.GetComponent<GunScript>();

					GUILayout.BeginArea(new Rect(10, 142, 150, 100), background_style);

					if (gun.slide.transform) {
							magazine.slot = gun.GetComponent<InventorySlot>();

							gun.slide.amount = GUILayout.HorizontalSlider(gun.slide.amount, 0, 1, GUILayout.Width(150));

							gun.slide.UpdateDisplay();

							if (gun.round_in_chamber) {
								Transform slide_round = gun.transform.Find("slide/point_chambered_round");
								Transform barrel_round_entering = gun.transform.Find("barrel/point_round_entering");
								Transform barrel_round_chambered = gun.transform.Find("barrel/point_chambered_round");
								float num5 = Vector3.Dot(slide_round.position, gun.transform.forward);
								float num6 = Vector3.Dot(barrel_round_entering.position, gun.transform.forward);
								float num7 = Vector3.Dot(barrel_round_chambered.position, gun.transform.forward);

								gun.round_in_chamber.transform.SetLerpNoScale(barrel_round_entering, barrel_round_chambered, (num5 - num6) / (num7 - num6));
							}
					}
					else {
						if (!gun.transform.Find("slide")) {
							Debug.LogError("No object named \"Slide\" in a gun");
						}
						else gun.slide.transform = gun.transform.Find("slide");

						if (!gun.transform.Find("point_slide_start")) {
							Debug.LogError("No object named \"point_slide_start\" in a gun");
						}
						else gun.slide.positions[0] = gun.transform.Find("point_slide_start").localPosition;

						if (!gun.transform.Find("slide")) {
							Debug.LogError("No object named \"point_slide_end\" in a gun");
						}
						else gun.slide.positions[1] = gun.transform.Find("point_slide_end").localPosition;				
					}

					GUILayout.EndArea();
				}
				else magazine.slot = null;
			}

			Handles.EndGUI();

			if (EditorGUI.EndChangeCheck()) {
				UpdateRounds();
			}
		}

		public override void OnToolGUI(EditorWindow window) {
			if ((target as MagazineScript).round_prefab == null) {
				GUILayout.BeginArea(new Rect(10, 10, 180, 60));

				GUILayout.Label("Fill the round_prefab field to \nhave access to more options", new GUIStyle(GUI.skin.label) { normal = new GUIStyleState() { textColor = Color.red } });

				GUILayout.EndArea();

				DebugFollower();
			}
			else {
				DebugMagazine();
			}
		}

		private void UpdateRounds() {
			var magazine = (MagazineScript) target;

			foreach(var round in magazine.rounds) if (round && round.gameObject) DestroyImmediate(round.gameObject);
			magazine.rounds.Clear();

			if (!magazine.round_top) {
				magazine.Awake();
			}

			try {
				if (round_count == 0) {
					magazine.rounds_in_mag = 0;

					magazine.UpdateRoundPositions();
				}
				else {
					for (int i = 0; i < round_count; i++) {
						magazine.AddRound();
					}

					magazine.UpdateRoundPositions();
				}
			} catch (NullReferenceException e) {
				Debug.LogError("Error Happened:" + e.StackTrace);

				if (!magazine.round_top) Debug.LogError("Missing round_top transform");
				if (!magazine.round_bottom) Debug.LogError("Missing round_bottom transform");
				if (!magazine.round_insert) Debug.LogError("Missing round_insert transform");
				if (!magazine.follower) Debug.LogError("Missing follower transform");
				if (!magazine.transform.Find("follower_under_round_top")) Debug.LogError("Missing follower_under_round_top transform");
				if (!magazine.transform.Find("follower_under_round_bottom")) Debug.LogError("Missing follower_under_round_bottom transform");

				if (magazine.transform.parent && magazine.transform.parent.parent && magazine.transform.parent.parent.GetComponent<GunScript>()) {
					GunScript gun = magazine.transform.parent.parent.GetComponent<GunScript>();

					if (!gun.transform.Find("load_progression/1")) Debug.LogError("No \"load_progression/1\" transform found");
					if (!gun.transform.Find("load_progression/2")) Debug.LogError("No \"load_progression/2\" transform found");
					if (!gun.transform.Find("load_progression/3")) Debug.LogError("No \"load_progression/3\" transform found");

					if (!gun.transform.Find("round_under_slide")) Debug.LogError("No \"round_under_slide\" transform found");
					if (!gun.transform.Find("slide/point_chambered_round")) Debug.LogError("No \"slide/point_chambered_round\" transform found");
					if (!gun.transform.Find("barrel/point_round_entering")) Debug.LogError("No \"barrel/point_round_entering\" transform found");
					if (!gun.transform.Find("barrel/point_chambered_round")) Debug.LogError("No \"barrel/point_chambered_round\" transform found");
				}
			}
		}
	}
}
#endif