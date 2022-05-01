using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Appalachia.Services
{
	public class Logger
	{// This thing is probably not very good, but I'm gonna use it anyway because I am stubborn and don't want to use another package -Jolkert 2021-05-06
	 // lol im just copying it from dexbot. above comment still applies -jolk 2022-01-02
		private string _logFile;
		private FileStream _stream;
		private string _folderPath = "Resources/logs";

		private readonly Queue<string> _writeQueue; // We use the queue to prevent collisions
		private readonly ThreadStart _writeThreadStart;
		private Thread _writeThread;

		public Logger()
		{
			_writeThreadStart = new ThreadStart(() =>
			{
				while (_writeQueue.Count > 0)
					LogToFileFromQueue();
			});
			_writeQueue = new Queue<string>();
			StartStream();
		}

		public void LogToFile(string log)
		{
			_writeQueue.Enqueue(log);
			if (_writeThread == null || _writeThread.ThreadState == ThreadState.Stopped)
			{
				_writeThread = new Thread(_writeThreadStart);
				_writeThread.Start();
			}
		}
		private void LogToFileFromQueue()
		{
			_stream.Write(Encoding.UTF8.GetBytes($"{_writeQueue.Dequeue()}\n"));
			RestartStream();
		}

		public void Close()
		{
			_writeThread.Join();
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

			_stream.Write(Encoding.UTF8.GetBytes($"Starting Log: {fileName}\n"));
		}
		private void RestartStream()
		{
			_stream.Close();
			_stream = new FileStream(_logFile, FileMode.Append);
		}
	}
}
