namespace Receiver2ModdingKit.CustomSounds {
	/// <summary>
	/// What bus should the sound reside on, it will control the volume slider and various mixing effects affecting its volume
	/// </summary>
	/// Remnants of an older prototype, might use it later if the need arises
	public enum SoundType {
		/// <summary> Event will use the root bus, only being affected by the Master Volume slider </summary>
		Root,
		/// <summary> Event will use the music bus, being affected by the Music Volume slider </summary>
		Music,
		/// <summary> Event will use the general sound bus, being affected by the SFX slider </summary>
		Sound,
		/// <summary> Event will use the external sound bus, being affected by the SFX slider as well as the tinnitus effect and hearing damage on VR </summary>
		SoundExternal,
		/// <summary> Event will use the external loud sound bus, being affected by the SFX slider as well as the tinnitus effect and hearing damage on VR </summary>
		SoundExternalLoud,
		/// <summary> Event will use the external loud sound bus currently reserved for the music head, being affected by the SFX slider as well as the hearing damage on VR </summary>
		SoundExternalLoudCubehead,
		/// <summary> Event will use the external loud sound bus currently reserved for the baloons, being affected by the SFX slider as well as the hearing damage on VR </summary>
		SoundExternalLoudBalloon,
		/// <summary> Event will use the external loud sound bus used for firing shots, being affected by the SFX slider as well as the hearing damage on VR </summary>
		SoundExternalLoudShots,
		/// <summary> Event will use the external quiet sound bus, being affected by the SFX slider as well as the tinnitus effect and hearing damage on VR </summary>
		SoundExternalQuiet,
		/// <summary> Event will use the external quiet sound bus used for player steps, being affected by the SFX slider as well as the tinnitus effect and hearing damage on VR </summary>
		SoundExternalQuietSteps,
		/// <summary> Event will use the UI sound bus, being affected by the SFX slider </summary>
		UI,
		/// <summary> Event will use the voice bus, being affected by the Voice Volume slider </summary>
		Voice,
		#if !RELEASE
		/// <summary> Various garbage useful in debugging </summary>
		Test
		#endif
	}

	public static class SoundTypeExtensions {
		private static string[] event_path_lookup = new string[] {
			"event:/Blank - Root",
			"event:/Blank - Music",
			"event:/Blank - Sound",
			"event:/Blank - SoundExternal",
			"event:/Blank - SoundExternalLoud",
			"event:/Blank - SoundExternalLoudCubehead",
			"event:/Blank - SoundExternalLoudBalloon",
			"event:/Blank - SoundExternalLoudShots",
			"event:/Blank - SoundExternalQuiet",
			"event:/Blank - SoundExternalQuietSteps",
			"event:/Blank - UI",
			"event:/Blank - Voice",
			"event:/Test"
		};

		public static string getEventPath(this SoundType type) {
			return event_path_lookup[(int) type];
		}
	}
}
