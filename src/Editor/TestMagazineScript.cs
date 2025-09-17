#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using FMODUnity;
using ImGuiNET;
using SimpleJSON;
using UnityEngine;
using UnityEditor;
using Wolfire;
using Receiver2;

namespace Receiver2ModdingKit.Editor.Test {
	public class TestMagazineScript : InventoryItem
	{
		public MagazineClass MagazineClass
		{
			get
			{
				int num = (int)this.model;
				if (num >= 250)
				{
					return MagazineClass.HighCapacityGold;
				}
				if (num >= 200)
				{
					return MagazineClass.HighCapacity;
				}
				if (num >= 150)
				{
					return MagazineClass.StandardCapacityGold;
				}
				if (num >= 100)
				{
					return MagazineClass.StandardCapacity;
				}
				if (num >= 50)
				{
					return MagazineClass.LowCapacityGold;
				}
				return MagazineClass.LowCapacity;
			}
		}

		public override void Start()
		{
			base.Start();
			int num = this.rounds_in_mag - this.rounds.Count;
			int num2 = 0;
			while (num2 < num && num2 < this.kMaxRounds)
			{
				this.AddRound(UnityEngine.Object.Instantiate<GameObject>(this.round_prefab).GetComponent<ShellCasingScript>());
				num2++;
			}
		}

