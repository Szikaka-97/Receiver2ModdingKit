using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;

namespace Receiver2ModdingKit.Assets {
	public class FileEncoder {
		public enum Endianness {
			LittleEndian,
			BigEndian,
		}

		public Stream BackingStream {
			get;
			private set;
		}
		public Endianness Endian {
			get;
			set;
		}
		public bool CanWrite {
			get;
			private set;
		}

#warning Prepare some type of exception for crossing the limit
		public long PositionLimit {
			get;
			set;
		} = -1;

		public bool IsLimited {
			get {
				return PositionLimit >= 0;
			}
		}

		public long Position {
			get {
				return BackingStream.Position;
			}
			set {
				BackingStream.Position = value;
			}
		}

		public FileEncoder(string file_path, bool read_only = false, Endianness endianness = Endianness.LittleEndian) {
			if (read_only || !FileUtilities.CanWrite(file_path)) {
				this.BackingStream = new FileStream(file_path, FileMode.Open, FileAccess.Read, FileShare.Read);

				this.CanWrite = false;
			}
			else {
				this.BackingStream = File.Open(file_path, FileMode.OpenOrCreate);

				this.CanWrite = true;
			}

			this.Endian = endianness;
		}

		public FileEncoder(Stream stream, bool can_write, Endianness endianness = Endianness.LittleEndian) {
			this.BackingStream = stream;
			this.CanWrite = can_write;
			this.Endian = endianness;
		}

		public FileEncoder(Stream stream, Endianness endianness = Endianness.LittleEndian) {
			this.BackingStream = stream;
			this.Endian = endianness;
		}

#region Read

		private T GetNumber<T>() where T : struct {
			int count = Marshal.SizeOf<T>();

			if (this.IsLimited && this.Position + count > this.PositionLimit) {
				throw new Exception("Stream position out of assigned limit (" + this.Position + count + " > " + this.PositionLimit +")");
			}

			byte[] bytes = new byte[count];

			this.BackingStream.Read(bytes, 0, count);

			if (this.Endian == Endianness.BigEndian) {
				byte[] temp = new byte[count];

				for (int i = 0; i < count; i++) {
					temp[count - 1 - i] = bytes[i];
				}
				
				bytes = temp;
			}

			var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
			T result = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
			handle.Free();

			return result;
		}

		public byte ReadByte() {
			return GetNumber<byte>();
		}

		public short ReadInt16() {
			return GetNumber<short>();
		}

		public ushort ReadUInt16() {
			return GetNumber<ushort>();
		}

		public int ReadInt32() {
			return GetNumber<int>();
		}

		public uint ReadUint32() {
			return GetNumber<uint>();
		}

		public long ReadInt64() {
			return GetNumber<long>();
		}

		public ulong ReadUint64() {
			return GetNumber<ulong>();
		}

		public bool ReadBool() {
			return GetNumber<byte>() != 0;
		}

		public string ReadStringToNull(int max_length = 256) {
			StringBuilder result = new StringBuilder();

			int current_byte = this.BackingStream.ReadByte();

			for (int i = 0; i < max_length && current_byte != 0; i++) {
				result.Append((char) current_byte);
				
				current_byte = this.BackingStream.ReadByte();

				if (this.IsLimited && this.Position > this.PositionLimit) {
					throw new Exception("Stream position out of assigned limit");
				}
			}


			return result.ToString();
		}

		public string ReadStringWithPrefix() {
			int length = GetNumber<int>();

			if (this.IsLimited && this.Position + length > this.PositionLimit) {
				throw new Exception("Stream position out of assigned limit");
			}

			StringBuilder result = new StringBuilder(length);

			for (int i = 0; i < length; i++) {
				int current_byte = this.BackingStream.ReadByte();

				result.Append((char) current_byte);
			}

			return result.ToString();
		}

		public byte[] ReadBytes(uint count) {
			if (this.IsLimited && this.Position + count > this.PositionLimit) {
				throw new Exception("Stream position out of assigned limit");
			}

			byte[] buffer = new byte[count];

			this.BackingStream.Read(buffer, 0, (int) count);

			return buffer;
		}
#endregion

#region Write
		private void WriteNumber<T>(T item) where T : struct {
			if (!this.CanWrite) {
				return;
			}

			long buffer = Convert.ToInt64(item);

			int count = Marshal.SizeOf<T>();

			byte[] bytes = new byte[count];


			for (int i = 0; i < count; i++) {
				if (this.Endian == Endianness.BigEndian) {
					bytes[count - 1 - i] = (byte) (buffer >> (8 * i));
				}
				else {
					bytes[i] = (byte) (buffer >> (8 * i));
				}
			}

			this.BackingStream.Write(bytes, 0, count);
		}

		public FileEncoder WriteByte(byte value) {
			WriteNumber(value);

			return this;
		}

		public FileEncoder WriteInt16(short value) {
			WriteNumber(value);

			return this;
		}

		public FileEncoder WriteUInt16(ushort value) {
			WriteNumber(value);

			return this;
		}

		public FileEncoder WriteInt32(int value) {
			WriteNumber(value);

			return this;
		}

		public FileEncoder WriteUint32(uint value) {
			WriteNumber(value);

			return this;
		}

		public FileEncoder WriteInt64(long value) {
			WriteNumber(value);

			return this;
		}

		public FileEncoder WriteUint64(ulong value) {
			WriteNumber(value);

			return this;
		}

		public FileEncoder WriteBool(bool value) {
			// Bools are 4 bytes long for some reason
			WriteNumber<byte>(Convert.ToByte(value));

			return this;
		}

		public FileEncoder WriteStringToNull(string value) {
			if (!this.CanWrite) {
				return this;
			}

			this.BackingStream.Write(Encoding.ASCII.GetBytes(value), 0, value.Length);

			this.BackingStream.WriteByte(0);

			return this;
		}

		public FileEncoder WriteStringWithPrefix(string value) {
			if (!this.CanWrite) {
				return this;
			}

			WriteNumber(value.Length);

			this.BackingStream.Write(Encoding.ASCII.GetBytes(value), 0, value.Length);

			return this;
		}

		public FileEncoder WriteBytes(byte[] data) {
			if (!this.CanWrite) {
				return this;
			}
			
			this.BackingStream.Write(data, 0, data.Length);

			return this;
		}
#endregion

		public FileEncoder SetEndianess(Endianness endianness) {
			this.Endian = endianness;

			return this;
		}

		public FileEncoder AlignStream(int n) {
			if (this.BackingStream.Position % n != 0) {
				this.BackingStream.Position += n - (this.BackingStream.Position % n);
			}

			return this;
		}

		public FileEncoder Advance(int n) {
			this.BackingStream.Position += n;

			return this;
		}

		public FileEncoder Clone() {
			var clone = new FileEncoder(this.BackingStream, this.CanWrite, this.Endian) {
				Position = this.Position
			};

			return clone;
		}

		public FileEncoder LimitedClone(long start, uint length) {
			var clone = new FileEncoder(this.BackingStream, this.CanWrite, this.Endian) {
				Position = start,
				PositionLimit = start + length
			};

			return clone;
		}
	}
}
