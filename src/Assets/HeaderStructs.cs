using System;
using UnityEngine;

namespace Receiver2ModdingKit.Assets {
	public static class HeaderStructs {
		public class TypeStruct : BinarySerializable {
			public int class_id;
			public bool is_stripped;
			public short script_type_index;
			public byte[] script_id;
			public byte[] hash;

			public TypeStruct() {}

			public void Deserialize(FileEncoder encoder) {
				this.class_id = encoder.ReadInt32();
				this.is_stripped = encoder.ReadBool();
				this.script_type_index = encoder.ReadInt16();

				if (this.class_id == 114) {
					this.script_id = encoder.ReadBytes(16);
				}

				this.hash = encoder.ReadBytes(16);
			}
			public FileEncoder Serialize(FileEncoder encoder) {
				return 
					encoder.WriteInt32(this.class_id)
					.WriteBool(this.is_stripped)
					.WriteInt16(this.script_type_index)
					.WriteBytes(this.script_id)
					.WriteBytes(this.hash);
			}

			public AssetIDType GetAssetType() {
				return (AssetIDType) class_id;
			}
		}

		public class ObjectStruct : BinarySerializable {
			public long path_id;
			public uint start_offset;
			public uint size_bytes;
			public int type_id;

			public ObjectStruct() {}

			public void Deserialize(FileEncoder encoder) {
				this.path_id = encoder.ReadInt64();
				this.start_offset = encoder.ReadUint32();
				this.size_bytes = encoder.ReadUint32();
				this.type_id = encoder.ReadInt32();
			}
			public FileEncoder Serialize(FileEncoder encoder) {
				return 
					encoder.WriteInt64(this.path_id)
					.WriteUint32(this.start_offset)
					.WriteUint32(this.size_bytes)
					.WriteInt32(this.type_id);
			}
		}

		public class ScriptStruct : BinarySerializable {
			public int file_index;
			public long file_id;

			public ScriptStruct() {}

			public void Deserialize(FileEncoder encoder) {
				this.file_index = encoder.ReadInt32();
				this.file_id = encoder.ReadInt64();
			}
			public FileEncoder Serialize(FileEncoder encoder) {
				return 
					encoder.WriteInt32(file_index)
					.WriteInt64(file_id);
			}
		}

		public class ExternalsStruct : BinarySerializable {
			public string unknown;
			public byte[] guid;
			public int type;
			public string path_name;

			public ExternalsStruct() {}

			public void Deserialize(FileEncoder encoder) {
				this.unknown = encoder.ReadStringToNull();
				this.guid = encoder.ReadBytes(16);
				this.type = encoder.ReadInt32();
				this.path_name = encoder.ReadStringToNull();
			}
			public FileEncoder Serialize(FileEncoder encoder) {
				return 
					encoder.WriteStringToNull(this.unknown)
					.WriteBytes(guid)
					.WriteInt32(type)
					.WriteStringToNull(path_name);
			}
		}

		public class RefTypeStruct : BinarySerializable {
			public int class_id;
			public bool is_stripped;
			public short script_type_index;
			public byte[] script_id;
			public byte[] hash;

			public RefTypeStruct() {}

			public void Deserialize(FileEncoder encoder) {
				this.class_id = encoder.ReadInt32();

				this.is_stripped = encoder.ReadBool();
				this.script_type_index = encoder.ReadInt16();

				if (this.script_type_index != -1) {
					this.script_id = encoder.ReadBytes(16);
				}

				this.hash = encoder.ReadBytes(16);
			}
			public FileEncoder Serialize(FileEncoder encoder) {
				return
					encoder.WriteInt32(this.class_id)
					.WriteBool(this.is_stripped)
					.WriteInt16(this.script_type_index)
					.WriteBytes(this.script_id)
					.WriteBytes(this.hash);
			}
		}
	}
}