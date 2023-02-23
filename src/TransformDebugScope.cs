using HarmonyLib;
using System;
using UnityEngine;

namespace Receiver2ModdingKit {
	/// <summary>
	/// Enables Transform debug when used in a scope and while using the debug build of the moddding kit
	/// Error message will also contain the last Transform.Find() call that failed, possibly simplifying debugging
	/// </summary>
	public class TransformDebugScope : IDisposable {
		public static string last_target {
			get;
			private set;
		}
		private static bool enabled = false;

		[HarmonyPatch(typeof(Transform), "Find")]
		[HarmonyPostfix]
		private static void PatchTransformFind(string n, Transform __result) {
			if (!enabled) return;

			if (__result == null) last_target = n;
		}

		public TransformDebugScope() {
			last_target = "";
			enabled = true;
		}

		public void Dispose() {
			enabled = false;
		}
	}
}
