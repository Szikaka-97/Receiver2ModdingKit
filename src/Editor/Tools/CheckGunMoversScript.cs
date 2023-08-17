#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using Receiver2;

namespace Receiver2ModdingKit.Editor.Tools {
	[EditorTool("Check Gun Movers", typeof(GunScript))]
    public class CheckGunMoversScript : EditorTool {
        public override void OnToolGUI(EditorWindow window) {

        }
    }
}

/*
            ImGui.SetNextWindowSize(new Vector2(360f, 500f), ImGuiCond.FirstUseEver);
			if (ImGui.Begin("Gun Controls"))
			{
				float timeScale = Time.timeScale;
				if (ImGui.SliderFloat("Timescale", ref timeScale, 0.001f, 1f))
				{
					Time.timeScale = timeScale;
				}
				float num = this.trigger.target_amount;
				if (ImGui.SliderFloat("Trigger", ref num, 0f, 1f))
				{
					this.SetAnalogTriggerPressure(num);
				}
				num = this.hammer.target_amount;
				if (ImGui.SliderFloat("Hammer", ref num, 0f, 1f))
				{
					this.SetAnalogHammerPressure(num);
				}
				if (this.gun_type == GunType.Automatic)
				{
					num = this.slide.target_amount;
					if (ImGui.SliderFloat("Slide", ref num, 0f, 1f))
					{
						this.slide.target_amount = num;
					}
				}
				if (this.HasSafety())
				{
					num = this.safety.target_amount;
					if (ImGui.SliderFloat("Safety", ref num, 0f, 1f))
					{
						this.safety.target_amount = num;
					}
				}
				if (this.magazine_catch.transform != null)
				{
					num = this.magazine_catch.amount;
					if (ImGui.SliderFloat("Mag Release", ref num, 0f, 1f))
					{
						this.magazine_catch.amount = num;
						this.magazine_catch.UpdateDisplay();
					}
				}
				if (this.gun_model == GunModel.Detective)
				{
					if (ImGui.SliderFloat("Yoke/Crane", ref this.yoke_open, 0f, 1f))
					{
						this.ApplyTransform("open_cylinder / crane_pivot/crane", this.yoke_open, base.transform.Find("crane_pivot/crane"));
					}
					if (ImGui.SliderFloat("Extractor Rod", ref this.extractor_rod.amount, 0f, 1f))
					{
					}
				}
				else if (this.gun_model == GunModel.Model10)
				{
					ImGui.SliderFloat("Yoke/Crane", ref this.yoke_open, 0f, 1f);
					if (ImGui.SliderFloat("Extractor Rod", ref this.extractor_rod.amount, 0f, 1f))
					{
					}
				}
				else if (this.gun_model == GunModel.SAA)
				{
					ImGui.SliderFloat("Gate", ref this.yoke_open, 0f, 1f);
					ImGui.SliderFloat("Cylinder Spin", ref this.cylinder_angle_vel, -3f, 3f);
					if (ImGui.SliderFloat("Extractor Rod", ref this.extractor_rod.amount, 0f, 1f))
					{
						this.ExtractorRod();
					}
					if (ImGui.Button("Open/close gate"))
					{
						if (this.yoke_stage == YokeStage.Closed || this.yoke_stage == YokeStage.Closing)
						{
							this.SwingOutCylinder();
						}
						else
						{
							this.CloseCylinder();
						}
					}
				}
				else
				{
					num = this.magazine.amount;
					if (ImGui.SliderFloat("Magazine", ref num, 0f, 1f))
					{
						this.magazine.amount = num;
						this.magazine.UpdateDisplay();
						this.ApplyTransform("insert_mag / magazine_catch", 1f - num, base.transform.Find("magazine_catch"));
					}
					bool flag = this.magazine_catch.target_amount == 1f;
					if (ImGui.Checkbox("Mag Eject Button", ref flag))
					{
						if (!flag)
						{
							this.ReleaseMagEjectButton();
						}
						else
						{
							this.PressMagEjectButton();
						}
					}
				}
				if (this.gun_type == GunType.Automatic)
				{
					if (ImGui.SliderInt("Slide Stop", ref this.slide_stop_manual_target, -1, 1) && this.slide_stop_manual_target != 0)
					{
						this.slide_stop_locked = false;
					}
					if (ImGui.Button("Spawn Round In Chamber"))
					{
						this.SpawnRoundInChamber();
					}
					if (ImGui.Button("Spawn Magazine"))
					{
						MagazineScript magazineScript = ReceiverCoreScript.Instance().SpawnMagazine(base.InternalName, null);
						this.ImmediatelyInsertMag(magazineScript);
						if (this.debug_controls)
						{
							this.magazine_instance_in_gun.debug_controls = true;
						}
					}
				}
				else
				{
					ImGui.Text("Load Round");
					foreach (object obj in this.cylinder)
					{
						ChamberState chamberState = (ChamberState)obj;
						ImGui.SameLine();
						bool flag2 = chamberState.HasBullet();
						if (ImGui.Checkbox(chamberState.transform.name ?? "", ref flag2))
						{
							if (!flag2)
							{
								chamberState.BulletFallFromChamber(default(Vector3));
							}
							else if (!chamberState.HasBullet() && chamberState.IsEnabled())
							{
								chamberState.FillChamber();
							}
						}
					}
				}
				if (ImGui.Checkbox("X-Ray", ref this.xray))
				{
					this.ToggleXray(this.xray, true);
				}
			}
			ImGui.End();
*/

#endif