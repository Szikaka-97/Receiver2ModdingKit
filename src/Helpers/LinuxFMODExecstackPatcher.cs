using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Receiver2ModdingKit.Helpers {
	public static class LinuxELFExecstackPatcher {
		/// <summary>
		/// since glibc 2.41 they made it so if ur lib is marked as having an executable stack, it won't load
		/// FMOD libs don't have an executable stack, but are still marked as having one, so shit ass glibc won't load it
		/// this patches the flag so you don't have to use execstack or other bullshit like that
		/// </summary>
		/// <param name="filePath">The path to the .so that needs to be patched</param>
		/// <returns><c>True</c> if the file wasn't patched and the patching attempt succeeded, or <c>False</c> if the file was already patched, or if the patching attempt failed.</returns>
		public static bool PatchFlag(string filePath) {
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {	
				Debug.LogWarning("Patch ELF Flags called on non-linux platform");

				return false;
			}

			Debug.Log(filePath);

			using (var lib = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read)) {
				//64bit elf format stuff
				const uint e_phoff_offset = 0x20;
				const uint e_phoff_byte_length = 8;

				const uint e_phentsize_offset = 0x36;
				const uint e_phentsize_byte_length = 2;

				const uint e_phnum_offset = 0x38;
				const uint e_phnum_byte_length = 2;

				const uint PT_GNU_STACK_IDENT = 0x64_74_E5_51;
				const uint p_type_byte_length = 4;

				uint p_type_seg_start_location = 0;

				//get program header offset location
				{
					byte[] e_phoff_buffer = new byte[e_phoff_byte_length];

					//I think it's always 0x40, but uh, idc, whatever! i'm crazy!
					lib.Seek(e_phoff_offset, SeekOrigin.Begin);
					lib.Read(e_phoff_buffer, 0, (int)e_phoff_byte_length);

					//probably 64
					p_type_seg_start_location = BitConverter.ToUInt32(e_phoff_buffer, 0);
				}

				int p_entry_size = 0;

				//get program header size (usually 0x38, but what's the harm in checking?)
				{
					byte[] e_phentsize_buffer = new byte[e_phentsize_byte_length];

					lib.Seek(e_phentsize_offset, SeekOrigin.Begin);
					lib.Read(e_phentsize_buffer, 0, (int)e_phentsize_byte_length);

					p_entry_size = BitConverter.ToUInt16(e_phentsize_buffer, 0);
				}

				int p_entry_count = 0;

				//get count of program header entries
				{
					byte[] e_phnum_buffer = new byte[e_phnum_byte_length];

					lib.Seek(e_phnum_offset, SeekOrigin.Begin);
					lib.Read(e_phnum_buffer, 0, (int)e_phnum_byte_length);

					p_entry_count = BitConverter.ToUInt16(e_phnum_buffer, 0);
				}

				//checks every program header entry until it finds one with the PT::GNU_STAC identifier
				{
					byte[] p_type_buffer = new byte[p_type_byte_length];

					for (long p_entry_index = 0; p_entry_index < p_entry_count; p_entry_index++)
					{
						lib.Seek(p_type_seg_start_location + (p_entry_index * p_entry_size), SeekOrigin.Begin);

						lib.Read(p_type_buffer, 0, (int)p_type_byte_length);
						
						if (BitConverter.ToUInt32(p_type_buffer, 0) == PT_GNU_STACK_IDENT)
						{
							Debug.Log("found gnu stack ident");

							byte[] p_flags_buffer = new byte[4];

							lib.Seek(p_type_seg_start_location + (p_entry_index * p_entry_size) + 0x04, SeekOrigin.Begin);
							lib.Read(p_flags_buffer, 0, 4);

							if ((p_flags_buffer[0] & 0b00000001) == 0) {
								Debug.Log("flag is already patched, skipping");

								return false;
							}

							//clear the X flag
							p_flags_buffer[0] &= 0b11111110;

							lib.Seek(p_type_seg_start_location + (p_entry_index * p_entry_size) + 0x04, SeekOrigin.Begin);
							lib.Write(p_flags_buffer, 0, 4);

							lib.Flush();

							lib.Close();

							Debug.Log("patched flag");

							return true;
						}
					}

					Debug.Log("failed to find gnu stack entry, sorry!");
				}
			}

			return false;
		}
	}
}