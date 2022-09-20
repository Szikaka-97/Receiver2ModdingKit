using System;
using System.Collections.Generic;
using HarmonyLib;
using Receiver2;
using UnityEngine;
using System.IO;
using FMOD.Studio;
using FMODUnity;
using System.Runtime.InteropServices;

namespace Receiver2ModdingKit.CustomSounds {
	public class ModAudioManager {
		public static readonly string TAPE_EVENT = "event:/TextToSpeech/TextToSpeech - tape";

		public static readonly EVENT_CALLBACK TAPE_CALLBACK = new EVENT_CALLBACK( (EVENT_CALLBACK_TYPE type, EventInstance instance, IntPtr parameter_ptr) => { //Copied from the dll
			instance.getUserData(out IntPtr value);
			GCHandle gchandle = GCHandle.FromIntPtr(value);
			CustomEventInstanceUserData eventInstanceUserData = gchandle.Target as CustomEventInstanceUserData;
			
			if (type != EVENT_CALLBACK_TYPE.DESTROYED) {
				if (type != EVENT_CALLBACK_TYPE.CREATE_PROGRAMMER_SOUND) {
					if (type == EVENT_CALLBACK_TYPE.DESTROY_PROGRAMMER_SOUND) {
						PROGRAMMER_SOUND_PROPERTIES properties = Marshal.PtrToStructure<PROGRAMMER_SOUND_PROPERTIES>(parameter_ptr);
					}
				} else {
					PROGRAMMER_SOUND_PROPERTIES properties = Marshal.PtrToStructure<PROGRAMMER_SOUND_PROPERTIES>(parameter_ptr);
					properties.sound = eventInstanceUserData.sound_handle;
					Marshal.StructureToPtr(properties, parameter_ptr, false);
				}
			} else {
				gchandle.Free();
			}

			return FMOD.RESULT.OK;
		});

		public class CustomEventInstanceUserData { public IntPtr sound_handle; }

		private static Dictionary<string, SoundAsset> customEvents = new();
		private static List<string> prefixes = new();

		/// <summary>
		/// Load all sound files at the specified directory upward.
		/// Event names will be created from folder structure in the directory
		/// </summary>
		/// <param name="prefix"> Prefix given to your sound events, should be unique among all mods </param>
		/// <param name="directory"> Directory to search for files in </param>
		public static void LoadCustomEvents(string prefix, string directory) {
			DirectoryInfo directoryInfo;

			try {
				directoryInfo = new DirectoryInfo(directory);
			} catch (ArgumentNullException) {
				Debug.LogError("ModAudioManager.LoadCustomEvents(string, string): Provided directory path is null");
				return;
			} catch (System.Security.SecurityException) {
				Debug.LogError("ModAudioManager.LoadCustomEvents(string, string): You do not have the permission to access directory at path " + directory);
				return;
			} catch (ArgumentException) {
				Debug.LogError("ModAudioManager.LoadCustomEvents(string, string): Path \"" + directory + "\" contains invalid characters");
				return;
			} catch (PathTooLongException) {
				Debug.LogError("ModAudioManager.LoadCustomEvents(string, string): Path \"" + directory + "\" is too long (> 260 characters)");
				return;
			}

			if (!directoryInfo.Exists) {
				Debug.LogError("ModAudioManager.LoadCustomEvents(string, string): Directory \"" + directory + "\" doesn't exist");
				return;
			}
			if (prefix == "") {
				Debug.LogWarning("ModAudioManager.LoadCustomEvents(string, string): Parameter \"assetBundleName\" shouldn't be empty");
			}

			foreach (var file in directoryInfo.GetFiles("*", SearchOption.AllDirectories)) {
			
				string assetPath = file.FullName;

				assetPath = "custom:/" + 
					prefix.ToLower() + 
					assetPath.Substring(directoryInfo.FullName.Length, assetPath.Length - file.Extension.Length - directoryInfo.FullName.Length)
						.Replace('\\', '/')
						.ToLower();

				var result = RuntimeManager.CoreSystem.createSound(file.FullName, FMOD.MODE.DEFAULT, out FMOD.Sound sound);

				if (result == FMOD.RESULT.OK) {
					if (customEvents.ContainsKey(assetPath)) {
						Debug.LogWarning("ModAudioManager.LoadCustomEvents(string, string): Asset \"" + assetPath + "\" already exists and will be overriden");
						customEvents[assetPath] = new SoundAsset(assetPath, sound);
					} else {
						customEvents.Add(assetPath, new SoundAsset(assetPath, sound));
					}
				}
			}

			prefixes.Add(prefix);
		}

