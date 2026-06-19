using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using Receiver2ModdingKit.CustomSounds;

namespace Receiver2ModdingKit.Helpers {
	/// <summary>
	/// Bunch of methods to use when you want to inspect FMOD stuff in Unity Explorer
	/// </summary>
	public static class FMODUnityExplorerHelper {
		/// <summary>
		/// Go through all the FMOD shit and tries and find the object with the provided handle (doesn't work)
		/// </summary>
		/// <param name="handle">The handle you're looking for</param>
		/// <returns>Whatever object was found, or null</returns>
		public static object KetsupBlast(IntPtr handle) {
			var coreSystem = RuntimeManager.CoreSystem;

			if (coreSystem.handle == handle) {
				return coreSystem;
			}

			if (coreSystem.getOutputHandle(out var outputhandle) == RESULT.OK && outputhandle == handle) {
				return handle;
			}

			if (coreSystem.getMasterChannelGroup(out var masterchannelgroup) == RESULT.OK) {
				if (masterchannelgroup.handle == handle){
					return masterchannelgroup;
				}

				var mcgresult = RecurseThroughChannelGroup(masterchannelgroup);
				if (mcgresult != null) {
					return mcgresult;
				}
			}

			if (coreSystem.getMasterSoundGroup(out var mastersoundgroup) == RESULT.OK) {
				if (mastersoundgroup.handle == handle) {
					return mastersoundgroup;
				}

				mastersoundgroup.getNumSounds(out var numsounds);

				for (int sindex = 0; sindex < numsounds; sindex++) {
					mastersoundgroup.getSound(sindex, out var soudning);
					
					if (soudning.handle == handle) {
						return handle;
					}

					var mssrcresult = RecurseThroughSound(soudning);
					if (mssrcresult != null)
					{
						return mssrcresult;
					}
				}
			}

			if (coreSystem.getUserData(out var udata) == RESULT.OK && udata == handle) {
				return udata;
			}

			var studioSystem = RuntimeManager.StudioSystem;

			if (studioSystem.handle == handle) {
				return studioSystem;
			}

			if (studioSystem.getBankList(out var banks) == RESULT.OK) {
				var numbanks = banks.Length;

				for (int bindex = 0; bindex < numbanks; bindex++) {
					banks[bindex].getEventList(out var eventDescs);

					var numdescs = eventDescs.Length;

					for (int dindex = 0; dindex < numdescs; dindex++) {
						if (eventDescs[dindex].handle == handle) {
							return eventDescs[dindex];
						}
					}
				}
			}

			object RecurseThroughChannelGroup(FMOD.ChannelGroup channelGroup) {
				channelGroup.getNumChannels(out var numchannels);

				for (int i = 0; i < numchannels; i++) {
					if (channelGroup.getChannel(i, out var subchannel) == RESULT.OK) {
						if (subchannel.handle == handle) {
							return subchannel;
						}

						if (subchannel.getCurrentSound(out var channelcurrentsound) == RESULT.OK && channelcurrentsound.handle == handle) {
							return channelcurrentsound;
						}
					}
				}

				channelGroup.getNumGroups(out var numgroups);

				for (int gindex = 0; gindex < numgroups; gindex++) {
					if (channelGroup.getGroup(gindex, out var group) == RESULT.OK) {
						var rgresult = RecurseThroughChannelGroup(group);
						if (rgresult != null)
							return rgresult;
					}
				}

				return null;
			}

			object RecurseThroughSound(FMOD.Sound sound) {
				if (sound.getNumSubSounds(out var numsubsounds) == RESULT.OK) {
					for (int ssindex = 0; ssindex < numsubsounds; ssindex++) {
						if (sound.getSubSound(ssindex, out var subsound) == RESULT.OK) {
							if (subsound.handle == handle) {
								return subsound;
							}

							if (subsound.getSoundGroup(out var coolsoundgrop) == RESULT.OK && coolsoundgrop.handle == handle) {
								return coolsoundgrop;
							}

							var rsresult = RecurseThroughSound(subsound);
							if (rsresult != null) {
								return rsresult;
							}
						}
					}
				}

				return null;
			}

			return null;
		}

		private static class EventInstance
		{
			private static T GetUserData<T>(FMOD.Studio.EventInstance eventInstance) {
				if (!Utility.IsError(eventInstance.getUserData(out var data), nameof(GetUserData))) {
					return Marshal.PtrToStructure<T>(data);
				}

				return default;
			}

			private static FMOD.Studio.EventDescription GetEventDescription(FMOD.Studio.EventInstance eventInstance) {
				if (!Utility.IsError(eventInstance.getDescription(out var eventDescription), nameof(GetEventDescription))) {
					return eventDescription;
				}

				return default;
			}

			private static void SetTimelinePosition(FMOD.Studio.EventInstance eventInstance, int position) {
				Utility.IsError(eventInstance.setTimelinePosition(position), nameof(SetTimelinePosition));
			}

			private static float GetParameterByID(FMOD.Studio.EventInstance eventInstance, PARAMETER_ID id) {
				if (!Utility.IsError(eventInstance.getParameterByID(id, out var val))) {
					return val;
				}

				return 0;
			}
			
			private static (float, float) GetParametersByID(FMOD.Studio.EventInstance eventInstance, PARAMETER_ID id) {
				if (!(Utility.IsError(eventInstance.getParameterByID(id, out var val1, out var val2)))) {
					return (val1, val2);
				}

				return (0f, 0f);
			}

