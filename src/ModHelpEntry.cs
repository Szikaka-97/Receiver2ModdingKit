using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Receiver2;

namespace Receiver2ModdingKit {
	public class ModHelpEntry {
		/// <summary>
		/// Sprite to display on the top of the help menu entry
		/// </summary>
		public Sprite info_sprite = null;

		/// <summary>
		/// Title of the entry, displayed in big, bold letters
		/// </summary>
		public string title;

		/// <summary>
		/// All the text describing the gun, HTML style selectors are allowed
		/// </summary>
		public string description;

		public string id {
			get;
			private set;
		}

		internal bool settings_button_active;

		private EntryComponentScript m_entry_component;
		private List<UnityAction> action_queue = new List<UnityAction>();

		/// <summary>
		/// Create a new help entry objects tied to an id. Additional options can be provided using curly brackets
		/// </summary>
		/// <param name="id"> ID of the help entry, displayed on the button. </param>
		public ModHelpEntry(string id) {
			this.id = id;
		}

		public EntryComponentScript GetEntryComponent() { return m_entry_component; }
		internal void SetEntryComponent(EntryComponentScript entry) {
			m_entry_component = entry;

			if (action_queue.Count > 0) {
				foreach (var action in action_queue) 
					m_entry_component.secondary_button.onClick.AddListener(action);
			}
		}

		/// <summary>
		/// Start displaying a help menu button and add an action to be executed when it's pressed
		/// </summary>
		/// <param name="action"> Function to be called when the button is pressed </param>
		public void AddSettingsButtonAction(UnityAction action) {
			this.settings_button_active = true;
			if (m_entry_component != null) m_entry_component.secondary_button.onClick.AddListener(action);
			else action_queue.Add(action);
		}
	}
}
