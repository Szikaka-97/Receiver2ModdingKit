using System;
using UnityEngine;
using FMOD;
using FMOD.Studio;
using Receiver2;
using HarmonyLib;

namespace Receiver2ModdingKit.CustomSounds {
	/// <summary>
	/// Class containing all Harmony patches necessary for the custom sounds system to function
	/// </summary>
	internal class ModAudioPatches {
		//Legacy custom sounds patches
		//////////////////////////////

		[HarmonyPatch(typeof(AudioManager), nameof(AudioManager.PlayOneShot))]
		[HarmonyPrefix]
		private static bool PatchPlayOneShot(string sound_event, float volume) {
			if (sound_event == "") {
				UnityEngine.Debug.LogWarning("AudioManager.PlayOneShot(string, float): Tried to pass an empty string as event name");
				return false;
			}

			if (sound_event.Substring(0, "custom:/".Length) == "custom:/") {
				ModAudioManager.PlayOneShot(sound_event, volume);
				return false;
			}    

			return true;
		}

		[HarmonyPatch(typeof(AudioManager), nameof(AudioManager.PlayOneShot3D), new Type[] {typeof(string), typeof(Vector3), typeof(float), typeof(float)})]
		[HarmonyPrefix]
		private static bool PatchPlayOneShot3D(string sound_event, Vector3 pos, float volume = 1f, float pitch = 1f) {
			if (sound_event == "") {
				UnityEngine.Debug.LogWarning("AudioManager.PlayOneShot3D(string, Vector3, float, float): Tried to pass an empty string as event name");
				return false;
			}

			if (sound_event.Substring(0, "custom:/".Length) == "custom:/") {
				ModAudioManager.PlayOneShot3D(sound_event, pos, volume, pitch);
				return false;
			}    

			return true;
		}

		[HarmonyPatch(typeof(AudioManager), nameof(AudioManager.PlayOneShotAttached), new Type[] {typeof(string), typeof(GameObject)})]
		[HarmonyPrefix]
		private static bool PatchPlayOneShotAttached(string sound_event, GameObject obj) {
			if (sound_event == "") {
				UnityEngine.Debug.LogWarning("AudioManager.PlayOneShotAttached(string, GameObject): Tried to pass an empty string as event name");
				return false;
			}

			if (sound_event.Substring(0, "custom:/".Length) == "custom:/") {
				ModAudioManager.PlayOneShotAttached(sound_event, obj);
				return false;
			}    

			return true;
		}

		[HarmonyPatch(typeof(AudioManager), nameof(AudioManager.PlayOneShotAttached), new Type[] {typeof(string), typeof(GameObject), typeof(float)})]
		[HarmonyPrefix]
		private static bool PatchPlayOneShotAttached(string sound_event, GameObject obj, float volume) {
			if (sound_event == "") {
				UnityEngine.Debug.LogWarning("AudioManager.PlayOneShotAttached(string, GameObject, float): Tried to pass an empty string as event name");
				return false;
			}

			if (sound_event.Substring(0, "custom:/".Length) == "custom:/") {
				ModAudioManager.PlayOneShotAttached(sound_event, obj, volume);
				return false;
			}    

			return true;
		}

		//New banked sounds patches
		///////////////////////////
		
		[HarmonyPatch(typeof(FMOD.Studio.System), "lookupID")]
		[HarmonyPostfix]
		private static RESULT PatchLookupID(RESULT __result, FMOD.Studio.System __instance, string path, ref Guid id) {
			if (!ModAudioManager.mod_system.isValid() || ModAudioManager.mod_system.handle == __instance.handle) return __result;

			if (__result == RESULT.ERR_EVENT_NOTFOUND) {
				__result = ModAudioManager.mod_system.lookupID(path, out id);
			}

			return __result;
		}

		[HarmonyPatch(typeof(FMOD.Studio.System), "getEventByID")]
		[HarmonyPostfix]
		private static RESULT PatchGetEventByID(RESULT __result, FMOD.Studio.System __instance, Guid id, ref EventDescription _event) {
			var isValid = !ModAudioManager.mod_system.isValid();

			var isSameHandleAAAAAAAAAA = ModAudioManager.mod_system.handle == __instance.handle;

			if (isValid || isSameHandleAAAAAAAAAA)
			{
				return __result;
			}

			var isEventNotFound = __result == RESULT.ERR_EVENT_NOTFOUND;

			if (isEventNotFound) {
				__result = ModAudioManager.mod_system.getEventByID(id, out _event);
			}

			return __result;
		}
	}
}
