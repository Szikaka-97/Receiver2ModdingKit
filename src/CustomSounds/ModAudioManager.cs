using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using FMOD.Studio;
using FMODUnity;
using ImGuiNET;
using Receiver2;
using Receiver2ModdingKit.Editor;
using System.Linq;
using HarmonyLib;

namespace Receiver2ModdingKit.CustomSounds {

	public static class ModAudioManager {
		public static readonly EVENT_CALLBACK CUSTOM_SOUNDS_CALLBACK = new EVENT_CALLBACK( (EVENT_CALLBACK_TYPE type, EventInstance instance, IntPtr parameters) => { //Copied from the dll
			instance.getUserData(out IntPtr value);
			GCHandle gchandle = GCHandle.FromIntPtr(value);
			CustomEventInstanceUserData eventData = gchandle.Target as CustomEventInstanceUserData;
		
			if (type == EVENT_CALLBACK_TYPE.DESTROY_PROGRAMMER_SOUND) { //Programmer sound is destroyed, we need to finish playing and clean the data
				gchandle.Free();

				instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
			}
			else { //Programmer sound has begun, we need to set it up
				PROGRAMMER_SOUND_PROPERTIES properties = Marshal.PtrToStructure<PROGRAMMER_SOUND_PROPERTIES>(parameters);
				properties.sound = eventData.sound_handle;
				Marshal.StructureToPtr(properties, parameters, false);
			}

			return FMOD.RESULT.OK;
		});

		public static FMOD.Studio.System mod_system;

		public class CustomEventInstanceUserData { public IntPtr sound_handle; }

		private static Dictionary<string, SoundAsset> customEvents = new Dictionary<string, SoundAsset>();
		private static List<string> prefixes = new List<string>();
		private static Bus[] busList;

		/// <summary>
		/// Load the main sound bank and setup variables for modded audio to use
		/// </summary>
		public static void Initialize() {

			if (
				Utility.IsError(FMOD.Studio.System.create(out mod_system), "Create system")
				||
				Utility.IsError(mod_system.initialize(512, INITFLAGS.NORMAL, FMOD.INITFLAGS.NORMAL, IntPtr.Zero))
			) {
				//ToDo - Error message

				return;
			}

			FMOD.Studio.Bank master_bank, master_strings_bank;

			using (var master_bank_strean = Assembly.GetExecutingAssembly().GetManifestResourceStream("Receiver2ModdingKit.resources.MainModBank.bank")) {
				var master_bank_bytes = new byte[master_bank_strean.Length];

				master_bank_strean.Read(master_bank_bytes, 0, master_bank_bytes.Length);

				if (
					Utility.IsError(mod_system.loadBankMemory(master_bank_bytes, LOAD_BANK_FLAGS.UNENCRYPTED, out master_bank), "FMOD load master bank")
					||
					Utility.IsError(master_bank.getBusList(out busList), "FMOD Get Bus List")
				) {
					return;
				}
			}
			using (var strings_bank_stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Receiver2ModdingKit.resources.MainModBank.strings.bank")) {
				var strings_bank_bytes = new byte[strings_bank_stream.Length];

				strings_bank_stream.Read(strings_bank_bytes, 0, strings_bank_bytes.Length);

				if (Utility.IsError(mod_system.loadBankMemory(strings_bank_bytes, LOAD_BANK_FLAGS.UNENCRYPTED, out master_strings_bank), "FMOD load strings bank")) {
					return;
				}
			}
		}