		public void UpdateRoundPositions()
		{
			this.follower_contacting_slide_stop = false;
			if (this.rounds.Count != 0)
			{
				Vector3 vector = Vector3.Normalize(this.round_bottom.localPosition - this.round_top.localPosition);
				Vector3 vector2 = vector / vector.y;
				if (this.slot && this.slot.type == InventorySlot.Type.Gun)
				{
					GunScript component = this.slot.GetComponent<GunScript>();
					Transform transform = this.slot.transform.Find("slide/point_chambered_round");
					float num = 0f;
					if (transform == null)
					{
						// component.MagInteraction(this);
					}
					else
					{
						this.forwards_amount = 0f;
						float num2 = Vector3.Dot(transform.position, base.transform.forward);
						float num3 = Vector3.Dot(this.round_top.position, base.transform.forward);
						float num4 = Vector3.Dot(this.slot.transform.Find("round_under_slide").position, base.transform.up);
						float num5 = Vector3.Dot(this.round_top.position, base.transform.up);
						if (!this.extracting)
						{
							if (num2 > num3 || (component.round_in_chamber != null && component.malfunction != GunScript.Malfunction.DoubleFeed))
							{
								if (num5 > num4)
								{
									num = (num5 - num4) / this.rounds[0].GetComponent<BoxCollider>().size[1];
									if (component.malfunction == GunScript.Malfunction.FailureToFeed && component.sub_malfunction == GunScript.MalfunctionSub.FTF_Clearable)
									{
										component.malfunction = GunScript.Malfunction.None;
									}
								}
							}
							else if ((component.round_in_chamber == null || component.malfunction == GunScript.Malfunction.DoubleFeed) && this.press_amount < Time.deltaTime * 50f * 2f)
							{
								this.extracting = true;
								this.extractor_forward_d_max = float.MinValue;
								if (component.malfunction == GunScript.Malfunction.FailureToFeed && component.sub_malfunction == GunScript.MalfunctionSub.FTF_Start)
								{
									component.sub_malfunction = GunScript.MalfunctionSub.FTF_Clearable;
								}
							}
						}
						if (component.malfunction == GunScript.Malfunction.FailureToFeed)
						{
							this.extracting = false;
						}
						if (this.extracting)
						{
							this.extractor_forward_d_max = Mathf.Max(num2 - num3, this.extractor_forward_d_max);
							num2 = this.extractor_forward_d_max + num3;
							Transform[] array = new Transform[6];
							int num6 = 0;
							array[0] = this.round_top.transform;
							array[1] = this.slot.transform.Find("load_progression/1");
							array[2] = this.slot.transform.Find("load_progression/2");
							array[3] = this.slot.transform.Find("load_progression/3");
							array[4] = this.slot.transform.Find("barrel/point_round_entering");
							array[5] = this.slot.transform.Find("barrel/point_chambered_round");
							num = 0f;
							this.rounds[0].transform.CopyPosRot(this.round_top);
							float[] array2 = new float[6];
							for (int i = 0; i < 6; i++)
							{
								array2[i] = Vector3.Dot(array[i].position, base.transform.forward);
							}
							for (int j = 0; j < 5; j++)
							{
								if (num2 >= array2[j] && num2 < array2[j + 1])
								{
									num6 = j;
									float num7 = (num2 - array2[j]) / (array2[j + 1] - array2[j]);
									this.rounds[0].transform.position = Vector3.Lerp(array[j].position, array[j + 1].position, num7);
									this.rounds[0].transform.rotation = Quaternion.Lerp(array[j].rotation, array[j + 1].rotation, num7);
									if (num6 == 3)
									{
										num = Mathf.Lerp(0f, (num5 - num4) / this.rounds[0].GetComponent<BoxCollider>().size[1] - 1f, num7);
									}
								}
							}
							if (num2 >= array2[5])
							{
								num6 = 5;
								this.rounds[0].transform.position = array[5].position;
								this.rounds[0].transform.rotation = array[5].rotation;
							}
							if (num6 >= 4 && component.malfunction != GunScript.Malfunction.DoubleFeed)
							{
								this.extracting = false;
								if (component.malfunction == GunScript.Malfunction.None && component.sub_malfunction == GunScript.MalfunctionSub.FTF_Clearable)
								{
									ReceiverEvents.TriggerEvent(ReceiverEventTypeInt.GunMalfunctionCleared, 3);
									component.sub_malfunction = GunScript.MalfunctionSub.None;
								}
								component.ReceiveRound(this.RemoveRound());
								this.press_amount += 0.5f;
							}
							if (component.malfunction == GunScript.Malfunction.DoubleFeed)
							{
								num = 0.2f;
								this.rounds[0].transform.localPosition -= vector2 * num * this.rounds[0].GetComponent<BoxCollider>().size[1];
							}
							if (this.rounds.Count > 0 && num5 > num4)
							{
								num = Mathf.Max(num, (num5 - num4) / this.rounds[0].GetComponent<BoxCollider>().size[1] - 0.5f);
							}
						}
					}
					if (num > this.press_amount)
					{
						this.press_amount = num;
					}
					else
					{
						this.press_amount = Mathf.MoveTowards(this.press_amount, num, Time.deltaTime * 50f);
					}
				}
				for (int k = 0; k < this.rounds.Count; k++)
				{
					if (k != 0 || !this.extracting)
					{
						Transform transform2 = this.rounds[k].transform;
						Vector3 vector3 = this.round_top.localPosition;
						Quaternion quaternion = this.round_top.localRotation;
						BoxCollider component2 = transform2.GetComponent<BoxCollider>();
						float num8 = (float)k + this.press_amount;
						float num9 = Mathf.Clamp(num8 * this.round_position_params.param_a, 0f, 1f);
						bool flag = k % 2 == 0 == (this.rounds.Count % 2 == 0) != this.round_position_params.mirror_double_stack;
						vector3 -= vector2 * component2.size[1] * (num8 * this.round_position_params.param_d + this.round_position_params.param_b * Mathf.Clamp(num9 * this.round_position_params.param_c, 0f, 1f));
						vector3 += Vector3.right * component2.size[0] * (flag ? (-this.round_position_params.param_e) : this.round_position_params.param_e) * num9;
						if (this.round_position_params.curve_positions.Length != 0)
						{
							Vector3 vector4 = Utility.EvaluateBezier(num8 / (float)this.kMaxRounds, this.round_position_params.curve_positions);
							vector4.y = 0f;
							vector3 += vector4;
							quaternion = Quaternion.Lerp(this.round_top.localRotation, this.round_bottom.localRotation, num8 / (float)this.kMaxRounds);
						}
						if (k == 0)
						{
							vector3 += (this.round_insert.localPosition - this.round_top.localPosition) * this.forwards_amount;
						}
						transform2.localRotation = quaternion;
						transform2.localPosition = vector3;
					}
				}
				if (this.rounds.Count > 0)
				{
					Vector3 vector5;
					if (this.rounds.Count == 1 || this.gun_model == GunModel.m1911 || this.gun_model == GunModel.HiPoint)
					{
						vector5 = base.transform.Find("follower_under_round_top").localPosition - base.transform.Find("round_top").localPosition;
					}
					else
					{
						vector5 = base.transform.Find("follower_under_round_bottom").localPosition - base.transform.Find("round_bottom").localPosition;
					}
					float num10 = Mathf.Min((this.rounds[this.rounds.Count - 1].transform.localPosition + vector5)[1] - this.orig_follower_local_position[1], 0f);
					Vector3 vector6 = this.orig_follower_local_position;
					if (this.round_position_params.curve_positions.Length != 0)
					{
						Vector3 vector7 = Utility.EvaluateBezier(1f - ((float)(this.rounds_in_mag + 1) + this.press_amount) / (float)this.kMaxRounds, this.round_position_params.curve_positions);
						vector7.y = 0f;
						vector6 += vector7;
						this.follower.localRotation = Quaternion.Lerp(base.transform.Find("follower_under_round_bottom").localRotation, base.transform.Find("follower_under_round_top").localRotation, 1f - ((float)(this.rounds_in_mag + 1) + this.press_amount) / (float)this.kMaxRounds);
					}
					vector6 += this.follower_offset;
					vector6 += vector2 * num10;
					this.follower.localPosition = vector6;
				}
			}
			if (this.rounds.Count == 0)
			{
				this.follower.localPosition = this.orig_follower_local_position;
			}
			if (this.rounds.Count < 2 && this.slot && this.slot.type == InventorySlot.Type.Gun)
			{
				Transform transform3 = this.follower.Find("slide_stop_interact");
				Transform transform4 = this.slot.transform.Find("slide_stop/follower_interact");
				if (transform4 != null)
				{
					float num11 = transform4.lossyScale[0] * transform4.GetComponent<SphereCollider>().radius;
					Vector3 vector8 = Vector3.Normalize(this.round_bottom.position - this.round_top.position);
					float num12 = Vector3.Dot(vector8, transform3.position);
					float num13 = Vector3.Dot(vector8, transform4.position) + num11;
					if (num12 < num13)
					{
						this.follower.position += vector8 * (num13 - num12);
						this.follower_contacting_slide_stop = true;
					}
				}
			}
			if (this.spring != null)
			{
				this.spring.UpdateScale();
				this.spring.UpdateDirection();
			}
			foreach (ShellCasingScript shellCasingScript in this.rounds)
			{
				if (shellCasingScript.pose_events.Count > 0)
				{
					PoseEvent poseEvent = shellCasingScript.pose_events[shellCasingScript.pose_events.Count - 1];
					poseEvent.position = shellCasingScript.transform.localPosition;
					poseEvent.rotation = shellCasingScript.transform.localRotation;
				}
			}
			foreach (ShellCasingScript shellCasingScript2 in this.rounds)
			{
				if (shellCasingScript2.pose_events.Count > 0)
				{
					Vector3 vector9;
					Quaternion quaternion2;
					float num14;
					LocalAimHandler.EvaluatePoseEvents(shellCasingScript2.pose_events, out vector9, out quaternion2, out num14, 1f);
					shellCasingScript2.transform.position = vector9;
					shellCasingScript2.transform.rotation = quaternion2;
					shellCasingScript2.transform.localScale = Vector3.one;
				}
			}
		}

