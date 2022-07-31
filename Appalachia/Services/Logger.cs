using Appalachia.Extensions;
using Discord;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Appalachia.Services
{
	public class Logger
	{
		// This thing is probably not very good, but I'm gonna use it anyway because I am stubborn and don't want to use another package -Jolkert 2021-05-06
		// lol im just copying it from dexbot. above comment still applies -jolk 2022-01-02
		// it should be a bit better and make a bit more sense now -jolk 2022-06-16
		// now we're gettings somewhere. this might actually be good now? I doubt it but at least it works much better than before -jolk 2022-07-26

		private static readonly object __streamLock = new object();
		private static readonly object __writeLock = new object();

		public bool ShouldFileLog { get; }

		private readonly ConsoleColor _defaultColor;
		private string _logFile;
		private FileStream _stream;
		private string _folderPath;
		private readonly Queue<LogMessage> _writeQueue; // We use the queue to prevent collisions

		private Thread _writeThread;

		public Logger(string folderPath, bool shouldFileLog = false)
		{
			_folderPath = folderPath;

			Console.ResetColor();
			_defaultColor = Console.ForegroundColor;
			ShouldFileLog = shouldFileLog;

			_writeQueue = new Queue<LogMessage>();
			if (shouldFileLog)
				StartStream();

			AppalachiaConsole.CommandInput += (string input) => _stream.Write(Encoding.UTF8.GetBytes($"> {input}\n"));
		}

		public void Info([DisallowNull] string message, [CallerMemberName] string source = "") => Log(new LogMessage(LogSeverity.Info, source, message));
		public void Warn([DisallowNull] string message, Exception excpetion = null, [CallerMemberName] string source = "") => Log(new LogMessage(LogSeverity.Warning, source, message, excpetion));
		public void Error([DisallowNull] string message, Exception exception = null, [CallerMemberName] string source = "") => Log(new LogMessage(LogSeverity.Error, source, message, exception));
		public void Critical([DisallowNull] string message, Exception exception = null, [CallerMemberName] string source = "") => Log(new LogMessage(LogSeverity.Critical, source, message, exception));
		public void Verbose([DisallowNull] string message, [CallerMemberName] string source = "") => Log(new LogMessage(LogSeverity.Verbose, source, message));
		public void Debug([DisallowNull] string message, [CallerMemberName] string source = "")
		{
#if DEBUG
			Log(new LogMessage(LogSeverity.Debug, source, message));
#endif
		}

		// object accepters
		public void Info(object obj, [CallerMemberName] string source = "") => Info(obj.ToStringAllowNull(), source);
		public void Warn(object obj, Exception exception = null, [CallerMemberName] string source = "") => Warn(obj.ToStringAllowNull(), exception, source);
		public void Error(object obj, Exception exception = null, [CallerMemberName] string source = "") => Error(obj.ToStringAllowNull(), exception, source);
		public void Critical(object obj, Exception exception = null, [CallerMemberName] string source = "") => Critical(obj.ToStringAllowNull(), exception, source);
		public void Verbose(object obj, [CallerMemberName] string source = "") => Verbose(obj.ToStringAllowNull(), source);
		public void Debug(object obj, [CallerMemberName] string source = "")
		{
#if DEBUG
			Debug(obj.ToStringAllowNull(), source);
#endif
		}


		public void Log(LogMessage message)
		{
#if !DEBUG
			if (message.Severity == LogSeverity.Debug)
				return;
#endif
			_writeQueue.Enqueue(message);
			if (_writeThread == null || _writeThread.ThreadState == ThreadState.Stopped)
				StartWriteThread();
		}
		private void LogFromQueue(LogMessage message)
		{
			lock (__writeLock) // if you dont do this things will be the wrong color -jolk 2022-07-26
			{
				if (message.Message == null) // if you don't do *this* random critical-severity logs with null messages and null sources will be printed. no idea how it happens but its fine -jolk 2022-07-26
					return;

				string write = $"{string.Format("{0, -10}", $"[{message.Severity}]")} {message}";

				AppalachiaConsole.OutputColor = message.Severity switch
				{
					LogSeverity.Info => ConsoleColor.White,
					LogSeverity.Debug => ConsoleColor.Magenta,
					LogSeverity.Warning => ConsoleColor.Yellow,
					LogSeverity.Error => ConsoleColor.Red,
					LogSeverity.Critical => ConsoleColor.DarkRed,
					LogSeverity.Verbose => ConsoleColor.Green,
					_ => _defaultColor
				};
				AppalachiaConsole.WriteLine(write);

				if (ShouldFileLog)
					_stream.Write(Encoding.UTF8.GetBytes($"{write}\n"));
			}
		}
		private void WriteAllFromQueue()
		{
			while (_writeQueue.Count > 0)
				LogFromQueue(_writeQueue.Dequeue());
			if (ShouldFileLog)
				lock (__streamLock) _stream.Flush();
		}

		public void Close()
		{
			_writeThread?.Join();
			_stream.Close();
		}
		public void Restart()
		{
			Close();
			StartStream();
		}

		private void StartStream()
		{
			if (!Directory.Exists(_folderPath))
				Directory.CreateDirectory(_folderPath);

			DateTime today = DateTime.Today.ToLocalTime();
			string fileName = $"{today:yyyy-MM-dd}";
			if (File.Exists($"{_folderPath}/{fileName}.log"))
			{
				int i = 1;
				while (File.Exists($"{_folderPath}/{fileName}_{i}.log"))
					i++;

				fileName += $"_{i}";
			}

			_logFile = $"{_folderPath}/{fileName}.log";
			_stream = new FileStream(_logFile, FileMode.Append);

			_stream.Write(Encoding.UTF8.GetBytes($"Starting Log: {fileName} ({DateTime.Now:HH:mm:ss.fff})\n"));
			if (_writeQueue.Count > 0)
				StartWriteThread();
		}

		private void StartWriteThread()
		{
			_writeThread = new Thread(WriteAllFromQueue)
			{
				Name = "Logger"
			};
			_writeThread.Start();
		}
	}
}