using System.Collections.Generic;
using Receiver2;
using TMPro;
using UnityEngine;

namespace Receiver2ModdingKit.CustomRounds {
	public class ShootingRangeAmmoBoxCustomRoundExtender : MonoBehaviour {
		private PressableButton shootingRangeButton;

		private ShootingRangeAmmoBoxScript shootingRangeAmmoBoxScript;

		private readonly static Dictionary<CartridgeSpec.Preset, string> cartridgeNamesLookUp = new Dictionary<CartridgeSpec.Preset, string>()
		{
			{CartridgeSpec.Preset._9mm, "9mm"},
			{CartridgeSpec.Preset._38_special, ".38"},
			{CartridgeSpec.Preset._45_acp, ".45 ACP"},
			{CartridgeSpec.Preset._45_LC, ".45 LC"},
			{CartridgeSpec.Preset._50_AE, ".50"}
		};

		private void Awake() {
			shootingRangeButton = (GetComponent<PressableButton>() == null) ? GetComponentInChildren<PressableButton>() : GetComponent<PressableButton>();

			shootingRangeAmmoBoxScript = GetComponent<ShootingRangeAmmoBoxScript>();
		}

		private void Update() {
			if (shootingRangeButton.IsHovered()) {
				if (Input.GetKeyDown(KeyCode.LeftArrow)) {
					var current_round = shootingRangeAmmoBoxScript.round_prefab.GetComponent<ShellCasingScript>();

					Debug.Log(current_round);

					var round_def = CustomRoundTypes.GetDefinitionForRound(current_round.cartridge_type);

					CartridgeSpec.Preset base_cartridge_type;

					if (round_def != null) {
						base_cartridge_type = round_def.baseVariant;
					}
					else
					{
						base_cartridge_type = current_round.cartridge_type;
					}

					Debug.Log($"round def: {round_def}");
					Debug.Log($"base: {base_cartridge_type}");
					
					var presets = CustomRoundTypes.GetSiblingCartridgeTypes(base_cartridge_type);

					//current round is vanilla
					if (presets.Length > 0 && round_def == null) {
						base_cartridge_type = presets[0];
						shootingRangeAmmoBoxScript.round_prefab = ModdingKitCorePlugin.GetRoundPrefab(presets[0]).gameObject;
					}

					for (int i = 0; i < presets.Length; i++) {
						Debug.Log(presets[i]);
						if (presets[i] == current_round.cartridge_type) {
							if (i + 1 == presets.Length) {
								shootingRangeAmmoBoxScript.round_prefab = ModdingKitCorePlugin.GetRoundPrefab(base_cartridge_type).gameObject;
							}
							else {
								base_cartridge_type = presets[i + 1];
								Debug.Log($"got type: {base_cartridge_type}");
								shootingRangeAmmoBoxScript.round_prefab = ModdingKitCorePlugin.GetRoundPrefab(base_cartridge_type).gameObject;
							}

							break;
						}
					}
					
					round_def = CustomRoundTypes.GetDefinitionForRound(base_cartridge_type);

					UpdateTapeText(round_def, base_cartridge_type);
				}

				if (Input.GetKeyDown(KeyCode.RightArrow)) {
					var current_round = shootingRangeAmmoBoxScript.round_prefab.GetComponent<ShellCasingScript>();

					var round_def = CustomRoundTypes.GetDefinitionForRound(current_round.cartridge_type);

					CartridgeSpec.Preset base_cartridge_type;

					if (round_def != null) {
						base_cartridge_type = round_def.baseVariant;
					}
					else
					{
						base_cartridge_type = current_round.cartridge_type;
					}
					
					var presets = CustomRoundTypes.GetSiblingCartridgeTypes(base_cartridge_type);

					//current round is vanilla
					if (presets.Length > 0 && round_def == null) {
						base_cartridge_type = presets[0];
						shootingRangeAmmoBoxScript.round_prefab = ModdingKitCorePlugin.GetRoundPrefab(presets[0]).gameObject;
					}

					for (int i = presets.Length - 1; i >= 0; i--) {
						if (presets[i] == current_round.cartridge_type) {
							if (i - 1 == -1) {
								shootingRangeAmmoBoxScript.round_prefab = ModdingKitCorePlugin.GetRoundPrefab(base_cartridge_type).gameObject;
							}
							else {
								base_cartridge_type = presets[i - 1];
								shootingRangeAmmoBoxScript.round_prefab = ModdingKitCorePlugin.GetRoundPrefab(base_cartridge_type).gameObject;
								Debug.Log($"got type: {base_cartridge_type}");
							}
							break;
						}
					}

					round_def = CustomRoundTypes.GetDefinitionForRound(base_cartridge_type);

					UpdateTapeText(round_def, base_cartridge_type);
				}
			}
		}

		void UpdateTapeText(CustomRoundDefinition round_def, CartridgeSpec.Preset cartridge_type) {
			foreach (Transform go in this.transform) {
				if (go.name.StartsWith("Tape")) {
					Debug.Log(go.name);

					if (!go.TryGetComponent<TextMeshPro>(out TextMeshPro text))
					{
						text = go.GetComponentInChildren<TextMeshPro>();
					}

					string title;

					if (round_def != null) {
						if (!string.IsNullOrWhiteSpace(round_def.clean_name)) {
							title = round_def.clean_name;
							Debug.Log("getting clean name");
						}
						else
						{
							var prefab = ModdingKitCorePlugin.GetRoundPrefab(cartridge_type);

							title = prefab.InternalName.Split('.')[prefab.InternalName.Split('.').Length - 1];

							title = "";

							foreach (var splitt in title.Split('_')) {
								title += char.ToUpperInvariant(splitt[0]) + splitt.Substring(1);
							}
						}
					}
					else
					{
						title = cartridgeNamesLookUp[cartridge_type];
					}

					text.text = title;

					text.ForceMeshUpdate();

					bool found_fitting_text = false;

					for (int tapeIndex = 7; tapeIndex > 0; tapeIndex--) {
						var tape = go.transform.Find("Tape_0" + tapeIndex);

						var text_fits = (Quaternion.AngleAxis(tape.eulerAngles.y, Vector3.up) * tape.GetComponent<MeshRenderer>().bounds.extents).z > text.bounds.extents.x + 0.02f;

						Debug.Log($"index: {tapeIndex} fits: {text_fits}");
						Debug.Log($"tape extents: {(Quaternion.AngleAxis(tape.eulerAngles.y, Vector3.up) * tape.GetComponent<MeshRenderer>().bounds.extents).z} vs text extents: {text.bounds.extents.x}");
						Debug.Log($"already found fitting tape: {found_fitting_text}");

						if (text_fits && !found_fitting_text)
						{
							found_fitting_text = true;
							tape.gameObject.SetActive(true);

							Debug.Log("setting active");
						}
						else
						{
							tape.gameObject.SetActive(false);

							Debug.Log("setting inactive");
						}

						if (tapeIndex == 1 && !found_fitting_text)
						{
							Debug.Log("didn't find any fitting tape, enable biggest & scaling");

							tape.gameObject.SetActive(true);

							var diff = text.bounds.extents.x / (Quaternion.AngleAxis(tape.eulerAngles.y, Vector3.up) * tape.GetComponent<MeshRenderer>().bounds.extents).z;

							tape.localScale = new Vector3(tape.localScale.x, tape.localScale.y, (tape.localScale.z * diff) + 0.02f);
						}
					}

					text.text = title;
				}
			}
		}
	}
}