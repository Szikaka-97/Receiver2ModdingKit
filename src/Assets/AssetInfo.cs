using System.IO;

namespace Receiver2ModdingKit.Assets {
	public class AssetInfo {
		public AssetFileHeader header {
			get;
			private set;
		}

		public long path_id {
			get;
			private set;
		}

		public uint start_offset {
			get;
			private set;
		}

		public uint size {
			get;
			private set;
		}

		public int type_index {
			get;
			private set;
		}

		public string name {
			get;
			set;
		}

		public AssetReferencePair[] dependencies_paths {
			get;
			set;
		}

		public AssetInfo[] dependencies {
			get;
			private set;
		}

		public static AssetInfo FromObjectStruct(HeaderStructs.ObjectStruct obj, AssetFileHeader header = null) {
			return new AssetInfo() {
				path_id = obj.path_id,
				start_offset = obj.start_offset,
				size = obj.size_bytes,
				type_index = obj.type_id,
				header = header
			};
		}

		public FileEncoder GetEncoder() {
			if (this.header == null) {
				return null;
			}

			var encoder = 
				(this.header.file_stream != null && this.header.file_stream.BackingStream.CanRead)
				? this.header.file_stream.Clone()
				: new FileEncoder(this.header.file_name, true, FileEncoder.Endianness.LittleEndian);

			encoder.Position = this.GetAbsoluteOffset();

			return encoder;
		}

		public uint GetAbsoluteOffset() {
			if (this.header == null) {
				return this.start_offset;
			}

			return this.header.data_offset + this.start_offset;
		}
	}
}