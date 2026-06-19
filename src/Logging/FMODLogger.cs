using System.Text;
using FMOD;

namespace Receiver2ModdingKit.Logging
{
	/// <summary>
	/// Logger for the Logging Version of FMOD™
	/// </summary>
	public static class FMODLogger 	{
		//TYPE_MEMORY logs too often and isn't particularily interesting
		public static DEBUG_FLAGS log_flags = ALL & ~DEBUG_FLAGS.TYPE_MEMORY;

		const DEBUG_FLAGS ALL = DEBUG_FLAGS.ERROR | DEBUG_FLAGS.WARNING | DEBUG_FLAGS.LOG | DEBUG_FLAGS.TYPE_MEMORY | DEBUG_FLAGS.TYPE_FILE | DEBUG_FLAGS.TYPE_CODEC | DEBUG_FLAGS.TYPE_TRACE | DEBUG_FLAGS.DISPLAY_TIMESTAMPS | DEBUG_FLAGS.DISPLAY_LINENUMBERS | DEBUG_FLAGS.DISPLAY_THREAD;

		public static bool Initialize() {
			return Debug.Initialize(ALL, DEBUG_MODE.CALLBACK, DEBUG_CALLBACK) == RESULT.OK;
		}

		delegate void UnityLogMethod(object message);

		private static RESULT DEBUG_CALLBACK(DEBUG_FLAGS flags, StringWrapper file, int line, StringWrapper func, StringWrapper message)
		{
			if ((flags & log_flags) != 0) {
				var logSeverity = flags & ~(DEBUG_FLAGS.ERROR | DEBUG_FLAGS.WARNING | DEBUG_FLAGS.LOG);

				UnityLogMethod unityLogMethod;

				switch (logSeverity)
				{
					case DEBUG_FLAGS.ERROR:
						unityLogMethod = UnityEngine.Debug.LogError;
						break;
					case DEBUG_FLAGS.WARNING:
						unityLogMethod = UnityEngine.Debug.LogWarning;
						break;
					case DEBUG_FLAGS.LOG:
					default:
						unityLogMethod = UnityEngine.Debug.Log;
						break;
				}

				var logStringBuilder = new StringBuilder();

				logStringBuilder
					.AppendLine(flags.ToString())
					.AppendLine($"{line}: {(string)file}: {(string)func}")
					.AppendLine((string)message)
					;
			}

			return RESULT.OK;
		}
	}
}