		/// <summary>
		/// Get all loaded custom sound events
		/// </summary>
		/// <returns></returns>
		public static string[] GetAllEvents() {
			List<string> events = new List<string>();

			foreach (var it in customEvents) {
				events.Add(it.Key);
			}

			return events.ToArray();
		}

		/// <summary>
		/// Get all loaded custom sound events with a given prefix
		/// </summary>
		/// <param name="prefix"> Prefix that you want to search for </param>
		/// <returns></returns>
		public static string[] GetAllEventsWithPrefix(string prefix) {
			List<string> events = new List<string>();

			foreach (var it in customEvents) {
				if (it.Key.Length > 8 && it.Key.Substring(8, prefix.Length).Equals(prefix, StringComparison.OrdinalIgnoreCase)) events.Add(it.Key);
			}

			return events.ToArray();
		}

		/// <summary>
		/// Get all gun sound prefixes
		/// </summary>
		/// <returns></returns>
		public static string[] GetAllPrefixes() { return prefixes.ToArray(); }
		
		/// <summary>
		/// Check if a prefix is present
		/// </summary>
		/// <param name="prefix"> Prefix that you want to search for </param>
		/// <returns></returns>
		public static bool IsPrefixPresent(string prefix) { return prefixes.Contains(prefix); }

		//Coroutines that play custom events
		////////////////////////////////////

		/// <summary>
		/// Play a sound event and stop it after specified time
		/// </summary>
		/// <param name="instance"> EventInstance to play and then stop </param>
		/// <param name="length"> Time after which to stop playing the instance in milliseconds </param>
		/// <returns></returns>
		public static System.Collections.IEnumerator PlayCustomEvent(EventInstance instance, uint length) { //This is stupid. Yes, I know
			instance.setPaused(false);
			instance.start();
			instance.release();

			yield return new WaitForSeconds((float) length / 1000f);

			instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);

			yield break;
		}

		/// <summary>
		/// Play a sound event and stop it after specified time
		/// </summary>
		/// <param name="asset"> InstanceAsset to play and then stop </param>
		/// <returns></returns>
		public static System.Collections.IEnumerator PlayCustomEvent(InstanceAsset asset) { //This is stupid. Yes, I know
			if (asset == null) {
				throw new ArgumentNullException("asset", "ModAudioManager.playCustomEvent(InstanceAsset): Parameter \"asset\" can't be null");
			}

			asset.instance.setPaused(false);
			asset.instance.start();
			asset.instance.release();

			yield return new WaitForSeconds((float) asset.length / 1000f);

			asset.instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);

