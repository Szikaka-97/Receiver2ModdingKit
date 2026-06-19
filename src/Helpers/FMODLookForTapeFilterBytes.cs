using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using FMOD.Studio;
using FMODUnity;
using HarmonyLib;
using Receiver2;
using UnityEngine;

namespace Receiver2ModdingKit.Helpers {
	//The event:/Tapes/tape_audio_wrapper has a parameter called DamagedMedia, whose max length is only 3:11 minutes, which means that, if you want to make a tape that lasts longer than 3:11 minutes, you're shit out of luck
	//unless you do this!!!!!!!!!!!!!
	//Step 1: Copy the original "Tapes Bank.bank" into "Tapes Bank-Patched.bank"
	//Step 2: Search for the 8 byte aligned pattern
	//Step 3: Make the event's Max Length the 32bit unsigned int value
	//Step 4: Redirect the FMODUnity.RuntimeManager to load "Tapes Bank-Patched" instead
	//Step 5: Epic Bacon Win, works flawlessly, DamagedMedia still functions perfectly after 3:11 minutes
	//I'd have prefered to have made a cool method to get an EventDescription, and increase its max length
	//but alas, I couldn't really be bothered to spend 5 days decompiling and stepping through debuggers
	public static class FMODLookForTapeFilterBytes {

		//Pattern of the stuff, only managed to figure out that it's an array of 12 elements that are 24bytes big, with the last 8 bytes being 2 (u?)ints
		private static readonly byte[] pattern = new byte[]
		{
			0x00, 0x58, 0xDB, 0xFA, 0xCE, 0x41, 0xCB, 0x4C, 0xA1, 0x05, 0xDF, 0xEC, 0x31, 0x40, 0x02, 0x13,
			0x00, 0xF9, 0x15, 0x00, 0x00, 0xF9, 0x15, 0x00, 0x0F, 0x75, 0x3E, 0xD0, 0xE4, 0x48, 0xBB, 0x4C, 
			0xA8, 0x51, 0x6E, 0x49, 0xA8, 0x6A, 0x73, 0xD6, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF9, 0x15, 0x00, 
			0x1A, 0x09, 0xA5, 0x90, 0x27, 0x6A, 0xE4, 0x40, 0xBF, 0xC3, 0x12, 0x5E, 0x50, 0xD8, 0x8F, 0x54, 
			0x00, 0x00, 0x00, 0x00, 0xB9, 0xBD, 0x2B, 0x00, 0x1A, 0x50, 0xE5, 0x2A, 0x61, 0xF8, 0xE4, 0x44, 
			0xA2, 0x7E, 0x80, 0x9F, 0xCB, 0xEC, 0xE8, 0x03, 0x00, 0xF2, 0x2B, 0x00, 0x00, 0xF9, 0x15, 0x00, 
			0x60, 0x21, 0xC5, 0x01, 0x33, 0x0E, 0xF1, 0x46, 0xB9, 0xEE, 0xE0, 0x68, 0x42, 0x40, 0xD6, 0x73, 
			0x00, 0xD6, 0x83, 0x00, 0x80, 0x0E, 0x08, 0x00, 0x80, 0x3E, 0x59, 0xAD, 0xFA, 0xFD, 0x56, 0x43, 
			0x9E, 0xEA, 0xAA, 0xED, 0x4D, 0xF7, 0x13, 0x63, 0x00, 0xE4, 0x57, 0x00, 0x00, 0xF9, 0x15, 0x00, 
			0x9F, 0x51, 0x4F, 0x92, 0xF3, 0xA4, 0x77, 0x4E, 0xAE, 0x0D, 0x0F, 0x82, 0x0D, 0x4B, 0x21, 0x02, 
			0x00, 0xDD, 0x6D, 0x00, 0x00, 0xF9, 0x15, 0x00, 0xB0, 0x1D, 0x52, 0xF5, 0x8B, 0xCF, 0x6B, 0x46, 
			0xBF, 0x8F, 0x00, 0xC0, 0xF2, 0xF6, 0x5F, 0xCF, 0x4A, 0x3B, 0x83, 0x00, 0x36, 0xA9, 0x08, 0x00, 
			0xCA, 0x47, 0x87, 0x59, 0x6A, 0x96, 0xDB, 0x47, 0x8D, 0x93, 0x59, 0xFB, 0x81, 0xE2, 0xD2, 0x37, 
			0x91, 0x7D, 0x57, 0x00, 0xB9, 0xBD, 0x2B, 0x00, 0xD0, 0xAA, 0x08, 0xC0, 0x32, 0x35, 0x18, 0x40, 
			0x90, 0xC4, 0xBB, 0x4D, 0x3A, 0xA1, 0x0D, 0x86, 0xB9, 0xBD, 0x2B, 0x00, 0xB9, 0xBD, 0x2B, 0x00, 
			0xDB, 0x7B, 0x19, 0x0B, 0x0D, 0x4E, 0x74, 0x4B, 0xB1, 0x0E, 0x66, 0xEA, 0xBF, 0xE3, 0xC4, 0x99, 
			0x00, 0x00, 0x00, 0x00, 0x80, 0xE4, 0x8B, 0x00, 0xF8, 0x80, 0x88, 0x09, 0x64, 0x6E, 0x6F, 0x42, 
			0xBD, 0x5F, 0x07, 0xA1, 0x95, 0xEB, 0x72, 0xEC, 0x00, 0xEB, 0x41, 0x00, 0x00, 0xF9, 0x15, 0x00,	
		};

