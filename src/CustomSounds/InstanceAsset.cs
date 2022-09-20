using System.Runtime.InteropServices;
using System;
using FMOD.Studio;
using FMODUnity;

namespace Receiver2ModdingKit.CustomSounds {
	/// <summary>
	/// Wrapper class to provide both the modded EventInstance and its length
	/// </summary>
	public class InstanceAsset {
		public EventInstance instance;
		public uint length;

		/// <summary>
		/// Create an InstanceAsset from an FMOD EventInstance with provided length
		/// </summary>
		/// <param name="instance"> EventInstance for InstanceAsset to wrap around </param>
		/// <param name="length"> Length of provided EventInstance </param>
		public InstanceAsset(EventInstance instance, uint length) {
			this.instance = instance;
			this.length = length;
		}

		/// <summary>
		/// Create an InstanceAsset from a SoundAsset
		/// </summary>
		/// <param name="asset"> SoundAsset containing information about InstanceAsset </param>
		public InstanceAsset(SoundAsset asset) {
			this.instance = RuntimeManager.CreateInstance(ModAudioManager.TAPE_EVENT);

			IntPtr userData = GCHandle.ToIntPtr(GCHandle.Alloc(new ModAudioManager.CustomEventInstanceUserData { sound_handle = asset.sound.handle }, GCHandleType.Pinned));

			this.instance.setUserData(userData);

			this.instance.setCallback(ModAudioManager.TAPE_CALLBACK, EVENT_CALLBACK_TYPE.ALL);

			this.length = asset.length;
		}
	}
}
