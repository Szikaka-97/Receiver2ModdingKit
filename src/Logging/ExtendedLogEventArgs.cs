using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Receiver2ModdingKit.Logging
{
	public class ExtendedLogEventArgs : LogEventArgs
	{
		public ExtendedLogEventArgs(object data, LogLevel level, ILogSource source, ConsoleColor consoleColor, string levelName = null) : base(data, level, source)
		{
			this.ConsoleColor = consoleColor;
			this.LevelName = levelName;
		}

		public ExtendedLogEventArgs(object data, LogLevel level, ILogSource source, string levelName = null) : base(data, level, source)
		{
			this.LevelName = levelName;
		}

		public ConsoleColor ConsoleColor { get; protected set; }

		public string LevelName { get; protected set; }

		/// <inheritdoc />
		public override string ToString() => string.Format("[{0,-7}:{1,10}] {2}", LevelName ?? Enum.GetName(typeof(LogLevel), Level), this.Source.SourceName, this.Data);
	}
}
