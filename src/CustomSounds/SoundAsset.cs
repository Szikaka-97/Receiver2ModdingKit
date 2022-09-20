using FMODUnity;
using FMOD;

namespace Receiver2ModdingKit.CustomSounds {
    /// <summary>
    /// Class containing asset name, sound related to it and its length. Used for preloading data
    /// </summary>
    public class SoundAsset {
        public string asset_name;
        public Sound sound;
        public uint length;

        public SoundAsset(string asset_name, Sound sound) {
            this.asset_name = asset_name;

            this.sound = sound;

            sound.getLength(out this.length, TIMEUNIT.MS);
        }

        public SoundAsset(string asset_name, string asset_file) {
            this.asset_name = asset_name;

            RuntimeManager.CoreSystem.createSound(asset_file, MODE.DEFAULT, out this.sound);

            sound.getLength(out this.length, TIMEUNIT.MS);
        }
    }
}
