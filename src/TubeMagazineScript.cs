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
		public Transform follower;
		public Transform first_round_position;

		public int capacity;
		public string insert_round_animation_path;
		public string remove_round_animation_path;

		public float cartridge_length = -1;
		public float follower_offset;

		public bool is_valid {
			get;
			private set;
		}

		public float GetAnimationSpeed() {
			if (ReceiverCoreScript.TryGetInstance(out var rcs)) {
				return rcs.player_stats.animation_speed;
			}
			return 1;
		}

		private GunScript gun;
		private Vector3 orig_follower_position;

		private bool removing_round = false;

		private float round_insert_time;
		private float last_round_inserted_time = -Mathf.Infinity;

		private float round_remove_time;
		private float last_round_removed_time = -Mathf.Infinity;

		public InventorySlot slot {
			get {
				return this.GetComponent<InventorySlot>();
			}
		}

		public bool busy {
			get {
				return Time.time < last_round_inserted_time + round_insert_time + Time.deltaTime || this.removing_round;
			}
		}

		public bool ready_to_remove_round {
			get {
				return this.removing_round && Time.time > last_round_removed_time + round_remove_time + Time.deltaTime;
			}
		}

		public int round_count {
			get {
				return this.rounds.Count;
			}
			set {
				if (this.is_valid && value != this.rounds.Count) {
					foreach (var round in this.rounds) {
						Destroy(round.gameObject);
					}

					this.removing_round = false;
					this.last_round_inserted_time = -Mathf.Infinity;
					this.last_round_removed_time = -Mathf.Infinity;

					this.rounds.Clear();

					for (int i = 0; i < value; i++) {
						var round = Instantiate(round_prefab).GetComponent<ShellCasingScript>();

						round.Move(this.slot);

						this.rounds.Push(round);

						round.transform.SetParent(this.first_round_position);

						round.transform.localPosition = Vector3.zero;
						round.transform.localRotation = Quaternion.identity;

						if (this.gun != null && !string.IsNullOrWhiteSpace(this.insert_round_animation_path)) {
							this.gun.ApplyTransform(this.insert_round_animation_path, Mathf.Infinity, round.transform);
						}
						else {
							round.transform.localPosition = Vector3.zero;
						}

						round.transform.localPosition += Vector3.forward * this.cartridge_length * i;
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
			if (this.first_round_position == null || this.round_prefab == null || this.follower == null) {
				Debug.LogError("Some crucial fields were not populated, fix it");

				this.is_valid = false;

				return;
			}

			if (this.round_prefab.GetComponent<ShellCasingScript>() == null) {
				Debug.LogError("Supplied round_prefab doesn't have a ShellCasingScript component, fix it");

				this.is_valid = false;

				return;
			}

			this.gun = GetComponentInParent<GunScript>();

			if (this.gun == null) {
				Debug.LogWarning("Magazine " + this.name + " isn't a child of a GunScript object, animating rounds will be disabled");
			}

			this.orig_follower_position = this.follower.localPosition;

			if (this.cartridge_length < 0) {
				var cartridge_collider = this.round_prefab.GetComponent<Collider>();

				if (cartridge_collider == null) {
					Debug.LogError("Cartridge" + this.round_prefab.GetComponent<ShellCasingScript>().InternalName + " doesn't have a Collider component. Fix it");

					this.is_valid = false;

					return;
				}

				Vector3 cartridge_length_forward = cartridge_collider.ClosestPoint(cartridge_collider.transform.position + cartridge_collider.transform.forward);
				Vector3 cartridge_length_backward = cartridge_collider.ClosestPoint(cartridge_collider.transform.position - cartridge_collider.transform.forward);

				cartridge_length = Vector3.Distance(cartridge_length_backward, cartridge_length_forward);
			}

			if (this.gun != null && !string.IsNullOrWhiteSpace(this.insert_round_animation_path)) {
				this.round_insert_time = GetAnimationTime(this.insert_round_animation_path);
			}
			else {
				this.round_insert_time = 1;
			}

			if (this.gun != null && !string.IsNullOrWhiteSpace(this.remove_round_animation_path)) {
				this.round_remove_time = GetAnimationTime(this.remove_round_animation_path);
			}
			else {
				this.round_remove_time = 1;
			}

			this.is_valid = true;
		}

		private void UpdateRoundPositions() {
			var rounds_array = this.rounds.ToArray();
			int first_round_index = 0;
			float offset = 0;

			if (this.last_round_inserted_time + this.round_insert_time * GetAnimationSpeed() >= Time.time - Time.deltaTime) {
				var time = Time.time - this.last_round_inserted_time;

				if (this.gun != null && !string.IsNullOrWhiteSpace(this.insert_round_animation_path)) {
					var inserted_round = this.rounds.Peek();

					this.gun.ApplyTransform(this.insert_round_animation_path, time, inserted_round.transform);

					float first_round_dot = Vector3.Dot(inserted_round.transform.localPosition, Vector3.forward);

					offset = Mathf.Clamp(first_round_dot, -this.cartridge_length, Mathf.Infinity);

					first_round_index = 1;
				}
				else {
					offset = this.cartridge_length * Mathf.Clamp01(time / this.round_insert_time) - this.cartridge_length;
				}
			}
			else if (this.removing_round) {
				var time = Time.time - this.last_round_removed_time;

				if (this.gun != null && !string.IsNullOrWhiteSpace(this.remove_round_animation_path)) {
					var removed_round = this.rounds.Peek();

					this.gun.ApplyTransform(this.remove_round_animation_path, time, removed_round.transform);

					float first_round_dot = Vector3.Dot(removed_round.transform.localPosition, Vector3.forward);

					offset = Mathf.Clamp(first_round_dot, -this.cartridge_length, Mathf.Infinity);

					first_round_index = 1;
				}
				else {
					offset = -this.cartridge_length * Mathf.Clamp01(time / this.round_insert_time);
				}
			}

			for (int round_index = first_round_index; round_index < round_count; round_index++) {
				rounds_array[round_index].transform.localPosition = Vector3.forward * (this.cartridge_length * round_index + offset);
			}

			if (this.round_count == 0 || (this.round_count == 1 && offset <= -this.cartridge_length)) {
				this.follower.localPosition = Vector3.MoveTowards(this.follower.localPosition, this.orig_follower_position, Time.deltaTime / this.round_remove_time);
			}
			else {
				this.follower.position = this.first_round_position.TransformPoint(Vector3.forward * (this.cartridge_length * (this.round_count - 1) + offset + this.follower_offset));
			}
		}

		private void Update() {
			UpdateRoundPositions();
		}

		/// <summary>
		/// Insert round into the magazine, if possible
		/// </summary>
		/// <param name="round"> Round to be inserted </param>
		public void InsertRound(ShellCasingScript round) {
			this.TryInsertRound(round);
		}

		/// <summary>
		/// Insert round into the magazine and return whether it was done
		/// </summary>
		/// <param name="round"> Round to be inserted </param>
		/// <returns> True if round was inserted, False otherwise </returns>
		public bool TryInsertRound(ShellCasingScript round) {
			if (!is_valid || this.rounds.Count >= capacity || this.busy) {
				return false;
			}

			round.Move(this.slot);

			this.rounds.Push(round);

			round.transform.SetParent(this.first_round_position);

			round.transform.localPosition = Vector3.zero;
			round.transform.localRotation = Quaternion.identity;

			if (this.gun != null && !string.IsNullOrWhiteSpace(this.insert_round_animation_path)) {
				this.gun.ApplyTransform(this.insert_round_animation_path, 0, round.transform);
			}
			else {
				round.transform.localPosition = -Vector3.forward * this.cartridge_length;
			}

			last_round_inserted_time = Time.time;

			UpdateRoundPositions();

			return true;
		}

		/// <summary>
		/// Begin the round removing animation and return whether there was anything to remove
		/// </summary>
		/// <returns> True if round was removed, False if there were no rounds or the magazine is busy </returns>
		public bool StartRemoveRound() {
			if (!is_valid || this.busy) {
				return false;
			}

			if (this.rounds.Count > 0) {
				this.last_round_removed_time = Time.time;

				this.removing_round = true;

				return true;
			}

			return false;
		}

		/// <summary>
		/// Retrieve the round that started to be removed in StartRemoveRound()
		/// </summary>
		/// <returns> Round that was being removed or Null if there was no such round </returns>
		public ShellCasingScript RetrieveRound() {
			if (!is_valid || !ready_to_remove_round) {
				return null;
			}

			this.removing_round = false;

			return rounds.Pop();
		}

		/// <summary>
		/// Try to retrieve the round that started to be removed in StartRemoveRound()
		/// </summary>
		/// <param name="round"> Retrieved round or null if failed to retrieve </param>
		/// <returns> True if the round was retrieved, False otherwise </returns>
		public bool TryRetrieveRound(out ShellCasingScript round) {
			round = this.RetrieveRound();

			return round != null;
		}
	}
}