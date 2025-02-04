using Receiver2ModdingKit.Assets.HeaderStructs;
using UnityEngine;
using System;
using System.Text.RegularExpressions;

namespace Receiver2ModdingKit.Assets {
	public class AssetFileHeader : BinarySerializable {
		public AssetFileMetadata metadata = new AssetFileMetadata();

		public TypeStruct[] types = new TypeStruct[0];
		public ObjectStruct[] objects = new ObjectStruct[0];
		public ScriptStruct[] scripts = new ScriptStruct[0];
		public ExternalsStruct[] externals = new ExternalsStruct[0];
		public RefTypeStruct[] ref_types = new RefTypeStruct[0];

		private void ReadArray<T>(out T[] array, FileEncoder encoder, bool align_stream = false) where T : BinarySerializable, new() {
			int count = encoder.ReadInt32();

			if (align_stream) {
				encoder.AlignStream(4);
			}

			try {
				array = new T[count];
			} catch (Exception e) {
				Debug.LogError("Broken count: " + count);
				array = new T[0];
				throw e;
			}

			for (int i = 0; i < count; i++) {
				array[i] = new T();
				array[i].Deserialize(encoder);
			}
		}

		private void SaveArray(BinarySerializable[] array, FileEncoder encoder, bool align_stream = false) {
			encoder.WriteInt32(array.Length);

			if (align_stream) {
				encoder.AlignStream(4);
			}

			foreach (BinarySerializable obj in array) {
				obj.Serialize(encoder);
			}
		}

		public void Deserialize(FileEncoder encoder) {
			this.metadata.Deserialize(encoder);

			// In case the file is corrupted, or not an asset file
#warning Implement some sane exception type
			if (!Regex.IsMatch(this.metadata.unity_version, @"\d+\.\d+\.\d+(f\d+)?")) {
				throw new Exception("Yo shitass this file is busted");
			}

			encoder.PositionLimit = this.metadata.file_size;
			
			this.ReadArray(out this.types, encoder);
			this.ReadArray(out this.objects, encoder, true);
			this.ReadArray(out this.scripts, encoder);
			this.ReadArray(out this.externals, encoder);
			this.ReadArray(out this.ref_types, encoder);
		}

		public FileEncoder Serialize(FileEncoder encoder) {
			this.metadata.Serialize(encoder);

			this.SaveArray(this.types, encoder);
			this.SaveArray(this.objects, encoder, true);
			this.SaveArray(this.scripts, encoder);
			this.SaveArray(this.externals, encoder);
			this.SaveArray(this.ref_types, encoder);

			return encoder;
		}
#warning Minus 19 for metadata.header_size
		public int GetSize() {
			int size = this.metadata.GetSize() + 4 * 5;

			foreach (var type_struct in this.types) {
				size += type_struct.GetSize();
			}

			if (size % 4 > 0) {
				size += 4 - (size % 4);
			}

			// Only object and script structs have fixed size

			int object_struct_size = 0;
			if (this.objects.Length > 0) {
				object_struct_size = this.objects[0].GetSize();
			}
			size += object_struct_size * this.objects.Length;

			int script_struct_size = 0;
			if (this.scripts.Length > 0) {
				script_struct_size = this.scripts[0].GetSize();
			}
			size += script_struct_size * this.scripts.Length;

			foreach (var external_struct in this.externals) {
				size += external_struct.GetSize();
			}

			foreach (var ref_type_struct in this.ref_types) {
				size += ref_type_struct.GetSize();
			}

			return size;
		}

		public void RecalculateSizes() {

		}
	}
}