		/// <summary>
		/// Try to load banks from the provided lists, returning the banks that loaded properly
		/// </summary>
		/// <param name="bank_lists"> Lists of banks to be loaded </param>
		/// <returns> List of banks that loaded properly, or null if none did </returns>
		public static Bank[] LoadBanksFromLists(params BankList[] bank_lists) {
			if (!mod_system.isValid()) {
				Debug.LogError("ModAudioManager: Tried to load banks before the mod audio system was initialized");

				return null;
			}

			List<Bank> loaded_banks = new List<Bank>();

			foreach (BankList list in bank_lists) {
				List<byte[]> bank_data_list = list.GetBanks();

				if (bank_data_list.Count == 0) {
					Debug.LogError("ModAudioManager: Bank list " + list.name + " contains no banks");
				}

				int index = 0;

				foreach(var bank_data in bank_data_list) {
					index++;

					if (
						Utility.IsError(mod_system.loadBankMemory(bank_data, LOAD_BANK_FLAGS.NORMAL, out Bank bank))
						||
						Utility.IsError(bank.loadSampleData())
					) {
						Debug.LogError("ModAudioManager: Bank at index " + index + " from bank list " + list.name + " is invalid and will not be loaded");

						if (bank.isValid()) bank.unload();

						continue;
					}

					if (bank.isValid()) loaded_banks.Add(bank);
				}
			}

			if (loaded_banks.Count == 0) return null;
			return loaded_banks.ToArray();
		}

		/// <summary>
		/// Sync the modded audio system with ingame one and update it
		/// </summary>
		public static void Update() {
			if (!mod_system.isValid()) return;

			foreach (Bus modBus in busList) {
				if (
					Utility.IsError(modBus.getPath(out string path), "Get Mod Bus Path " + path)
					||
					Utility.IsError(RuntimeManager.StudioSystem.getBus(path, out Bus game_bus), "Get Game Bus named " + path)
					||
					Utility.IsError(game_bus.getVolume(out float volume), "Get Volume of Bus named " + path)
					||
					Utility.IsError(modBus.setVolume(volume), "Set Volume of Bus named " + path)
					||
					Utility.IsError(game_bus.getPaused(out bool paused), "Get Paused Status of Bus named " + path)
					||
					Utility.IsError(modBus.setPaused(paused), "Set Paused Status of Bus named " + path)
				) {
					continue;
				}
			}

			if (Utility.IsError(RuntimeManager.StudioSystem.getNumListeners(out int num_listeners))) {
				return;
			}

			for (int listener = 0; listener < num_listeners; listener++) {
				RuntimeManager.StudioSystem.getListenerAttributes(listener, out var attributes);
				mod_system.setListenerAttributes(listener, attributes);
			}

			mod_system.update();
		}

		/// <summary>
		/// Free the resources needed for the modded sounds to work, only use it if the game is ending or you want to call ModAudioManager.Initialize() immediately after
		/// </summary>
		public static void Release() {
			mod_system.release();
		}

		internal static void DrawImGUIDebug() {
			ImGui.Separator();

			ImGui.Text("Modded sounds debug:");

			List<string> active_instances = new List<string>();

			if (Utility.IsError(mod_system.getBankList(out Bank[] bank_list))) return;

			foreach (Bank bank in bank_list) {
				if (Utility.CheckResult(bank.getEventList(out var event_list))) {
					foreach (EventDescription event_description in event_list) {
						if (Utility.CheckResult(event_description.getInstanceList(out var instance_list))) {
							instance_list.Do( inst => { 
								if (Utility.CheckResult(event_description.getPath(out string event_path)))
									active_instances.Add(event_path);
							} );
						}
					}
				}
			}

			if (ImGui.TreeNode("Active Instances: " + active_instances.Count)) {
				foreach (string instance_path in active_instances) {
					ImGui.Text(instance_path);
				}

				ImGui.TreePop();
			}

			foreach (Bank bank in bank_list) {
				if (Utility.CheckResult(bank.getPath(out var bank_path)) && ImGui.TreeNode(bank_path)) {
					ImGui.Text("Events:");

					bank.getEventList(out var event_list);

					foreach (var event_desc in event_list) {
						if (Utility.CheckResult(event_desc.getPath(out string path))) ImGui.Text(path);
					}

					ImGui.TreePop();
				}
			}
		}

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

			SoundAsset soundEvent = customEvents[sound_event];

			EventInstance instance = RuntimeManager.CreateInstance(soundEvent.type.getEventPath());