			yield break;
		}


		//Event play methods
		////////////////////

		/// <summary>
		/// Create an InstanceAsset event, then return it
		/// </summary>
		/// <param name="sound_event"> Event to create, custom events start with "custom:/" </param>
		public static InstanceAsset CreateInstance(string sound_event) {
			if (!customEvents.ContainsKey(sound_event)) {
				Debug.LogError("ModAudioManager.CreateInstance(string): Tried to play an event that doesn't exist: \"" + sound_event + "\"\nDid you load it using ModAudioManager.LoadCustomEvents(string, string); ?");
				return null;
			}

			SoundAsset soundEvent = customEvents[sound_event];

			InstanceAsset asset = new InstanceAsset(soundEvent);

			return asset;
		}

		/// <summary>
		/// Play an InstanceAsset event, then return it
		/// </summary>
		/// <param name="sound_event"> Event to play, custom events start with "custom:/" </param>
		/// <param name="volume"> Volume to play the event at </param>
		public static InstanceAsset Play(string sound_event, float volume = 1) {
			if (!customEvents.ContainsKey(sound_event)) {
				Debug.LogError("ModAudioManager.Play(string, float): Tried to play an event that doesn't exist: \"" + sound_event + "\"\nDid you load it using ModAudioManager.LoadCustomEvents(string, string); ?");
				return null;
			}

			SoundAsset soundEvent = customEvents[sound_event];

			InstanceAsset asset = new InstanceAsset(soundEvent);

			asset.instance.setVolume(volume);

			asset.instance.start();

			return asset;
		}

		/// <summary>
		/// Play the specified event and then release it
		/// </summary>
		/// <param name="sound_event"> Event to play, custom events start with "custom:/" </param>
		/// <param name="volume"> Volume to play the event at </param>
		public static void PlayOneShot(string sound_event, float volume = 1) {
			if (sound_event == "") {
				Debug.LogWarning("ModAudioManager.PlayOneShot(string, float): Tried to pass an empty string as event name");
				return;
			}

			if (sound_event.Substring(0, "event:/".Length) ==  "event:/") {
				AudioManager.PlayOneShot(sound_event, volume);
				return;
			}

			if (!customEvents.ContainsKey(sound_event)) {
				Debug.LogError("ModAudioManager.PlayOneShot(string, float): Tried to play an event that doesn't exist: \"" + sound_event + "\"\nDid you load it using ModAudioManager.LoadCustomEvents(string, string); ?");
				return;
			}

			EventInstance instance = RuntimeManager.CreateInstance(TAPE_EVENT);

			SoundAsset soundEvent = customEvents[sound_event];

			IntPtr userData = GCHandle.ToIntPtr(GCHandle.Alloc(new CustomEventInstanceUserData { sound_handle = soundEvent.sound.handle }, GCHandleType.Pinned));

			instance.setUserData(userData);

			instance.setCallback(TAPE_CALLBACK, EVENT_CALLBACK_TYPE.ALL);

			instance.setVolume(volume);

			ModdingKitCorePlugin.instance.StartCoroutine(PlayCustomEvent(instance, soundEvent.length));
		}

		/// <summary>
		/// Play the specified event at specified position and then release it
		/// </summary>
		/// <param name="sound_event"> Event to play, custom events start with "custom:/" </param>
		/// <param name="pos"> Position the event should appear at </param>
		/// <param name="volume"> Volume to play the event at </param>
		/// <param name="pitch"> Pitch to play the event at </param>
		public static void PlayOneShot3D(string sound_event, Vector3 pos, float volume = 1f, float pitch = 1f) {
			if (sound_event == "") {
				Debug.LogWarning("ModAudioManager.PlayOneShot3D(string, Vector3, float, float): Tried to pass an empty string as event name");
				return;
			}

			if (sound_event.Substring(0, "event:/".Length) ==  "event:/") {
				AudioManager.PlayOneShot3D(sound_event, pos, volume, pitch);
				return;
			}

			if (!customEvents.ContainsKey(sound_event)) {
				Debug.LogError("ModAudioManager.PlayOneShot3D(string, Vector3, float, float): Tried to play an event that doesn't exist: \"" + sound_event + "\"\nDid you load it using ModAudioManager.LoadCustomEvents(string, string); ?");
				return;
			}

			EventInstance instance = RuntimeManager.CreateInstance(TAPE_EVENT);

			SoundAsset soundEvent = customEvents[sound_event];

			IntPtr userData = GCHandle.ToIntPtr(GCHandle.Alloc(new CustomEventInstanceUserData { sound_handle = soundEvent.sound.handle }, GCHandleType.Pinned));

			instance.setUserData(userData);

			instance.setCallback(TAPE_CALLBACK, EVENT_CALLBACK_TYPE.ALL);

			instance.set3DAttributes(pos.To3DAttributes());

			instance.setVolume(volume);

			instance.setPitch(pitch);

			ModdingKitCorePlugin.instance.StartCoroutine(PlayCustomEvent(instance, soundEvent.length));
		}

		/// <summary>
		/// Play the specified event attached to the specified GameObject and then release it
		/// </summary>
		/// <param name="sound_event"> Event to play, custom events start with "custom:/" </param>
		/// <param name="obj"> Game object the sound should be attached to </param>
		public static void PlayOneShotAttached(string sound_event, GameObject obj) {
			if (sound_event == "") {
				Debug.LogWarning("ModAudioManager.PlayOneShotAttached(string, GameObject): Tried to pass an empty string as event name");
				return;
			}

			if (sound_event.Substring(0, "event:/".Length) ==  "event:/") {
				AudioManager.PlayOneShotAttached(sound_event, obj);
				return;
			}

			if (!customEvents.ContainsKey(sound_event)) {
				Debug.LogError("ModAudioManager.PlayOneShotAttached(string, GameObject): Tried to play an event that doesn't exist: \"" + sound_event + "\"\nDid you load it using ModAudioManager.LoadCustomEvents(string, string); ?");
				return;
			}

			EventInstance instance = RuntimeManager.CreateInstance(TAPE_EVENT);

			SoundAsset soundEvent = customEvents[sound_event];

			IntPtr userData = GCHandle.ToIntPtr(GCHandle.Alloc(new CustomEventInstanceUserData { sound_handle = soundEvent.sound.handle }, GCHandleType.Pinned));

			instance.setUserData(userData);

			instance.setCallback(TAPE_CALLBACK, EVENT_CALLBACK_TYPE.ALL);

			RuntimeManager.AttachInstanceToGameObject(instance, obj.transform, obj.GetComponent<Rigidbody>());

			ModdingKitCorePlugin.instance.StartCoroutine(PlayCustomEvent(instance, soundEvent.length));
		}

		/// <summary>
		/// Play the specified event attached to the specified GameObject and then release it
		/// </summary>
		/// <param name="sound_event"> Event to play, custom events start with "custom:/" </param>
		/// <param name="obj"> Game object the sound should be attached to </param>
		/// <param name="volume"> Volume to play the event at </param>
		public static void PlayOneShotAttached(string sound_event, GameObject obj, float volume) {
			if (sound_event == "") {
				Debug.LogWarning("ModAudioManager.PlayOneShotAttached(string, GameObject, float): Tried to pass an empty string as event name");
				return;
			}

			if (sound_event.Substring(0, "event:/".Length) ==  "event:/") {
				AudioManager.PlayOneShotAttached(sound_event, obj, volume);
				return;
			}

			if (!customEvents.ContainsKey(sound_event)) {
				Debug.LogError("ModAudioManager.PlayOneShotAttached(string, GameObject, float): Tried to play an event that doesn't exist: \"" + sound_event + "\"\nDid you load it using ModAudioManager.LoadCustomEvents(string, string); ?");
				return;
			}

			EventInstance instance = RuntimeManager.CreateInstance(TAPE_EVENT);

			SoundAsset soundEvent = customEvents[sound_event];

			IntPtr userData = GCHandle.ToIntPtr(GCHandle.Alloc(new CustomEventInstanceUserData { sound_handle = soundEvent.sound.handle }, GCHandleType.Pinned));

			instance.setUserData(userData);

			instance.setCallback(TAPE_CALLBACK, EVENT_CALLBACK_TYPE.ALL);

			instance.setVolume(volume);

			RuntimeManager.AttachInstanceToGameObject(instance, obj.transform, obj.GetComponent<Rigidbody>());

			ModdingKitCorePlugin.instance.StartCoroutine(PlayCustomEvent(instance, soundEvent.length));
		}


		//Patches
		/////////

		[HarmonyPatch(typeof(AudioManager), nameof(AudioManager.PlayOneShot))]
		[HarmonyPrefix]
		private static bool PatchPlayOneShot(string sound_event, float volume) {
			if (sound_event == "") {
				Debug.LogWarning("AudioManager.PlayOneShot(string, float): Tried to pass an empty string as event name");
				return false;
			}

			if (sound_event.Substring(0, "custom:/".Length) == "custom:/") {
				PlayOneShot(sound_event, volume);
				return false;
			}    

			return true;
		}

		[HarmonyPatch(typeof(AudioManager), nameof(AudioManager.PlayOneShot3D), new Type[] {typeof(string), typeof(Vector3), typeof(float), typeof(float)})]
		[HarmonyPrefix]
		private static bool PatchPlayOneShot3D(string sound_event, Vector3 pos, float volume = 1f, float pitch = 1f) {
			if (sound_event == "") {
				Debug.LogWarning("AudioManager.PlayOneShot3D(string, Vector3, float, float): Tried to pass an empty string as event name");
				return false;
			}

			if (sound_event.Substring(0, "custom:/".Length) == "custom:/") {
				PlayOneShot3D(sound_event, pos, volume, pitch);
				return false;
			}    

			return true;
		}

		[HarmonyPatch(typeof(AudioManager), nameof(AudioManager.PlayOneShotAttached), new Type[] {typeof(string), typeof(GameObject)})]
		[HarmonyPrefix]
		private static bool PatchPlayOneShotAttached(string sound_event, GameObject obj) {
			if (sound_event == "") {
				Debug.LogWarning("AudioManager.PlayOneShotAttached(string, GameObject): Tried to pass an empty string as event name");
				return false;
			}

			if (sound_event.Substring(0, "custom:/".Length) == "custom:/") {
				PlayOneShotAttached(sound_event, obj);
				return false;
			}    

			return true;
		}

		[HarmonyPatch(typeof(AudioManager), nameof(AudioManager.PlayOneShotAttached), new Type[] {typeof(string), typeof(GameObject), typeof(float)})]
		[HarmonyPrefix]
		private static bool PatchPlayOneShotAttached(string sound_event, GameObject obj, float volume) {
			if (sound_event == "") {
				Debug.LogWarning("AudioManager.PlayOneShotAttached(string, GameObject, float): Tried to pass an empty string as event name");
				return false;
			}

			if (sound_event.Substring(0, "custom:/".Length) == "custom:/") {
				PlayOneShotAttached(sound_event, obj, volume);
				return false;
			}    

			return true;
		}
	}
}
