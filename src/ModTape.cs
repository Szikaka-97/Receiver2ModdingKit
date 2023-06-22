using UnityEngine;
using Receiver2;

namespace Receiver2ModdingKit {
	public class ModTape {
		private readonly TapeContent tape_content;
		private readonly GameObject tertiary_button;
		private readonly EntryComponentScript entry;

		public ModTape(TapeContent tape_content, GameObject tertiary_button, EntryComponentScript entry) {
			this.tape_content = tape_content;
			this.tertiary_button = tertiary_button;
			this.entry = entry;
		}

		/// <summary>
		/// Add the tape spawning button
		/// </summary>
		public void SetEntryCompound() {
			entry.button.GetComponent<RectTransform>().anchoredPosition = new Vector2(-56.2f, 2f);
            entry.button.GetComponent<RectTransform>().sizeDelta = new Vector2(343f, 46f);

			tertiary_button.SetActive(true);
		}

		/// <summary>
		/// Remove the tape spawning button
		/// </summary>
		public void SetEntryNormal() {
			entry.button.GetComponent<RectTransform>().anchoredPosition = new Vector2(200, -23f);
            entry.button.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 46f);

			tertiary_button.SetActive(false);
		}
	}
}