		public void DeleteRound()
		{
			if (ConfigFiles.global.infinite_ammo)
			{
				this.Fill();
			}
			if (this.rounds.Count > 0)
			{
				Component component = this.rounds[0];
				this.rounds.RemoveAt(0);
				this.rounds_in_mag = this.rounds.Count;
				this.UpdateRoundPositions();
				UnityEngine.Object.Destroy(component.gameObject);
			}
		}

		public ShellCasingScript RemoveRound()
		{
			if (ConfigFiles.global.infinite_ammo)
			{
				this.Fill();
			}
			if (this.rounds.Count > 0)
			{
				ShellCasingScript shellCasingScript = this.rounds[0];
				this.rounds.RemoveAt(0);
				this.rounds_in_mag = this.rounds.Count;
				this.UpdateRoundPositions();
				return shellCasingScript;
			}
			return null;
		}

		public bool IsFull()
		{
			return this.rounds.Count == this.kMaxRounds;
		}

		public bool IsEmpty()
		{
			return this.rounds.Count == 0;
		}

		public bool AddRound(ShellCasingScript round)
		{
			if (this.IsFull())
			{
				return false;
			}
			this.rounds.Insert(0, round);
			this.rounds_in_mag = this.rounds.Count;
			round.transform.parent = base.transform;
			round.transform.localScale = new Vector3(1f, 1f, 1f);
			round.Move(base.GetComponent<InventorySlot>());
			PoseEvent poseEvent = new PoseEvent();
			poseEvent.parent = base.transform;
			poseEvent.position = new Vector3(0f, 0f, 0f);
			poseEvent.rotation = Quaternion.identity;
			poseEvent.scale = 1f;
			poseEvent.time = Time.time;
			poseEvent.transition = InventorySlot.fast_spring_baked;
			round.pose_events.Add(poseEvent);
			this.UpdateRoundPositions();
			return true;
		}

