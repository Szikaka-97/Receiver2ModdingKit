using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace Receiver2ModdingKit.Editor {
	[CreateAssetMenu(fileName = "Custom Bank list", menuName = "Receiver 2 Modding/Custom Bank List")]
	public class BankList : ScriptableObject, INotifiedByAssetbundleBuild {
		public UnityEngine.Object[] banks;

		public List<byte> bank_data = new List<byte>();
		public List<int> offsets = new List<int>();

		public bool ready = true;

		public Queue<string> bank_load_queue = new Queue<string>();

        public async void LoadBanks() {
#if UNITY_EDITOR
#pragma warning disable CS1998
			ready = false;

			if (bank_load_queue.Count == 0) {
				ready = true;
				return;
			}

			string path = bank_load_queue.Dequeue();

			byte[] buffer = new byte[new FileInfo(path).Length];

			await File.OpenRead(path).ReadAsync(buffer, 0, (int) new FileInfo(path).Length);

			offsets.Add(bank_data.Count);

			bank_data.AddRange(buffer);

			LoadBanks();
#pragma warning restore CS1998
#endif
		}

		public void ReloadBanks() {
#if UNITY_EDITOR
			bank_data.Clear();
			offsets.Clear();

			foreach (var bank in banks) { 
				if (!bank) continue;

				string path = Path.Combine(new DirectoryInfo(Application.dataPath).Parent.FullName, AssetDatabase.GetAssetPath(bank));

				if (File.Exists(path)) { 
					bank_load_queue.Enqueue(path);
				}
			}
			LoadBanks();
#endif
		}

		public void ReloadBanksSynchronous() {
#if UNITY_EDITOR
			bank_data.Clear();
			offsets.Clear();

			foreach (var bank in banks) { 
				if (!bank) continue;

				string path = Path.Combine(new DirectoryInfo(Application.dataPath).Parent.FullName, AssetDatabase.GetAssetPath(bank));

				byte[] buffer = new byte[new FileInfo(path).Length];

				File.OpenRead(path).Read(buffer, 0, (int) new FileInfo(path).Length);

				offsets.Add(bank_data.Count);

				bank_data.AddRange(buffer);
			}
#endif
		}

		public List<byte[]> GetBanks() {
			List<byte[]> banks_data = new List<byte[]>();

			#if DEBUG
			Debug.Log("Trying to get banks from list " + this.name);
			Debug.Log("Banks count: " + offsets.Count);
			foreach (int o in offsets) {
				Debug.Log("Offset: " + o);
			}
			#endif

			for (int index = 0; index < offsets.Count; index++) {
				if (index == offsets.Count - 1) {
					byte[] current_bank_data = new byte[bank_data.Count - offsets[index]];

					current_bank_data = bank_data.GetRange(offsets[index], current_bank_data.Length).ToArray();

					banks_data.Add(current_bank_data);
				}
				else {
					byte[] current_bank_data = new byte[offsets[index + 1] - offsets[index]];

					current_bank_data = bank_data.GetRange(offsets[index], current_bank_data.Length).ToArray();

					banks_data.Add(current_bank_data);
				}
			}

			return banks_data;
		}

		public void PreAssetbundleBuild() {
			ReloadBanksSynchronous();
		}
	}
}