using FMODUnity;
using FMOD;

namespace Receiver2ModdingKit.CustomSounds {
    /// <summary>
    /// Class containing asset name, sound related to it, length and the bus it's sitting on. Used for preloading data
    /// </summary>
    public class SoundAsset {
        public string asset_name {
			get;
			private set;
		}
        public Sound sound {
			get;
			private set;
		}
        public uint length {
			get;
			private set;
		}
		public SoundType type {
			get;
			private set;
		}

        public SoundAsset(string asset_name, Sound sound, SoundType type = SoundType.Sound) {
            this.asset_name = asset_name;

            this.sound = sound;

            sound.getLength(out var soundLength, TIMEUNIT.MS);

			this.length = soundLength;

			this.type = type;
        }

        public SoundAsset(string asset_name, string asset_file, SoundType type = SoundType.Sound) {
            this.asset_name = asset_name;

            RuntimeManager.CoreSystem.createSound(asset_file, MODE.DEFAULT, out var createdSound);

			this.sound = createdSound;

            sound.getLength(out var soundLength, TIMEUNIT.MS);

			this.length = soundLength;

			this.type = type;
        }
    }
}
