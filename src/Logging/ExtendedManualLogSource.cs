using BepInEx.Logging;
using System;
using System.Runtime.CompilerServices;

namespace Receiver2ModdingKit.Logging
{
	public class ExtendedManualLogSource : ManualLogSource
	{
		public ExtendedManualLogSource(string sourceName) : base(sourceName) //what
		{
			Logger.Sources.Add(this);
		}

		/// <inheritdoc />
		public new event EventHandler<LogEventArgs> LogEvent;

		/// <summary>
		///     Logs a message with the specified log level.
		/// </summary>
		/// <param name="level">Log levels to attach to the message. Multiple can be used with bitwise ORing.</param>
		/// <param name="data">Data to log.</param>
		public new void Log(LogLevel level, object data) => LogEvent?.Invoke(this, new LogEventArgs(data, level, this));

		public void LogWithColor(LogLevel level, object data, ConsoleColor color) => LogEvent?.Invoke(this, new ExtendedLogEventArgs(data, level, this, color));

		public void LogWithLevelName(LogLevel level, object data, string levelName) => LogEvent?.Invoke(this, new ExtendedLogEventArgs(data, level, this, levelName));

		/// <summary>
		/// Logs a message with <see cref="LogLevel.Fatal"/> level.
		/// </summary>
		/// <param name="data">Data to log.</param>
		public new void LogFatal(object data) => Log(LogLevel.Fatal, data);

		/// <summary>
		/// Logs a message with <see cref="LogLevel.Fatal"/> level.
		/// </summary>
		/// <param name="data">Data to log.</param>
		public void LogFatalWithColor(object data, ConsoleColor color) => LogWithColor(LogLevel.Fatal, data, color);

		public void LogFatalWithLevelName(object data, string levelName) => LogEvent?.Invoke(this, new ExtendedLogEventArgs(data, LogLevel.Fatal, this, levelName));

		/// <summary>
		/// Logs a message with <see cref="LogLevel.Error"/> level.
		/// </summary>
		/// <param name="data">Data to log.</param>
		public new void LogError(object data) => Log(LogLevel.Error, data);

		/// <summary>
		/// Logs a message with <see cref="LogLevel.Error"/> level.
		/// </summary>
		/// <param name="data">Data to log.</param>
		public void LogErrorWithColor(object data, ConsoleColor color) => LogWithColor(LogLevel.Error, data, color);
		
		public void LogErrorWithLevelName(object data, string levelName) => LogEvent?.Invoke(this, new ExtendedLogEventArgs(data, LogLevel.Error, this, levelName));

		/// <summary>
		/// Logs a message with <see cref="LogLevel.Warning"/> level.
		/// </summary>
		/// <param name="data">Data to log.</param>
		public new void LogWarning(object data) => Log(LogLevel.Warning, data);

		/// <summary>
		/// Logs a message with <see cref="LogLevel.Warning"/> level.
		/// </summary>
		/// <param name="data">Data to log.</param>
		public void LogWarningWithColor(object data, ConsoleColor color) => LogWithColor(LogLevel.Warning, data, color);

		public void LogWarningWithLevelName(object data, string levelName) => LogEvent?.Invoke(this, new ExtendedLogEventArgs(data, LogLevel.Warning, this, levelName));

		/// <summary>
		/// Logs a message with <see cref="LogLevel.Message"/> level.
		/// </summary>
		/// <param name="data">Data to log.</param>
		public new void LogMessage(object data) => Log(LogLevel.Message, data);

		/// <summary>
		/// Logs a message with <see cref="LogLevel.Message"/> level.
		/// </summary>
		/// <param name="data">Data to log.</param>
		public void LogMessageWithColor(object data, ConsoleColor color) => LogWithColor(LogLevel.Message, data, color);

		public void LogMessageWithLevelName(object data, string levelName) => LogEvent?.Invoke(this, new ExtendedLogEventArgs(data, LogLevel.Message, this, levelName));

		/// <summary>
		/// Logs a message with <see cref="LogLevel.Info"/> level.
		/// </summary>
		/// <param name="data">Data to log.</param>
		public new void LogInfo(object data) => Log(LogLevel.Info, data);

		/// <summary>
		/// Logs a message with <see cref="LogLevel.Info"/> level.
		/// </summary>
		/// <param name="data">Data to log.</param>
		public void LogInfoWithColor(object data, ConsoleColor color) => LogWithColor(LogLevel.Info, data, color);

		public void LogInfoWithLevelName(object data, string levelName) => LogEvent?.Invoke(this, new ExtendedLogEventArgs(data, LogLevel.Info, this, levelName));

		/// <summary>
		/// Logs a message with <see cref="LogLevel.Debug"/> level.
		/// </summary>
		/// <param name="data">Data to log.</param>
		public new void LogDebug(object data) => Log(LogLevel.Debug, data);

		/// <summary>
		/// Logs a message with <see cref="LogLevel.Debug"/> level.
		/// </summary>
		/// <param name="data">Data to log.</param>
		public void LogDebugWithColor(object data, ConsoleColor color) => LogWithColor(LogLevel.Debug, data, color);

		public void LogDebugWithLevelName(object data, string levelName) => LogEvent?.Invoke(this, new ExtendedLogEventArgs(data, LogLevel.Debug, this, levelName));
	}
}
