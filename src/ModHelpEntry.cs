using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Receiver2;

namespace Receiver2ModdingKit {
	public class ModHelpEntry {
		public Sprite info_sprite;
		public string title;
		public string description;
		public string id;

		internal bool settings_button_active;

		private EntryComponentScript m_entry_component;
		private List<UnityAction> action_queue = new List<UnityAction>();

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

		public void AddSettingsButtonAction(UnityAction action) {
			if (m_entry_component != null) m_entry_component.secondary_button.onClick.AddListener(action);
			else action_queue.Add(action);
		}
	}
}
