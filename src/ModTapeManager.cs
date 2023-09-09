using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using ImGuiNET;
using Receiver2;

namespace Receiver2ModdingKit {
	internal class ModTapeManager : MonoBehaviour {
		private static List<ModTape> tapes = new List<ModTape>();
		private static GameObject instantiate_tape_button_prefab;
		private static Sprite instantiate_tape_sprite;

		private static TapesMenuScript tapes_menu;

		internal static bool tapes_debug_window_open;

		private static Dictionary<string, FileInfo> tape_subtitles {
			get;
		} = new Dictionary<string, FileInfo>();

		internal static void Init() {
			instantiate_tape_button_prefab = Instantiate(GameObject.Find("ReceiverCore/Menus/Overlay Menu Canvas/Aspect Ratio Fitter/New Pause Menu/Backdrop1/Sub-Menu Layout Group/New Tape Menu/Entries Layout/ScrollableContent Variant/Viewport/Content/Standard/Invalid/Secondary Button"));
			
			using (var image_stream = ModdingKitCorePlugin.GetResourceStream("tape.png")) {
				if (image_stream.Length > 0) {
					byte[] data = new byte[image_stream.Length];

					image_stream.Read(data, 0, (int) image_stream.Length);

					var temp_texture = new Texture2D(64, 64);
					temp_texture.LoadImage(data);

					instantiate_tape_sprite = Sprite.Create(temp_texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
				}
				else {
					instantiate_tape_sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
				}
			}

			instantiate_tape_button_prefab.name = "Instantiate Tape ";
			instantiate_tape_button_prefab.transform.localScale = Vector3.one;
			instantiate_tape_button_prefab.transform.Find("Image").GetComponent<Image>().sprite = instantiate_tape_sprite;
			instantiate_tape_button_prefab.transform.Find("Image").localScale = new Vector3(1.2f, 1.2f, 1.2f);
			instantiate_tape_button_prefab.SetActive(true);
		}

		internal static void CreateModTapes(TapesMenuScript tapes_menu) {
			foreach(var mod_tape in ReceiverCoreScript.Instance().tape_loadout_asset.GetModTapes()) {
				var entry_category = tapes_menu.category_string_id_dict[mod_tape.tape_id_string];

				var tape_entry = entry_category.entries.First( entry => entry.string_id == mod_tape.tape_id_string );

				tape_entry.gameObject.name = mod_tape.name;

				var tertiary_button = Object.Instantiate(instantiate_tape_button_prefab);

				tertiary_button.transform.SetParent(tape_entry.transform);
				tertiary_button.transform.localScale = Vector3.one;
				tertiary_button.GetComponent<RectTransform>().SetSiblingIndex(1);

				tertiary_button.GetComponent<SelectableButton>().onClick.AddListener(delegate () {
					TapeScript instantiated_tape = TapeManager.InstantiateTape(tape_entry.string_id, null);

					if (LocalAimHandler.player_instance != null) {
						instantiated_tape.transform.position = LocalAimHandler.player_instance.transform.position + new Vector3(0, 0.5f, 0);

						instantiated_tape.transform.rotation = Random.rotation;
					}

					instantiated_tape.AddRigidBody();
				});

				ModTape mod_tape_data = new ModTape(
					mod_tape,
					tertiary_button,
					tape_entry
				);

				tapes.Add(mod_tape_data);
			}
		}

		internal static void RegisterSubtitles(string tape_id, FileInfo tape_subtitles_file) {
			tape_subtitles.Add(tape_id, tape_subtitles_file);
		}

		internal static void TryReplaceSubtitles(string tape_id, ref TapeSubtitles subtitles) {
			if (tape_subtitles.ContainsKey(tape_id)) {

				FileInfo subtitle_file = tape_subtitles[tape_id];

				if (subtitle_file.Exists) {
					using (StreamReader streamReader = new StreamReader(subtitle_file.FullName)) {
						subtitles.tape_path = subtitle_file.FullName;
						subtitles.tape_subtitle_string = streamReader.ReadToEnd();
					}
				}
			}
		}

		internal static void PrepareTapesForCompound() {
			foreach (var mod_tape in tapes) {
				mod_tape.SetEntryCompound();
			}
		}

		internal static void PrepareTapesForGame() {
			foreach (var mod_tape in tapes) {
				mod_tape.SetEntryNormal();
			}
		}

		internal static void SwitchMenuVisible() {
			tapes_debug_window_open = !tapes_debug_window_open;
		}

		private static void DrawTapesImGUI() {
			if (tapes_menu == null && GameObject.Find("ReceiverCore/Menus/Overlay Menu Canvas/Aspect Ratio Fitter/New Pause Menu/Backdrop1/Sub-Menu Layout Group/New Tape Menu") != null) tapes_menu = GameObject.Find("ReceiverCore/Menus/Overlay Menu Canvas/Aspect Ratio Fitter/New Pause Menu/Backdrop1/Sub-Menu Layout Group/New Tape Menu").GetComponent<TapesMenuScript>();

			if (tapes_menu != null && tapes_debug_window_open) {
				ImGui.SetNextWindowSize(new Vector2(240f, 600f), ImGuiCond.FirstUseEver);

				ImGuiWindowFlags window_flags = ImGuiWindowFlags.None;
				if (!ReceiverGlobals.window_interactive)
				{
					window_flags |= ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar;
					ImGui.SetNextWindowBgAlpha(0f);
					ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
				}

				if (ImGui.Begin("Unlocked Tapes", ref tapes_debug_window_open, window_flags)) {
					foreach (var tape_category in tapes_menu.categories) {
						ImGui.Separator();
						ImGui.Text(tape_category.category_id.ToString());
						ImGui.Separator();

						foreach (var entry in tape_category.entries) {
							bool unlocked = ReceiverCoreScript.Instance().PlayerData.picked_up_tape_ids_string.Contains(entry.string_id);

							if (ImGui.Checkbox(entry.string_id, ref unlocked)) {
								if (unlocked) { //Add tape
									ReceiverCoreScript.Instance().PlayerData.picked_up_tape_ids_string.Add(entry.string_id);
								}
								else {
									ReceiverCoreScript.Instance().PlayerData.picked_up_tape_ids_string.Remove(entry.string_id);
								}

								tapes_menu.OnEnable();
							}
						}
					}

					ImGui.End();
				}
			}
		}

		private void Update() {
			DrawTapesImGUI();
		}
	}
}
