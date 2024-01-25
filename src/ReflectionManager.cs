using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Receiver2;
using HarmonyLib;

namespace Receiver2ModdingKit {
	internal static class ReflectionManager {
		public static AccessTools.FieldRef<GunScript, bool> GS_disconnector_needs_reset {
			get;
			private set;
		}
		public static AccessTools.FieldRef<GunScript, float> GS_hammer_halfcocked {
			get;
			private set;
		}
		public static AccessTools.FieldRef<GunScript, float> GS_hammer_cocked_val {
			get;
			private set;
		}
		public static AccessTools.FieldRef<GunScript, int> GS_hammer_state {
			get;
			private set;
		}

		public static AccessTools.FieldRef<GunScript, LinearMover> GS_select_fire {
			get;
			private set;
		}

		public static AccessTools.FieldRef<GunScript, float> GS_yoke_open {
			get;
			private set;
		}

		public static AccessTools.FieldRef<GunScript, int> GS_current_firing_mode_index {
			get;
			private set;
		}

		public static AccessTools.FieldRef<GunScript, LinearMover> GS_firing_pin {
			get;
			private set;
		}

		public static FieldInfo RCS_gun_prefabs_all {
			get;
			private set;
		}

		public static FieldInfo RCS_magazine_prefabs_all {
			get;
			private set;
		}

		public static AccessTools.FieldRef<GunScript, bool> GS_slide_stop_locked {
			get;
			private set;
		}

		public static Type LAH_BulletInventory {
			get;
			private set;
		}

		public static FieldInfo LAH_BulletInventory_item {
			get;
			private set;
		}

		public static Dictionary<PoseSpring, FieldInfo> LAH_pose_springs {
			get;
			private set;
		}

		public static MethodInfo LAH_Get_Last_Bullet {
			get;
			private set;
		}

		public static FieldInfo LAH_bullet_shake_time {
			get;
			private set;
		}

		public static FieldInfo PH_bounds {
			get;
			private set;
		}

		public static FieldInfo UEB_persistent_calls {
			get;
			private set;
		}

		public static MethodInfo PCG_Clear {
			get;
			private set;
		}

		public static FieldInfo OP_pool_map {
			get;
			private set;
		}

		private static AccessTools.FieldRef<I_Type, F_Type> GetFieldDelegate<I_Type, F_Type>(string field_name) {
			try {
				return AccessTools.FieldRefAccess<I_Type, F_Type>(field_name);
			} catch (ArgumentException) {
				Debug.LogError("Cannot find field \"" + field_name + "\", perhaps your Modding Kit plugin is out of date?");
				throw new MissingFieldException("Cannot Find Field: - " + typeof(F_Type).ToString() + " " + typeof(I_Type).ToString() + "." + field_name);
			}
		}

		private static FieldInfo GetFieldInfo(Type type, string fieldName) {
			FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

			if (field == null) {
				Debug.LogError("Cannot find field \"" + fieldName + "\", perhaps your Modding Kit plugin is out of date?");
				throw new MissingFieldException("Cannot Find Field: - " + type.ToString() + "." + fieldName);
			}

			return field;
		}

		private static MethodInfo GetMethodInfo(Type type, string methodName) { 
			MethodInfo method = null;	
			
			try {
				method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			} catch (AmbiguousMatchException) {
				Debug.LogError("Cannot identify method \"" + methodName + "\", perhaps your Modding Kit plugin is out of date?");
				throw new MissingFieldException("Wrong Method Parameters: - " + type.ToString() + "." + methodName);
			}

			if (method == null) {
				Debug.LogError("Cannot find method \"" + methodName + "\", perhaps your Modding Kit plugin is out of date?");
				throw new MissingFieldException("Cannot Find Method: - " + type.ToString() + "." + methodName);
			}

			return method;
		}

