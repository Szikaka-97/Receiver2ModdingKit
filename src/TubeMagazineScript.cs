using System.Collections.Generic;
using UnityEngine;
using Receiver2;
using System;
using System.Linq;

namespace Receiver2ModdingKit {
	[RequireComponent(typeof(InventorySlot))]
	public class TubeMagazineScript : MonoBehaviour {
		public Stack<ShellCasingScript> rounds = new Stack<ShellCasingScript>();

		public GameObject round_prefab;
		public int capacity;
		public Transform first_round_position;
		public string entering_round_animation_path;
		public string exiting_round_animation_path;
		public Transform follower;

		public float max_round_distance = 2;
		public int animation_precision = 5;

		public bool is_valid {
			get;
			private set;
		}

		private GunScript gun;
		private float cartridge_length;
		private Vector3 magazine_direction;
		private Vector3 orig_follower_position;

		private bool removing_round = false;

		private float round_add_time;
		private float last_round_added_time = -Mathf.Infinity;

		private float round_exit_time;
		private float last_round_removed_time = -Mathf.Infinity;

		public InventorySlot slot {
			get {
				return this.GetComponent<InventorySlot>();
			}
		}

		public bool busy {
			get {
				return Time.time < last_round_added_time + round_add_time + Time.deltaTime || this.removing_round;
			}
		}

		public bool ready_to_remove_round {
			get {
				return this.removing_round && Time.time > last_round_removed_time + round_exit_time + Time.deltaTime;
			}
		}

		public int round_count {
			get {
				return this.rounds.Count;
			}
			set {
				if (this.is_valid && value != this.round_count) {
					foreach (var round_in_magazine in this.rounds) {
						DestroyImmediate(round_in_magazine.gameObject);
					}

					this.rounds.Clear();

					for (int index = 0; index < value; index++) {
						var round = Instantiate(this.round_prefab);

						round.GetComponent<ShellCasingScript>().Move(this.slot);

						round.transform.parent = this.transform;

						this.rounds.Push(round.GetComponent<ShellCasingScript>());

						if (this.gun != null && !string.IsNullOrEmpty(this.entering_round_animation_path)) {
							this.gun.ApplyTransform(this.entering_round_animation_path, Mathf.Infinity, round.transform);

							round.transform.position = this.first_round_position.position + this.magazine_direction * this.cartridge_length * index;
						}
						else {
							round.transform.rotation = Quaternion.identity;
							round.transform.position = this.first_round_position.position + this.magazine_direction * this.cartridge_length * index;
						}
					}

					if (this.follower != null) {
						this.follower.localPosition = this.orig_follower_position + this.magazine_direction * this.cartridge_length * (value);
					}
				}
			}
		}

		private float GetAnimationTime(string path) {
			if (gun == null) return 0;

			float time = 0;

			foreach (var anim_index in this.gun.gun_animations.indices) {
				string anim_short_path = anim_index.path.Substring(0, anim_index.path.LastIndexOf("/") - 1);

				if (anim_short_path == path) {
					time = Mathf.Max(time, this.gun.gun_animations.keyframes[anim_index.index + (anim_index.length - 1) * 2] - this.gun.gun_animations.keyframes[anim_index.index]);
				}
			}

			return time;
		}

		public void Awake() {
			if (this.first_round_position == null || round_prefab == null) {
				Debug.LogError("Some crucial fields were not populated, fix it");

				this.is_valid = false;

				return;
			}

			if (round_prefab.GetComponent<ShellCasingScript>() == null) {
				Debug.LogError("Supplied round_prefab doesn't have a ShellCasingScript component, fix it");

				this.is_valid = false;

				return;
			}

			this.gun = GetComponentInParent<GunScript>();

			if (this.gun == null) {
				Debug.LogWarning("Magazine " + this.name + " isn't a child of a GunScript object, animating rounds will be disabled");
			}

			this.magazine_direction = first_round_position.forward;

			if (this.follower == null) {
				Debug.LogWarning("Magazine " + this.name + " doesn't have a follower");
			}
			else {
				if (this.follower.GetComponent<Collider>() == null) {
					Debug.LogWarning("Follower must have a collider component to animate correctly");
				}
				
				this.orig_follower_position = this.follower.localPosition;
			}

			Collider round_collider = round_prefab.GetComponent<Collider>();

			if (round_collider == null) {
				Debug.LogError("Supplied round_prefab doesn't have a collider in it, fix it");

				this.is_valid = false;

				return;
			}

			round_add_time = string.IsNullOrEmpty(this.entering_round_animation_path) ? 1 : GetAnimationTime(this.entering_round_animation_path);
			round_exit_time = string.IsNullOrEmpty(this.exiting_round_animation_path) ? 1 : GetAnimationTime(this.exiting_round_animation_path);

			float length_front = Vector3.Distance(round_collider.transform.position, round_collider.ClosestPoint(round_collider.transform.position + magazine_direction));
			float length_back = Vector3.Distance(round_collider.transform.position, round_collider.ClosestPoint(round_collider.transform.position - magazine_direction));

			cartridge_length = length_front + length_back;

			this.is_valid = true;
		}

