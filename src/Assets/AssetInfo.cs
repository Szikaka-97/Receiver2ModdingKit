using Receiver2ModdingKit.Assets.HeaderStructs;

namespace Receiver2ModdingKit.Assets {
	public class AssetInfo {
		public ObjectStruct backing_object;
		public TypeStruct backing_type;
		public long absolute_start;

		public string name;
		public uint total_size;
		public AssetInfo[] dependencies;
		public AssetDataReference[] references;

		public void AddReferences(params AssetDataReference[] references_to_add) {
			if (this.references == null) {
				this.references = references_to_add;

				for (int i = 0; i < this.references.Length; i++) {
					this.total_size += (uint) references_to_add[i].size;
				}

				return;
			}

			var new_references = new AssetDataReference[this.references.Length + references_to_add.Length];

			for (int i = 0; i < this.references.Length; i++) {
				new_references[i] = this.references[i];
			}

			for (int i = 0; i < references_to_add.Length; i++) {
				new_references[this.references.Length + i] = references_to_add[i];

				this.total_size += (uint) references_to_add[i].size;
			}

			this.references = new_references;
		}
	}
}