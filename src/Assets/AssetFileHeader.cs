using System;
using System.Text;
using System.IO;
using UnityEngine;
using SimpleJSON;
using System.Text.RegularExpressions;

namespace Receiver2ModdingKit.Assets {
	public class AssetFileHeader {
		public string file_name {
			get;
			private set;
		}
		public FileEncoder file_stream {
			get;
			private set;
		}

		//Header start
		public int metadata_size {
			get;
			private set;
		}
		public uint file_size {
			get;
			private set;
		}
		public uint version {
			get;
			private set;
		}
		public uint data_offset {
			get;
			private set;
		}
		public byte endianess {
			get;
			private set;
		}
		// 3 * byte padding

		// Metadata
		public string unity_version {
			get;
			private set;
		}
		public AssetBuildTarget build_target {
			get;
			private set;
		}
		public bool enable_type_tree {
			get;
			private set;
		}

		// Types
		// int types_count
		private HeaderStructs.TypeStruct[] m_types;
		public HeaderStructs.TypeStruct[] Types {
			get {
				return m_types;
			}
		}

		// Objects
		// int object_count
		// align stream to 4
		private HeaderStructs.ObjectStruct[] m_objects;
		public HeaderStructs.ObjectStruct[] Objects {
			get {
				return m_objects;
			}
		}

		// Scripts
		// int scripts_count
		private HeaderStructs.ScriptStruct[] m_scripts;
		public HeaderStructs.ScriptStruct[] Scripts {
			get {
				return m_scripts;
			}
		}

		// Externals
		// int externals_count
		private HeaderStructs.ExternalsStruct[] m_externals;
		public HeaderStructs.ExternalsStruct[] Externals {
			get {
				return m_externals;
			}
		}

		// Ref types
		// int ref_types_count
		private HeaderStructs.RefTypeStruct[] m_ref_types;
		public HeaderStructs.RefTypeStruct[] RefTypes {
			get {
				return m_ref_types;
			}
		}

		private AssetFileHeader(string file_name) {
			this.file_name = file_name;
		}

		private void ReadHeaderStart(FileEncoder encoder) {
			encoder.Endian = FileEncoder.Endianness.BigEndian;

			this.metadata_size = encoder.ReadInt32();
			this.file_size = encoder.ReadUint32();
			this.version = encoder.ReadUint32();
			this.data_offset = encoder.ReadUint32();
			this.endianess = encoder.ReadByte();

			encoder.Advance(3);
		}

		private void ReadMetadata(FileEncoder encoder) {
			this.unity_version = encoder.ReadStringToNull();

			// In case the file is corrupted, or not an asset file
#warning Implement some sane exception type
			if (!Regex.IsMatch(this.unity_version, @"\d+\.\d+\.\d+(f\d+)?")) {
				throw new Exception("Yo shitass this file is busted");
			}

			this.build_target = (AssetBuildTarget) encoder.ReadInt32();
			this.enable_type_tree = encoder.ReadBool();
		}

		private void PopulateArray<T>(out T[] array, FileEncoder encoder) where T : BinarySerializable, new() {
			int count = encoder.ReadInt32();

			try {
				array = new T[count];
			} catch (Exception e) {
				Debug.LogError("Broken count: " + count);
				array = new T[0];
				count = 0;

				throw e;
			}

			for (int i = 0; i < count; i++) {
				array[i] = new T();
				array[i].Deserialize(encoder);
			}
		}

		private JSONArray ToJSONArray<T>(T[] array, Func<T, JSONNode> converter) {
			JSONArray result = new JSONArray();

			for (int i = 0; i < array.Length; i++) {
				result.Add(converter(array[i]));
			}

			return result;
		}

		public static string ByteArrayToString(byte[] byte_array) {
			StringBuilder hex = new StringBuilder(byte_array.Length * 2);
			foreach (byte b in byte_array)
				hex.AppendFormat("{0:x2}", b);
			return hex.ToString();
		}

