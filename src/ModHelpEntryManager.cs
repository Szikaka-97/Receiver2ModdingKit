using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Receiver2;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Receiver2ModdingKit {
	internal class ModHelpEntryManager : MonoBehaviour {
		public static Dictionary<string, ModHelpEntry> entries = new Dictionary<string, ModHelpEntry>();

		private static Delegate OnEntryClick;
		private delegate void D2(string s); //I'm going to shoot the person who documented delegates for Microsoft
		private static HelpMenuScript m_help_menu;
		private static Sprite secondary_button_sprite;
		private static readonly int IMAGE_SIZE = 4501; // Cog sprite has a known size, makes loading the texture easier

		private static HelpMenuScript help_menu {
			get {
				if (!m_help_menu) {
					m_help_menu = FindObjectOfType<HelpMenuScript>();

					OnEntryClick = Delegate.CreateDelegate(
						typeof(D2),
						m_help_menu,
						typeof(EntryDescriptionMenuScript)
							.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
							.FirstOrDefault( x => x.Name == "OnEntryClick" && x.GetParameters()[0].ParameterType == typeof(string) )
					);
				}
				return m_help_menu;
			}	
		}

		void Awake() {
			using (var image_stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Receiver2ModdingKit.resources.cog.png")) {
				byte[] data = new byte[IMAGE_SIZE];

				image_stream.Read(data, 0, IMAGE_SIZE);

				var temp_texture = new Texture2D(64, 64);
				temp_texture.LoadImage(data);

				secondary_button_sprite = Sprite.Create(temp_texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
			}
		}

		#region Patches

		[HarmonyPatch(typeof(HelpMenuScript), "CreateMenuEntries")]
		[HarmonyPrefix]
		private static void PatchHelpCreateMenuEntries(HelpMenuScript __instance) {

			foreach (var mod_entry in entries.Values) {
				if (!Locale.active_locale_help_menu_entries_string.ContainsKey(mod_entry.id)) {
					var entry = new LocaleHelpMenuEntry();
					entry.title = mod_entry.title;
					entry.description = mod_entry.description;
					entry.id_string = mod_entry.id;
					entry.name = mod_entry.id;

					Locale.active_locale_help_menu_entries_string.Add(mod_entry.id, entry);
				}

				EntryDescriptionMenuScript.Category gun_help = __instance.categories[4];

				if (!gun_help.entry_string_id_dict.ContainsKey(mod_entry.id)) {
					GameObject entry_button = Instantiate(help_menu.ecsPrefab);
					entry_button.name = mod_entry.id;
					entry_button.transform.SetParent(gun_help.root_object.transform, true);
					entry_button.transform.Find("Button").localPosition += (Vector3.left * 37);
					RectTransform button_transform = entry_button.GetComponent<RectTransform>();
					button_transform.localScale = new Vector3(1f, 1f, 1f);
					EntryComponentScript entry_script = entry_button.GetComponent<EntryComponentScript>();
					entry_script.string_id = mod_entry.id;
					entry_script.title.text = mod_entry.id;
					entry_script.button.onClick.AddListener( delegate {
						OnEntryClick.DynamicInvoke(mod_entry.id);
					});
					entry_script.default_enabled = true;
					if (mod_entry.info_sprite != null) {
						entry_script.media.sprite_index = help_menu.sprites.Count();
						help_menu.sprites.Add(mod_entry.info_sprite);
					}
					gun_help.entries.Add(entry_script);
					gun_help.entry_string_id_dict.Add(mod_entry.id, entry_script);
					__instance.category_string_id_dict.Add(mod_entry.id, gun_help);

					if (mod_entry.settings_button_active) {
						GameObject secondary_button = Instantiate(GameObject.Find("ReceiverCore/Menus/Overlay Menu Canvas/Aspect Ratio Fitter/New Pause Menu/Backdrop1/Sub-Menu Layout Group/New Help Menu/Entries Layout/ScrollableContent Variant/Viewport/Content/Guns/StovePipe/Secondary Button"));
						secondary_button.transform.SetParent(entry_button.transform);
						secondary_button.transform.localScale = Vector3.one;
						secondary_button.transform.localPosition = (Vector3.right * 236.2f); // Unity position shenanigans
						secondary_button.SetActive(true);

						entry_script.secondary_button = secondary_button.GetComponent<SelectableButton>();
						entry_script.secondary_button.onClick.AddListener(delegate { AudioManager.PlayOneShot("event:/UI/ui_butt_sett_key"); });

						entry_script.secondary_button.transform.Find("Image").GetComponent<Image>().sprite = secondary_button_sprite;
					}

					mod_entry.SetEntryComponent(entry_script);
				}
			}
		}

		[HarmonyPatch(typeof(HelpMenuScript), "OnLoad")]
		[HarmonyPostfix]
		private static void PatchHelpOnLoad(HelpMenuScript __instance) {
			EntryDescriptionMenuScript.Category gun_help = __instance.categories[4];

			int active = gun_help.entry_string_id_dict.Values.Count(e => e.isActiveAndEnabled);
			int hidden = gun_help.entry_string_id_dict.Values.Count(e => e.hidden);

			if (Locale.active_locale_category_string.TryGetValue(gun_help.category_id, out string title)) {
				gun_help.title.text = string.Format("{0} - {1}/{2}", title, active, gun_help.entries.Count - hidden);
			}
		}

		#endregion
	}
}