		public void UpdateRoundPositions() {
			if (this.rounds.Count == 0) return;

			var round_array = this.rounds.ToArray();

			if (last_round_added_time + round_add_time + Time.deltaTime > Time.time) {
				var added_round = this.rounds.Peek();

				if (this.gun != null && !string.IsNullOrEmpty(entering_round_animation_path)) {
					this.gun.ApplyTransform(entering_round_animation_path, Time.time - last_round_added_time, added_round.transform);

					added_round.transform.position += first_round_position.localPosition;

					var most_forward_point = added_round.GetComponent<Collider>().ClosestPoint(added_round.transform.position + this.magazine_direction);

					if (round_array.Length > 1 && round_array[1].GetComponent<Collider>().ClosestPoint(most_forward_point) == most_forward_point) { //Rounds should be moved
						float high_dist = Time.deltaTime * max_round_distance;
						float low_dist = 0;

						float move_dist = (high_dist + low_dist) / 2;

						Vector3 orig_pos = round_array[1].transform.position;

						for (int iteration = 0; iteration < animation_precision; iteration++) {
							round_array[1].transform.position = orig_pos + this.magazine_direction * move_dist;

							if (round_array[1].GetComponent<Collider>().ClosestPoint(most_forward_point) == most_forward_point) { // More dist
								low_dist = move_dist;
							}
							else {
								high_dist = move_dist;
							}
							move_dist = (low_dist + high_dist) / 2;
						}

						for (int index = 2; index < round_array.Length; index++) {
							round_array[index].transform.position += this.magazine_direction * move_dist;
						}

						follower.transform.localPosition += this.magazine_direction * move_dist;
					}
					else if (round_array.Length == 1 && this.follower.TryGetComponent<Collider>(out var follower_collider) && follower_collider.ClosestPoint(most_forward_point) == most_forward_point) {
						float high_dist = Time.deltaTime * max_round_distance;
						float low_dist = 0;

						float move_dist = (high_dist + low_dist) / 2;

						Vector3 orig_pos = this.follower.transform.position;

						for (int iteration = 0; iteration < animation_precision; iteration++) {
							this.follower.transform.position = orig_pos + this.magazine_direction * move_dist;

							if (follower_collider.ClosestPoint(most_forward_point) == most_forward_point) { // More dist
								low_dist = move_dist;
							}
							else {
								high_dist = move_dist;
							}
							move_dist = (low_dist + high_dist) / 2;
						}

						this.follower.transform.position += this.magazine_direction * move_dist;
					}
				}
				else {
					float amount = Mathf.Clamp01((Time.time - last_round_added_time) / round_add_time);

					added_round.transform.position = Vector3.Lerp(
						first_round_position.position - this.magazine_direction * cartridge_length,
						first_round_position.position,
						amount
					);

					for (int i = 1; i < this.rounds.Count; i++) {
						round_array[i].transform.position = Vector3.Lerp(
							first_round_position.position + this.magazine_direction * cartridge_length * (i - 1),
							first_round_position.position + this.magazine_direction * cartridge_length * i,
							amount
						);
					}

					this.follower.localPosition = this.orig_follower_position + this.magazine_direction * (this.rounds.Count - 1 + amount) * this.cartridge_length;
				}
			}
			else if (this.removing_round) {
				var removed_round = this.rounds.Peek();

				if (this.gun != null && !string.IsNullOrEmpty(exiting_round_animation_path)) {
					this.gun.ApplyTransform(exiting_round_animation_path, Time.time - last_round_removed_time, removed_round.transform);

					removed_round.transform.position += first_round_position.localPosition;

					var most_forward_point = removed_round.GetComponent<Collider>().ClosestPoint(removed_round.transform.position + this.magazine_direction);

					if (round_array.Length > 1 && round_array[1].GetComponent<Collider>().ClosestPoint(most_forward_point) != most_forward_point) { //Rounds should be moved
						float high_dist = Time.deltaTime * max_round_distance;
						float low_dist = 0;

						float move_dist = (high_dist + low_dist) / 2;

						Vector3 orig_pos = round_array[1].transform.position;

						for (int iteration = 0; iteration < animation_precision; iteration++) {
							round_array[1].transform.position = orig_pos - this.magazine_direction * move_dist;

							if (round_array[1].GetComponent<Collider>().ClosestPoint(most_forward_point) == most_forward_point) { // less dist
								high_dist = move_dist;
							}
							else {
								low_dist = move_dist;
							}
							move_dist = (low_dist + high_dist) / 2;

							round_array[1].transform.position = orig_pos;
						}

						Vector3 prev_round_pos = round_array[1].transform.position;

						round_array[1].transform.position = Vector3.MoveTowards(round_array[1].transform.position, this.first_round_position.position, move_dist);

						move_dist = Vector3.Distance(prev_round_pos, round_array[1].transform.position);

						for (int index = 2; index < round_array.Length; index++) {
							round_array[index].transform.position = Vector3.MoveTowards(round_array[index].transform.position, this.first_round_position.position + this.magazine_direction * cartridge_length * (index - 1), move_dist);
						}

						this.follower.transform.localPosition = Vector3.MoveTowards(follower.transform.localPosition, this.orig_follower_position + (this.magazine_direction * cartridge_length * (rounds.Count - 1)), move_dist);
					}
					else if (round_array.Length == 1 && this.follower.TryGetComponent<Collider>(out var follower_collider)) {
						float high_dist = Time.deltaTime * max_round_distance;
						float low_dist = 0;

						float move_dist = (high_dist + low_dist) / 2;

						Vector3 orig_pos = this.follower.transform.position;

						for (int iteration = 0; iteration < animation_precision; iteration++) {
							this.follower.transform.position = orig_pos - this.magazine_direction * move_dist;

							if (follower_collider.ClosestPoint(most_forward_point) == most_forward_point) { // less dist
								high_dist = move_dist;
							}
							else {
								low_dist = move_dist;
							}
							move_dist = (low_dist + high_dist) / 2;

							this.follower.transform.position = orig_pos;
						}

						this.follower.transform.localPosition = Vector3.MoveTowards(follower.transform.localPosition, this.orig_follower_position + (this.magazine_direction * cartridge_length * (rounds.Count - 1)), move_dist);
					}
				}
				else {
					float amount =  Mathf.Clamp01((Time.time - last_round_removed_time) / round_exit_time);

					removed_round.transform.position = Vector3.Lerp(
						first_round_position.position,
						first_round_position.position - this.magazine_direction * cartridge_length,
						amount
					);

					for (int i = 1; i < this.rounds.Count; i++) {
						round_array[i].transform.position = Vector3.Lerp(
							first_round_position.position + this.magazine_direction * cartridge_length * i,
							first_round_position.position + this.magazine_direction * cartridge_length * (i - 1),
							amount
						);
					}

					this.follower.localPosition = this.orig_follower_position + this.magazine_direction * (this.rounds.Count - amount) * this.cartridge_length;
				}
			}
		}

		public void Update() {
			UpdateRoundPositions();
		}

		public void AddRound(ShellCasingScript round) {
			this.TryAddRound(round);
		}

		public bool TryAddRound(ShellCasingScript round) {
			if (!is_valid || this.rounds.Count >= capacity || this.busy) {
				return false;
			}

			round.Move(this.slot);

			round.transform.position = first_round_position.position - this.magazine_direction * cartridge_length;

			round.transform.parent = this.transform;

			this.rounds.Push(round);

			last_round_added_time = Time.time;

			UpdateRoundPositions();

			return true;
		}

		public bool StartRemoveRound() {
			if (!is_valid || this.busy) {
				return false;
			}

			if (this.rounds.Count > 0) {
				last_round_removed_time = Time.time;

				this.removing_round = true;

				return true;
			}

			return false;
		}

		public ShellCasingScript RemoveRound() {
			if (!is_valid || !ready_to_remove_round) {
				return null;
			}

			this.removing_round = false;

			return rounds.Pop();
		}
	}
}