		public bool AddRound()
		{
			return this.AddRound(UnityEngine.Object.Instantiate<GameObject>(this.round_prefab).GetComponent<ShellCasingScript>());
		}

		public int NumRounds()
		{
			return this.rounds.Count;
		}

		public void ToggleXray(bool enabled, bool force = false)
		{
			if (this.xray == enabled && !force)
			{
				return;
			}
			foreach (GunPartMaterial gunPartMaterial in this.gun_part_materials)
			{
				Renderer renderer;
				if (gunPartMaterial && gunPartMaterial.TryGetComponent<Renderer>(out renderer))
				{
					renderer.sharedMaterial = (enabled ? gunPartMaterial.xray_material : gunPartMaterial.material);
				}
			}
			// ProjectionRenderer[] componentsInChildren = base.GetComponentsInChildren<ProjectionRenderer>();
			// for (int i = 0; i < componentsInChildren.Length; i++)
			// {
			// 	componentsInChildren[i].gameObject.SetActive(!enabled);
			// }
			this.xray = enabled;
		}

		public override void Awake()
		{
			base.Awake();

			this.rounds.Clear();

			this.ToggleXray(false, true);
			this.spring = null;
			Transform transform = base.transform.Find("magazine/magazine_spring");
			if (transform != null)
			{
				this.spring = transform.GetComponent<SpringCompressInstance>();
			}
			this.follower = base.transform.Find("follower");
			this.round_insert = base.transform.Find("round_insert");
			this.round_top = base.transform.Find("round_top");
			this.round_bottom = base.transform.Find("round_bottom");
			Transform transform2 = base.transform.Find("follower_empty") ?? this.follower;
			this.orig_follower_local_position = transform2.localPosition;
		}

		public void Fill()
		{
			while (!this.IsFull())
			{
				this.AddRound(UnityEngine.Object.Instantiate<GameObject>(this.round_prefab).GetComponent<ShellCasingScript>());
			}
		}