		private static MethodInfo GetMethodInfo(Type type, string methodName, params Type[] parameters) { 
			MethodInfo method = null;	

			try {
				method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, parameters, null);
			} catch (AmbiguousMatchException) {
				Debug.LogError("Cannot identify method \"" + methodName + "\", perhaps your Modding Kit plugin is out of date?");
				throw new MissingFieldException("Wrong Method Parameters: - " + type.ToString() + "." + methodName);
			}

			if (method == null) {
				Debug.LogError("Cannot find method \"" + methodName + "\", perhaps your Modding Kit plugin is out of date?");
				throw new MissingFieldException("Cannot Find Method: - " + type.ToString() + "." + methodName);
			}

			return method;
		}

		internal static void Initialize() {
			GS_disconnector_needs_reset = GetFieldDelegate<GunScript, bool>("disconnector_needs_reset");
			GS_hammer_halfcocked = GetFieldDelegate<GunScript, float>("hammer_halfcocked");
			GS_hammer_cocked_val = GetFieldDelegate<GunScript, float>("hammer_cocked_val");
			GS_hammer_state = GetFieldDelegate<GunScript, int>("hammer_state");
			GS_slide_stop_locked = GetFieldDelegate<GunScript, bool>("slide_stop_locked");
			GS_select_fire = GetFieldDelegate<GunScript, LinearMover>("select_fire");
			GS_yoke_open = GetFieldDelegate<GunScript, float>("yoke_open");
			GS_current_firing_mode_index = GetFieldDelegate<GunScript, int>("current_firing_mode_index");
			GS_firing_pin = GetFieldDelegate<GunScript, LinearMover>("firing_pin");

			PH_bounds = GetFieldInfo(typeof(PegboardHanger), "bounds");

			RCS_gun_prefabs_all = GetFieldInfo(typeof(ReceiverCoreScript), "gun_prefabs_all");
			RCS_magazine_prefabs_all = GetFieldInfo(typeof(ReceiverCoreScript), "magazine_prefabs_all");

			UEB_persistent_calls = GetFieldInfo(typeof(UnityEventBase), "m_PersistentCalls");

			OP_pool_map = GetFieldInfo(typeof(ObjectPool), "pool_map");

			PCG_Clear = GetMethodInfo(UEB_persistent_calls.FieldType, "Clear");

			LAH_BulletInventory = typeof(LocalAimHandler).GetNestedTypes(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Single(t => t.Name == "BulletInventory");
			LAH_BulletInventory_item = GetFieldInfo(LAH_BulletInventory, "item");
			LAH_pose_springs = new Dictionary<PoseSpring, FieldInfo>() {
				{ PoseSpring.Aim, GetFieldInfo(typeof(LocalAimHandler), "aim_spring")},
				{ PoseSpring.CameraZoom, GetFieldInfo(typeof(LocalAimHandler), "camera_zoom_spring")},
				{ PoseSpring.InspectGun, GetFieldInfo(typeof(LocalAimHandler), "inspect_gun_pose_spring")},
				{ PoseSpring.SlidePull, GetFieldInfo(typeof(LocalAimHandler), "slide_pose_spring")},
				{ PoseSpring.Reload, GetFieldInfo(typeof(LocalAimHandler), "reload_pose_spring")},
				{ PoseSpring.PressCheck, GetFieldInfo(typeof(LocalAimHandler), "press_check_pose_spring")},
				{ PoseSpring.InspectCylinder, GetFieldInfo(typeof(LocalAimHandler), "inspect_cylinder_pose_spring")},
				{ PoseSpring.AddRounds, GetFieldInfo(typeof(LocalAimHandler), "add_rounds_pose_spring")},
				{ PoseSpring.EjectRounds, GetFieldInfo(typeof(LocalAimHandler), "eject_rounds_pose_spring")},
			};

			LAH_Get_Last_Bullet = GetMethodInfo(typeof(LocalAimHandler), "GetLastMatchingLooseBullet");
			LAH_bullet_shake_time = GetFieldInfo(typeof(LocalAimHandler), "show_bullet_shake_time");
		}
	}
}
