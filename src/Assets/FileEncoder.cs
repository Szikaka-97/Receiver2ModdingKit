using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Receiver2ModdingKit.Assets {
	public class FileEncoder {
		public enum Endianness {
			BigEndian,
			LittleEndian
		}

		public FileStream stream {
			get;
			private set;
		}
		public Endianness Endian {
			get;
			set;
		}

		public FileEncoder(string file_name) {
			this.stream = File.Open(file_name, FileMode.OpenOrCreate);
			this.Endian = Endianness.BigEndian;
		}

		private T GetNBitsAs<T>() where T : struct {
			long result = 0;

			int count = Marshal.SizeOf<T>();

			byte[] bytes = new byte[count];

			this.stream.Read(bytes, 0, count);

			if (this.Endian == Endianness.BigEndian) {
				for (int i = 0; i < count; i++) {
					result += bytes[i] << (8 * i);
				}
			}
			else {
				for (int i = count - 1; i >= 0; i--) {
					result += bytes[i] << (8 * i);
				}
			}

			return (T)(object) result;
		}

		public byte ReadByte() {
			return GetNBitsAs<byte>();
		}

		public short ReadInt16() {
			return GetNBitsAs<short>();
		}

		public ushort ReadUInt16() {
			return GetNBitsAs<ushort>();
		}

		public int ReadInt32() {
			return GetNBitsAs<int>();
		}

		public uint ReadUint32() {
			return GetNBitsAs<uint>();
		}

		public long ReadInt64() {
			return GetNBitsAs<long>();
		}

		public ulong ReadUint64() {
			return GetNBitsAs<ulong>();
		}

		public bool ReadBool() {
			return GetNBitsAs<byte>() != 0;
		}

		public string ReadStringToNull() {
			StringBuilder result = new StringBuilder();

			int current_byte = this.stream.ReadByte();

			while (current_byte != 0) {
				result.Append((char) current_byte);
			}

			return result.ToString();
		}

		public string ReadStringWithPrefix() {
			int length = GetNBitsAs<int>();

			StringBuilder result = new StringBuilder(length);

			for (int i = 0; i < length; i++) {
				int current_byte = this.stream.ReadByte();

				result.Append((char) current_byte);
			}

			return result.ToString();
		}
	}
}