		public void SetRoundCount(int count)
		{
			if (count < 0)
			{
				count = 0;
			}
			if (count > this.kMaxRounds)
			{
				count = this.kMaxRounds;
			}
			while (this.rounds.Count < count)
			{
				this.AddRound(UnityEngine.Object.Instantiate<GameObject>(this.round_prefab).GetComponent<ShellCasingScript>());
			}
			while (this.rounds.Count > count)
			{
				this.DeleteRound();
			}
		}

		protected override void Update()
		{
			base.Update();
			if (this.debug_controls)
			{
				ImGui.SetNextWindowSize(new Vector2(360f, 500f), ImGuiCond.FirstUseEver);
				if (ImGui.Begin("Mag Controls"))
				{
					if (ImGui.SliderFloat("Press Amount", ref this.press_amount, 0f, 1f))
					{
						this.UpdateRoundPositions();
					}
					if (ImGui.SliderFloat("param_a", ref this.round_position_params.param_a, 0.01f, 4f))
					{
						this.UpdateRoundPositions();
					}
					if (ImGui.SliderFloat("param_b", ref this.round_position_params.param_b, 0.01f, 4f))
					{
						this.UpdateRoundPositions();
					}
					if (ImGui.SliderFloat("param_c", ref this.round_position_params.param_c, 0.01f, 4f))
					{
						this.UpdateRoundPositions();
					}
					if (ImGui.SliderFloat("param_d", ref this.round_position_params.param_d, 0.01f, 4f))
					{
						this.UpdateRoundPositions();
					}
					if (ImGui.SliderFloat("param_e", ref this.round_position_params.param_e, 0.01f, 4f))
					{
						this.UpdateRoundPositions();
					}
					if (ImGui.Checkbox("mirror_double_stack", ref this.round_position_params.mirror_double_stack))
					{
						this.UpdateRoundPositions();
					}
					if (ImGui.SliderFloat("Forward Amount", ref this.forwards_amount, 0f, 1f))
					{
						this.UpdateRoundPositions();
					}
					if (ImGui.Button("Add Round"))
					{
						this.AddRound(UnityEngine.Object.Instantiate<GameObject>(this.round_prefab).GetComponent<ShellCasingScript>());
					}
					if (ImGui.Button("Remove Round"))
					{
						UnityEngine.Object.Destroy(this.RemoveRound().gameObject);
					}
					if (ImGui.Button("Fill"))
					{
						this.Fill();
					}
				}
				ImGui.End();
			}
		}

		public void FixedUpdate()
		{
			Component component;
			if (this.rigid_body != null && !this.rigid_body.IsSleeping() && base.TryGetComponent(typeof(Collider), out component) && ((Collider)component).enabled && Time.time - this.physics_activated_time > 2f)
			{
				this.rigid_body.Sleep();
			}
			if (this.rigid_body != null && Time.time - this.physics_activated_time > 1f)
			{
				this.EnableGlint = true;
				return;
			}
			if (this.EnableGlint)
			{
				this.EnableGlint = false;
			}
		}

		protected override void OnCollisionEnter(Collision collision)
		{
			if (!this.physics_collided)
			{
				base.OnCollisionEnter(collision);
				this.physics_collided = true;
				AudioManager audioManager = AudioManager.Instance();
				if (audioManager != null)
				{
					AudioManager.PlayOneShotAttached(audioManager.sound_event_mag_drop, base.gameObject);
				}
			}
		}

		public override void OnChangeInventorySlot(InventorySlot old_slot, InventorySlot new_slot, LocalAimHandler.Hand from_hand, LocalAimHandler.Hand to_hand)
		{
			if (new_slot == null && this.xray)
			{
				this.ToggleXray(false, false);
			}
			GunScript gunScript;
			if (old_slot == null && LocalAimHandler.player_instance.TryGetGun(out gunScript) && gunScript.xray)
			{
				this.ToggleXray(true, false);
			}
		}

		public override void SetPersistentData(JSONObject data)
		{
			this.rounds_in_mag = data["rounds_in_mag"];
			this.spring_quality = data["spring_quality"];
		}

