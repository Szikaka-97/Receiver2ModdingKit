using Receiver2;

namespace Receiver2ModdingKit.Gamemodes {
	public abstract class ModGameModeBase : GameModeBase {
		// Move to DummyGameMode
		public static readonly GameMode ModGameMode = (GameMode) 5;

		public override GameMode GetGameMode() {
			return ModGameMode;
		}

		public abstract string GameModeName { get; }

		public abstract string SceneName { get; }

		public abstract string SceneAssetBundleName { get; }

		public virtual GameModeMusicSpec GetMusicParams() {
			return new GameModeMusicSpec();
		}
	}
}