		public JSONObject Save() {
			JSONObject root = new JSONObject();

			root["file_name"] = this.file_name;

			root["metadata_size"] = this.metadata_size;
			root["file_size"] = this.file_size;
			root["version"] = this.version;
			root["data_offset"] = this.data_offset;
			root["endianess"] = this.endianess;

			root["unity_version"] = this.unity_version;
			root["build_target"] = this.build_target.ToString();
			root["type_tree_enabled"] = this.enable_type_tree;

			root["types_count"] = this.Types.Length;
			root["types"] = ToJSONArray(this.Types, (type_struct) => {
				var node = new JSONObject();

				node["class_id"] = type_struct.class_id;
				node["is_stripped"] = type_struct.is_stripped;
				node["script_type_index"] = type_struct.script_type_index;
				node["script_id"] = type_struct.script_id != null ? ByteArrayToString(type_struct.script_id) : "null";
				node["hash"] = ByteArrayToString(type_struct.hash);

				return node;
			} );

			root["objects_count"] = this.Objects.Length;
			root["objects"] = ToJSONArray(this.Objects, (object_struct) => {
				var node = new JSONObject();

				node["path_id"] = object_struct.path_id;
				node["start_offset"] = object_struct.start_offset;
				node["size_bytes"] = object_struct.size_bytes;
				node["type_id"] = object_struct.type_id;

				return node;
			} );

			root["scripts_count"] = this.Scripts.Length;
			root["scripts"] = ToJSONArray(this.Scripts, (script_struct) => {
				var node = new JSONObject();

				node["file_index"] = script_struct.file_index;
				node["file_id"] = script_struct.file_id;

				return node;
			} );

			root["externals_count"] = this.Externals.Length;
			root["externals"] = ToJSONArray(this.Externals, (external_struct) => {
				var node = new JSONObject();

				node["unknown"] = external_struct.unknown;
				node["file_id"] = ByteArrayToString(external_struct.guid);
				node["type"] = external_struct.type;
				node["path_name"] = external_struct.path_name;

				return node;
			} );

			root["ref_types_count"] = this.RefTypes.Length;
			root["ref_types"] = ToJSONArray(this.RefTypes, (ref_type_struct) => {
				var node = new JSONObject();

				node["class_id"] = ref_type_struct.class_id;
				node["is_stripped"] = ref_type_struct.is_stripped;
				node["script_type_index"] = ref_type_struct.script_type_index;
				node["script_id"] = ref_type_struct.script_id != null ? ByteArrayToString(ref_type_struct.script_id) : "null";
				node["hash"] = ByteArrayToString(ref_type_struct.hash);

				return node;
			} );

			return root;
		}

		public static AssetFileHeader CreateFromFile(FileInfo file) {
			FileEncoder encoder = null;

			try {
				encoder = new FileEncoder(file.FullName, true, FileEncoder.Endianness.BigEndian);
				AssetFileHeader header = new AssetFileHeader(file.FullName) {
					file_stream = encoder
				};

				long position = 0;

				try {
					position = encoder.BackingStream.Position;

					header.ReadHeaderStart(encoder);
				} catch (Exception e) {
					Debug.LogError("Problem reading header from " + header.file_name);
					Debug.LogError("Section start at offset: " + position);
					Debug.LogException(e);

					return null;
				}

				encoder.Endian = header.endianess == 0 ? FileEncoder.Endianness.LittleEndian : FileEncoder.Endianness.BigEndian;

				try {
					position = encoder.BackingStream.Position;

					header.ReadMetadata(encoder);
				} catch (Exception e) {
					Debug.LogError("Problem reading metadata from " + header.file_name);
					Debug.LogError("Section start at offset: " + position);
					Debug.LogException(e);

					return null;
				}

				try {
					position = encoder.BackingStream.Position;

					header.PopulateArray(out header.m_types, encoder);
				} catch (Exception e) {
					Debug.LogError("Problem reading types from " + header.file_name);
					Debug.LogError("Section start at offset: " + position);
					Debug.LogException(e);

					return null;
				}

				try {
					position = encoder.BackingStream.Position;
					
					int count = encoder.ReadInt32();

					encoder.AlignStream(4);

					header.m_objects = new HeaderStructs.ObjectStruct[count];

					for (int i = 0; i < count; i++) {
						header.m_objects[i] = new HeaderStructs.ObjectStruct();
						header.m_objects[i].Deserialize(encoder);
					}
				} catch (Exception e) {
					Debug.LogError("Problem reading objects from " + header.file_name);
					Debug.LogError("Section start at offset: " + position);
					Debug.LogException(e);

					return null;
				}
				try {
					position = encoder.BackingStream.Position;

					header.PopulateArray(out header.m_scripts, encoder);
				} catch (Exception e) {
					Debug.LogError("Problem reading scripts from " + header.file_name);
					Debug.LogError("Section start at offset: " + position);
					Debug.LogException(e);

					return null;
				}
				try {
					position = encoder.BackingStream.Position;
					
					header.PopulateArray(out header.m_externals, encoder);
				} catch (Exception e) {
					Debug.LogError("Problem reading externals from " + header.file_name);
					Debug.LogError("Section start at offset: " + position);
					Debug.LogException(e);

					return null;
				}
				try {
					position = encoder.BackingStream.Position;

					header.PopulateArray(out header.m_ref_types, encoder);
				} catch (Exception e) {
					Debug.LogError("Problem reading ref types from " + header.file_name);
					Debug.LogError("Section start at offset: " + position);
					Debug.LogException(e);

					return null;
				}

				encoder.BackingStream.Close();

				return header;
			} catch (IOException) {
				if (encoder != null) {
					encoder.BackingStream.Close();
				}

				Debug.LogError("File " + file.FullName + " can't be read at the moment!");
				Debug.LogError("Can be read: " + file.CanRead());
				// Debug.LogException(e);
			}

			return null;
		}
	}
}