using SimpleJSON;
using Receiver2;

namespace Receiver2ModdingKit.Gamemodes {
	public class DummyGameMode : GameModeBase {
		public override GameMode GetGameMode() {
			return ModGameModeBase.ModGameMode;
		}

		public override void StartLevel() {
			if (ModGameModeManager.CurrentGameMode != null) {
				ModGameModeManager.CurrentGameMode.StartLevel();
			}
		}

		public override void PostResetLevel() {
			if (ModGameModeManager.CurrentGameMode != null) {
				ModGameModeManager.CurrentGameMode.PostResetLevel();
			}
		}

		public override void Restart(bool complete_reset, bool reset_checkpoint) {
			if (ModGameModeManager.CurrentGameMode != null) {
				ModGameModeManager.CurrentGameMode.Restart(complete_reset, reset_checkpoint);
			}
		}

		public override JSONObject StoreCheckpoint(CheckpointTrigger trigger_name) {
			if (ModGameModeManager.CurrentGameMode != null) {
				return ModGameModeManager.CurrentGameMode.StoreCheckpoint(trigger_name);
			}
			return new JSONObject();
		}

		public override void LoadCheckpoint(JSONObject checkpoint_data) {
			if (ModGameModeManager.CurrentGameMode != null) {
				ModGameModeManager.CurrentGameMode.LoadCheckpoint(checkpoint_data);
			}
		}
	}
}