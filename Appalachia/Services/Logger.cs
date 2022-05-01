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
		private bool _isOpen;

		public Logger()
		{
			_isOpen = false;
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
			if (_isOpen && (_writeThread == null || _writeThread.ThreadState == ThreadState.Stopped))
				StartWriteThread();
		}
		private void LogToFileFromQueue()
		{
			if (_isOpen)
				_stream.Write(Encoding.UTF8.GetBytes($"{_writeQueue.Dequeue()}\n"));
			RestartStream();
		}

		public void Close()
		{
			_isOpen = false;
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
			_isOpen = true;

			_stream.Write(Encoding.UTF8.GetBytes($"Starting Log: {fileName} ({DateTime.Now:HH:mm:ss.fff})\n"));
			if (_writeQueue.Count > 0)
				StartWriteThread();
		}
		private void RestartStream()
		{
			_isOpen = false;
			_stream.Close();

			// so like. this try block is a *really* stupid solution, but im sure it wont cause any problems at all right? -jolk 2022-05-01
			try
			{
				_stream = new FileStream(_logFile, FileMode.Append);
			}
			catch (IOException) { }

			_isOpen = true;
			if (_writeQueue.Count > 0)
				StartWriteThread();
		}

		private void StartWriteThread()
		{
			_writeThread = new Thread(_writeThreadStart);
			_writeThread.Start();
		}
	}
}