		const string k_PatchedName = "Tapes Bank-Patched";

		//24 bytes element, only the last thing matters :)
		//might represent an event's parameters?
		[StructLayout(LayoutKind.Explicit, Size = 24)]
		private struct UnknownArrayElement
		{
			//pad it out, lol :)
			[FieldOffset(0)] public byte fullOfMilk;
			[FieldOffset(16)] public uint lengthOfSomethingIThink;
			[FieldOffset(20)] public uint eventLength;
		}

		[HarmonyPatch(typeof(RuntimeManager), nameof(RuntimeManager.LoadBank), new Type[] { typeof(string), typeof(bool) })]
		[HarmonyPrefix]
		private static void SleightOfHand(ref string bankName) {
			if (bankName == "Tapes Bank") {
				if (!File.Exists(Path.Combine(Application.streamingAssetsPath, k_PatchedName + ".bank"))) {
					if (!SwapAndPatchBank()) {
						UnityEngine.Debug.LogError("Failed to swap and patch bank");
					}
					else
					{
						UnityEngine.Debug.Log("Successfully patched Tapes Bank");
					}
				}

				bankName = k_PatchedName;
			}
		}

		private static bool SwapAndPatchBank() {
			//8 bytes alignment for FMOD Banks
			const int k_BankAlignment = 8;

			File.Copy(Path.Combine(Application.streamingAssetsPath, "Tapes Bank.bank"), Path.Combine(Application.streamingAssetsPath, k_PatchedName + ".bank"));

			using (var bankFile = File.Open(Path.Combine(Application.streamingAssetsPath, k_PatchedName + ".bank"), FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
				byte[] buffer = new byte[8];

				bool foundPattern = false;
				long basePosition = 0;

				while (bankFile.Position < bankFile.Length && !foundPattern) {
					basePosition = bankFile.Position;

					int offset = 0;

					while (offset < k_BankAlignment) {
						offset = bankFile.Read(buffer, offset, k_BankAlignment);
					}
					
					//makes me weep
					for (int matchingBytesOffset = 0; 
						buffer[0] == pattern[matchingBytesOffset] && 
						buffer[1] == pattern[matchingBytesOffset + 1] && 
						buffer[2] == pattern[matchingBytesOffset + 2] && 
						buffer[3] == pattern[matchingBytesOffset + 3] && 
						buffer[4] == pattern[matchingBytesOffset + 4] && 
						buffer[5] == pattern[matchingBytesOffset + 5] && 
						buffer[6] == pattern[matchingBytesOffset + 6] && 
						buffer[7] == pattern[matchingBytesOffset + 7]; 
						matchingBytesOffset += k_BankAlignment)
					{
						bankFile.Read(buffer, 0, k_BankAlignment);

						if (matchingBytesOffset == pattern.Length - k_BankAlignment){	
							foundPattern = true;

							break;
						}
					}
				}

				//I WISH I HAD SPANS!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
				if (foundPattern) {
					var iWishIHadSpans = new byte[pattern.Length];

					Array.Copy(pattern, 0, iWishIHadSpans, 0, pattern.Length);

					//I hate how well this works
					//Should've given me my Spans!
					for (int elementIndex = 0; elementIndex < iWishIHadSpans.Length; elementIndex += 24) {
						unsafe {
							fixed (byte* elementStart = &iWishIHadSpans[elementIndex]) {
								UnknownArrayElement* element = (UnknownArrayElement*)elementStart;

								element->eventLength = uint.MaxValue;
							}
						}
					}

					bankFile.Seek(basePosition, SeekOrigin.Begin);
					bankFile.Write(iWishIHadSpans, 0, pattern.Length);

					bankFile.Flush();

					return true;
				}
			}

			return false;
		}
	}
}