			IntPtr userData = GCHandle.ToIntPtr(GCHandle.Alloc(new CustomEventInstanceUserData { sound_handle = soundEvent.sound.handle }, GCHandleType.Pinned));

			instance.setUserData(userData);

			instance.setCallback(CUSTOM_SOUNDS_CALLBACK, EVENT_CALLBACK_TYPE.CREATE_PROGRAMMER_SOUND | EVENT_CALLBACK_TYPE.DESTROY_PROGRAMMER_SOUND);

			instance.setVolume(volume);

			instance.setPaused(false);
			instance.start();
			instance.release();
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

			SoundAsset soundEvent = customEvents[sound_event];

			EventInstance instance = RuntimeManager.CreateInstance(soundEvent.type.getEventPath());

			IntPtr userData = GCHandle.ToIntPtr(GCHandle.Alloc(new CustomEventInstanceUserData { sound_handle = soundEvent.sound.handle }, GCHandleType.Pinned));

			instance.setUserData(userData);

			instance.setCallback(CUSTOM_SOUNDS_CALLBACK, EVENT_CALLBACK_TYPE.CREATE_PROGRAMMER_SOUND | EVENT_CALLBACK_TYPE.DESTROY_PROGRAMMER_SOUND);

			instance.set3DAttributes(pos.To3DAttributes());

			instance.setVolume(volume);

			instance.setPitch(pitch);

			instance.setPaused(false);
			instance.start();
			instance.release();
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

			SoundAsset soundEvent = customEvents[sound_event];

			EventInstance instance = RuntimeManager.CreateInstance(soundEvent.type.getEventPath());

			IntPtr userData = GCHandle.ToIntPtr(GCHandle.Alloc(new CustomEventInstanceUserData { sound_handle = soundEvent.sound.handle }, GCHandleType.Pinned));

			instance.setUserData(userData);

			instance.setCallback(CUSTOM_SOUNDS_CALLBACK, EVENT_CALLBACK_TYPE.CREATE_PROGRAMMER_SOUND | EVENT_CALLBACK_TYPE.DESTROY_PROGRAMMER_SOUND);

			RuntimeManager.AttachInstanceToGameObject(instance, obj.transform, obj.GetComponent<Rigidbody>());

			instance.setPaused(false);
			instance.start();
			instance.release();
		}

        /// <summary>
        /// Play the specified event attached to the specified Transform's GameObject and then release it
        /// </summary>
        /// <param name="sound_event"> Event to play, custom events start with "custom:/" </param>
        /// <param name="transform"> Transform whose Game object the sound should be attached to </param>
        public static void PlayOneShotAttached(string sound_event, Transform transform)
        {
            PlayOneShotAttached(sound_event, transform.gameObject);
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

			SoundAsset soundEvent = customEvents[sound_event];

			EventInstance instance = RuntimeManager.CreateInstance(soundEvent.type.getEventPath());

			IntPtr userData = GCHandle.ToIntPtr(GCHandle.Alloc(new CustomEventInstanceUserData { sound_handle = soundEvent.sound.handle }, GCHandleType.Pinned));

			instance.setUserData(userData);

			instance.setCallback(CUSTOM_SOUNDS_CALLBACK, EVENT_CALLBACK_TYPE.CREATE_PROGRAMMER_SOUND | EVENT_CALLBACK_TYPE.DESTROY_PROGRAMMER_SOUND);

			instance.setVolume(volume);

			RuntimeManager.AttachInstanceToGameObject(instance, obj.transform, obj.GetComponent<Rigidbody>());

			instance.setPaused(false);
			instance.start();
			instance.release();
        }       

		/// <summary>
        /// Play the specified event attached to the specified Transform's GameObject and then release it
        /// </summary>
        /// <param name="sound_event"> Event to play, custom events start with "custom:/" </param>
        /// <param name="transform"> Transform whose Game object the sound should be attached to </param>
        /// <param name="volume"> Volume to play the event at </param>
        public static void PlayOneShotAttached(string sound_event, Transform transform, float volume)
        {
            PlayOneShotAttached(sound_event, transform.gameObject, volume);
        }
    }
}
