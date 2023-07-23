using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Receiver2;
using System;

namespace Receiver2ModdingKit {
	[Serializable]
	public class ModHelpEntry {
		/// <summary>
		/// Should the help entry be generated
		/// </summary>
		[Tooltip("Should the help entry be generated")]
		public bool generate;

		/// <summary>
		/// Name of the help menu button
		/// </summary>
		[Tooltip("Name of the help menu button")]
		public string name;

		/// <summary>
		/// Sprite to display on the top of the help menu entry
		/// </summary>
		[Tooltip("Sprite to display on the top of the help menu entry")]
		public Sprite info_sprite = null;

		/// <summary>
		/// Title of the entry, displayed in big, bold letters
		/// </summary>
		[Tooltip("Title of the entry, displayed in big, bold letters")]
		public string title;

		/// <summary>
		/// All the text describing the gun, HTML style selectors are allowed
		/// </summary>
		[Tooltip("All the text describing the gun, HTML style selectors are allowed")]
		[TextArea]
		public string description;

		internal bool settings_button_active;

		private EntryComponentScript m_entry_component;
		private List<UnityAction> action_queue = new List<UnityAction>();

		/// <summary>
		/// Create a new help entry objects tied to an id. Additional options can be provided using curly brackets
		/// </summary>
		/// <param name="name"> Text displayed on the button. </param>
		public ModHelpEntry(string name) {
			this.name = name;
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