			private static FMOD.ChannelGroup GetChannelGroup(FMOD.Studio.EventInstance eventInstance) {
				if (!(Utility.IsError(eventInstance.getChannelGroup(out var group)))) {
					return group;
				}

				return default;
			}
		}

		private static class ChannelGroup {
			private static MODE GetMode(FMOD.ChannelGroup group) {
				if (!Utility.IsError(group.getMode(out var mode))) {
					return mode;
				}

				return MODE.DEFAULT;
			}

			private static FMOD.ChannelGroup GetChannelGroup(FMOD.ChannelGroup group, int index) {
				if (!Utility.IsError(group.getGroup(index, out var outgroup))) {
					return outgroup;
				}

				return default;
			}

			private static int GetNumGroups(FMOD.ChannelGroup group) {
				if (!Utility.IsError(group.getNumGroups(out var count))) {
					return count;
				}

				return 0;
			}

			private static FMOD.Sound[] DebugLogGroups(FMOD.ChannelGroup group) {
				if (!Utility.IsError(group.getNumGroups(out var count), nameof(DebugLogGroups))) {
					List<FMOD.Sound> sounds = new List<FMOD.Sound>();
					for (int i = 0; i < count; i++) {
						if (!Utility.IsError(group.getGroup(i, out var outgroup), nameof(DebugLogGroups) + " 1")) {
							if (!Utility.IsError(group.getName(out var gname, 256), nameof(DebugLogGroups) + " 2")) {
								UnityEngine.Debug.Log(gname);

								DebugLogGroups(outgroup);

								sounds.AddRange(DebugLogChannels(outgroup));
							}
						}
					}

					return sounds.ToArray();
				}
				
				return null;
			}

			private static FMOD.Sound[] DebugLogChannels(FMOD.ChannelGroup group) {
				if (!Utility.IsError(group.getNumChannels(out var numchannels), nameof(DebugLogChannels))) {
					UnityEngine.Debug.Log($"numchannel: {numchannels}");
					List<FMOD.Sound> sounds = new List<FMOD.Sound>();
					for (int i = 0; i < numchannels; i++) {
						if (!Utility.IsError(group.getChannel(i, out var channel), nameof(DebugLogChannels) + " 1")) {
							if (!Utility.IsError(channel.getMode(out var mode), nameof(DebugLogChannels) + " 2")) {
								UnityEngine.Debug.Log(mode);
							}

							if (!Utility.IsError(channel.getCurrentSound(out var sound), nameof(DebugLogChannels) + " 3")) {
								if (!Utility.IsError(sound.getName(out var name, 256), nameof(DebugLogChannels) + " 3.1")) {
									UnityEngine.Debug.Log(name);
								}

								if (!Utility.IsError(sound.getLength(out var soundlength, TIMEUNIT.MS), nameof(DebugLogChannels) + " 3.2")) {
									UnityEngine.Debug.Log($"length: {soundlength}");
								}

								if (!Utility.IsError(sound.getLoopCount(out var loopcount), nameof(DebugLogChannels) + " 3.3")) {
									UnityEngine.Debug.Log($"loop count: {loopcount}");

									if (!Utility.IsError(sound.getLoopPoints(out var loopstart, TIMEUNIT.MS, out var loopend, TIMEUNIT.MS))) {
										UnityEngine.Debug.Log($"loop start: {loopstart}");
										UnityEngine.Debug.Log($"loop end: {loopend}");
									}
								}

								sounds.Add(sound);
							}

							if (!Utility.IsError(channel.getPosition(out var pos, TIMEUNIT.MS), nameof(DebugLogChannels) + " 4")) {
								UnityEngine.Debug.Log($"pos: {pos}");
							}
						}
					}

					return sounds.ToArray();
				}

				return null;
			}
		}

		private static class Channel {
		}

		private static class EventDescription {
			private static int GetLength(FMOD.Studio.EventDescription eventDescription) {
				if (!Utility.IsError(eventDescription.getLength(out var length), nameof(GetLength))) {
					return length;
				}

				return 0;
			}

			private static void DebugLogParameters(FMOD.Studio.EventDescription eventDescription) {
				if (!Utility.IsError(eventDescription.getParameterDescriptionCount(out var count), nameof(DebugLogParameters))) {
					for (int i = 0; i < count; i++) {
						if (!Utility.IsError(eventDescription.getParameterDescriptionByIndex(i, out var param), param.name)) {
							UnityEngine.Debug.Log($"name: {(string)param.name}");
							UnityEngine.Debug.Log($"id data 1: {param.id.data1}");
							UnityEngine.Debug.Log($"id data 2: {param.id.data2}");
							UnityEngine.Debug.Log($"minimum: {param.minimum}");
							UnityEngine.Debug.Log($"default: {param.defaultvalue}");
							UnityEngine.Debug.Log($"maximum: {param.maximum}");
							UnityEngine.Debug.Log($"type: {param.type}");
							UnityEngine.Debug.Log($"flags: {param.flags}");
						}
					}
				}
			}
		}

		private static class Sound {
			private static int GetLoopCount(FMOD.Sound sound) {
				if (!Utility.IsError(sound.getLoopCount(out var loop), nameof(GetLoopCount))) {
					return loop;
					
				}

				return 0;
			}
		}

		private static class System {
			private static Bank[] GetBanks(FMOD.Studio.System system) {
				if (!Utility.IsError(system.getBankList(out var banks))) {
					return banks;
				}

				return null;
			}
		}
	}
}