		public override JSONObject GetPersistentData()
		{
			JSONObject jsonobject = new JSONObject();
			jsonobject.Add("rounds_in_mag", this.rounds_in_mag);
			jsonobject.Add("spring_quality", this.spring_quality);
			return jsonobject;
		}

		public override string TypeName()
		{
			return "magazine";
		}

		public MagazineModel model;

		public Material magazine_material;

		public int kMaxRounds = 8;

		public bool debug_controls;

		[HideInInspector]
		public SpringCompressInstance spring;

		public float spring_quality = 1f;

		[EventRef]
		public string sound_event_add_round;

		[EventRef]
		public string sound_event_thumb_push_down;

		[EventRef]
		public string sound_event_thumb_release;

		[EventRef]
		public string sound_event_bullet_insert_vertical;

		[EventRef]
		public string sound_event_bullet_remove_vertical;

		[EventRef]
		public string sound_event_bullet_insert_horizontal;

		[HideInInspector]
		public List<ShellCasingScript> rounds;

		public GameObject round_prefab;

		[HideInInspector]
		public Transform follower;

		[HideInInspector]
		public Transform round_insert;

		[HideInInspector]
		public Transform round_top;

		[HideInInspector]
		public Transform round_bottom;

		public Transform left_touch_offset;

		public Transform right_touch_offset;

		[HideInInspector]
		public float press_amount;

		[HideInInspector]
		public float forwards_amount;

		private Vector3 orig_follower_local_position;

		public Vector3 follower_offset = Vector3.zero;

		[HideInInspector]
		public bool extracting;

		private float extractor_forward_d_max = float.MinValue;

		public bool follower_contacting_slide_stop;

		public bool xray;

		public GunModel gun_model;

		public string magazine_root_type;

		public MagazineScript.MagazineRoundPositionParams round_position_params = new MagazineScript.MagazineRoundPositionParams();

		public Transform tutorial_point_round_insert;

		public GunPartMaterial[] gun_part_materials;

		public int rounds_in_mag;

		[Serializable]
		public class MagazineRoundPositionParams
		{
			public MagazineRoundPositionParams()
			{
				this.param_a = 0f;
				this.param_b = 0f;
				this.param_c = 0f;
				this.param_d = 1f;
				this.param_e = 0f;
				this.curve_positions = new Vector3[0];
				this.mirror_double_stack = false;
			}

			public float param_a;

			public float param_b;

			public float param_c;

			public float param_d;

			public float param_e;

			public Vector3[] curve_positions;

			public bool mirror_double_stack;
		}
	}

	[CustomEditor(typeof(TestMagazineScript))]
	public class TestMagazineEditor : UnityEditor.Editor {
		public override void OnInspectorGUI() {
			serializedObject.Update();
			var mag = target as TestMagazineScript;

			EditorGUILayout.PropertyField(serializedObject.FindProperty("round_prefab"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("round_position_params"));

			EditorGUILayout.Space(20);

			EditorGUILayout.LabelField("Num Rounds", mag.NumRounds().ToString());

			EditorGUILayout.Space(20);

			mag.rounds.RemoveAll( bullet => bullet == null );

			if (GUILayout.Button("Add Round") && mag.NumRounds() < mag.kMaxRounds) {
			   mag.AddRound(Instantiate(mag.round_prefab).GetComponent<ShellCasingScript>());
			}

			if (GUILayout.Button("Remove Round") && mag.NumRounds() > 0) {
				DestroyImmediate(mag.RemoveRound().gameObject);

				// int round_count = mag.NumRounds() - 1;

				// foreach(var round in mag.rounds) if (round && round.gameObject) DestroyImmediate(round.gameObject);
				// mag.rounds.Clear();

				// for (int i = 0; i < round_count; i++) {
				// 	mag.AddRound(Instantiate(mag.round_prefab).GetComponent<ShellCasingScript>());
				// }
			}

			if (serializedObject.ApplyModifiedProperties()) {
				mag.UpdateRoundPositions();
			}
		}
	}
}

#endif