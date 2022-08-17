using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

/* PLANS FOR THIS THING
 * 1. Actually implement user commands lol
 * 2. Some sort of prefix thing for the input field (to enable current 'directory' listing in servers for input field)
 * 3. Maybe try to make it properly respond to window resizes? mega pain in the ass since I have no idea how the cursor movement on resize works but I'm sure it's possible to figure out
 * 4. Make it less scuffed. I know for certain that I'll come back to this later and realize that it's garbage and do a big rewrite eventually current date: (2022-07-28)
 * 5. 
 */
namespace Appalachia.ConsoleInput
{
	public static class AppalachiaConsole
	{
		// this is a mess but it works. this was way more effort than it was worth given the minimal amount of things I plan on doing with this but i made it cause I could mostly -jolk 2022-07-28
		// there are probably ***many*** better ways to do this (a real GUI being the main one) but I really *dont* wanna do that so console manipulation it is :) -jolk 2022-07-28

		private static readonly object __lock = new object();

		private static bool _cancelled = false; // technically I *should* be using a cancellation token for this but w/e -jolk 2022-07-28
		private static LinkedList<string> _inputHistory = new LinkedList<string>();
		private static LinkedListNode<string> _currentNode = null;

		private static List<char> _inputLine = new List<char>();
		private static Thread _inputThread = new Thread(ReadInput) { Name = "Input" };

		private static CursorPos _lastOutputPos = CursorPos.Origin;
		private static CursorPos _inputPos = CursorPos.Origin;
		private static CursorPos _inputStart = CursorPos.Origin;

		private static readonly ConsoleColor _defaultColor = Console.ForegroundColor;
		public static ConsoleColor OutputColor { get; set; } = _defaultColor;
		public static ConsoleColor InputColor { get; set; } = _defaultColor;

		private static string _inputPrefix = "";
		public static string InputPrefix { get => _inputPrefix; set => UpdateInputPrefix(value); }

		public static bool EchoInput { get; set; } = true;

		public static bool StartRead()
		{
			_cancelled = false;

			if (_inputThread.ThreadState == ThreadState.Running)
				return false;

			if (_inputThread.ThreadState != ThreadState.Unstarted)
				_inputThread = new Thread(ReadInput) { Name = "Input" };

			_inputThread.Start();
			return true;
		}
		public static bool StopRead()
		{
			if (_inputThread.ThreadState != ThreadState.Running)
				return false;

			_cancelled = true;
			_inputThread.Join();

			return true;
		}
		private static void ReadInput()
		{
			while (!_cancelled)
			{
				ConsoleKeyInfo input = Console.ReadKey(true);

				switch (input.Key)
				{
					case ConsoleKey.Delete:
						if (_inputLine.Count > 0 && _inputPos.X < _inputLine.Count)
							_inputLine.RemoveAt(_inputPos.X - InputPrefix.Length);
						break;
					case ConsoleKey.LeftArrow:
						if (_inputPos > _inputStart)
							_inputPos <<= 1;
						Console.CursorLeft = _inputPos.X - InputPrefix.Length;
						break;
					case ConsoleKey.RightArrow:
						if (_inputPos.DistanceFrom(_inputStart) < _inputLine.Count)
							_inputPos >>= 1;
						Console.CursorLeft = _inputPos.X - InputPrefix.Length;
						break;
					case ConsoleKey.UpArrow:
						MoveNodeForward();
						break;
					case ConsoleKey.DownArrow:
						MoveNodeBackward();
						break;
				}

				if (input.Key != ConsoleKey.Delete)
				{
					switch (input.KeyChar)
					{
						case '\0':
							continue;
						case '\r':
						case '\n':
							PushLine();
							break;
						case '\b':
							if (_inputLine.Count > 0)
								_inputLine.RemoveAt(_inputPos.DistanceFrom(_inputStart) - 1);
							break;
						default:
							_inputLine.Insert(_inputPos.DistanceFrom(_inputStart), input.KeyChar);
							break;
					}
				}

				WriteToInput(input);
			}
		}
		private static void PushLine()
		{
			string inputString = _inputLine.Stringify(), inputTrimmed = inputString.Trim();
			_inputLine.Clear();
			ClearInput();

			if (EchoInput)
				Write($"{_inputPrefix}{inputString}\n", true);


			OnCommandInput(inputTrimmed);
			if (_inputHistory.First?.Value != inputTrimmed)
				_inputHistory.AddFirst(inputTrimmed);
			_currentNode = null;

			_inputStart += _lastOutputPos.Y - _inputStart.Y;
			_inputPos = _inputStart;
		}

		private static void MoveNodeForward()
		{
			if (_currentNode == null)
				_currentNode = _inputHistory.First;
			else if (_currentNode?.Next == null)
				return;
			else
				_currentNode = _currentNode.Next;

			UpdateNodeMove();
		}
		private static void MoveNodeBackward()
		{
			if (_currentNode == null)
				return;

			_currentNode = _currentNode.Previous;
			UpdateNodeMove();
		}
		private static void UpdateNodeMove()
		{
			int oldInputLength = _inputLine.Count;
			_inputLine = _currentNode?.Value.ToList() ?? new List<char>();
			UpdateInput(0, oldInputLength);
			_inputPos = _inputStart >> _inputLine.Count;
			Console.SetCursorPosition(_inputPos.X, _inputPos.Y);
		}

		private static void Write(string str, bool isInputEcho)
		{
			lock (__lock)
			{
				ClearInput();

				Console.ForegroundColor = isInputEcho ? InputColor : OutputColor;
				Console.SetCursorPosition(_lastOutputPos.X, _lastOutputPos.Y);
				Console.Write(str);
				_lastOutputPos = Console.GetCursorPosition();

				_inputPos += _lastOutputPos.Y - _inputPos.Y;
				_inputStart += _lastOutputPos.Y - _inputStart.Y;

				Console.ForegroundColor = InputColor;
				Console.SetCursorPosition(0, _inputStart.Y);
				Console.Write($"{InputPrefix}{_inputLine.Stringify()}");
				Console.SetCursorPosition(_inputPos.X, _inputPos.Y);

				Console.ResetColor();
			}
		}
		public static void Write(string str) => Write(str, false);
		public static void WriteLine(object input)
		{
			Write($"{input}\n");
		}

		private static void WriteToInput(ConsoleKeyInfo input)
		{
			lock (__lock)
			{
				int offset = _inputPos.X - InputPrefix.Length, overflow = 0;
				if (input.Key is ConsoleKey.Backspace or ConsoleKey.Delete && offset > 0) // pattern matching slaps wtf? -jolk 2022-07-24
				{
					offset--;
					overflow++;
				}

				UpdateInput(offset, overflow);

				if (input.KeyChar is not ('\n' or '\r') && input.Key != ConsoleKey.Delete)
				{
					if (input.KeyChar == '\b')
						_inputPos <<= _inputPos != _inputStart ? 1 : 0;
					else
						_inputPos >>= 1;
				}

				Console.SetCursorPosition(_inputPos.X, _inputPos.Y);
			}
		}

		private static void ClearInput()
		{
			CursorPos lastPos = Console.GetCursorPosition();

			Console.SetCursorPosition(_inputStart.X, _inputStart.Y);
			Console.Write(new string(' ', _inputLine.Count));

			Console.SetCursorPosition(lastPos.X, lastPos.Y);
		}
		private static void UpdateInput(int offset = 0, int overflow = 0)
		{
			CursorPos lastPos = Console.GetCursorPosition();

			CursorPos startPos = _inputStart >> offset;
			Console.SetCursorPosition(startPos.X, startPos.Y);
			Console.Write(new string(' ', _inputLine.Count - offset + overflow));

			Console.SetCursorPosition(startPos.X, startPos.Y);
			Console.ForegroundColor = InputColor;
			if (offset < 0)
			{
				Console.Write(InputPrefix);
				offset = 0;
			}
			Console.Write(_inputLine.Stringify()[offset..]);
			Console.ResetColor();

			Console.SetCursorPosition(lastPos.X, lastPos.Y);
		}
		private static void UpdateInputPrefix(string newPrefix)
		{
			_inputStart >>= newPrefix.Length - _inputPrefix.Length;
			_inputPos >>= newPrefix.Length - _inputPrefix.Length;
			Console.SetCursorPosition(_inputPos.X, _inputPos.Y);

			_inputPrefix = newPrefix;
			UpdateInput(-_inputStart.X);
		}

		public static void ResetColors()
		{
			InputColor = OutputColor = _defaultColor;
			Console.ResetColor();
		}

		public static event Action<string> CommandInput;
		private static void OnCommandInput(string args) => CommandInput?.Invoke(args);

		private static string Stringify(this List<char> list) => new string(list.ToArray());

		private readonly record struct CursorPos(int X, int Y)
		{
			public CursorPos((int x, int y) pos) : this(pos.x, pos.y) { }

			public static readonly CursorPos Origin = (0, 0);
			public CursorPos LineStart { get => new CursorPos(0, Y); }

			// i think this would make sense as a subtraction operator? like `pos1 - pos2 == pos1.DistanceFrom(pos2)` -jolk 2022-07-28
			// but also +- is also used to lineshift? though i think i could remove that cause I basically never use it and should be updating _inputPos with a shift from _inpustStart anyways -jolk 2022-07-28
			// TODO: consider that lol
			public int DistanceFrom(CursorPos otherPos)
			{
				return Console.BufferWidth * (Y - otherPos.Y) + (X - otherPos.X);
			}

			public override string ToString() => $"({X}, {Y})";

			public static CursorPos operator +(CursorPos left, int right) => new CursorPos(left.X, left.Y + right);
			public static CursorPos operator -(CursorPos left, int right) => new CursorPos(left.X, left.Y - right);

			public static CursorPos operator <<(CursorPos pos, int shift)
			{
				if (shift == 0)
					return pos;

				if (shift < 0)
					return pos >> -shift;

				int linesToMove = 0;
				while (pos.X - shift < 0)
				{
					linesToMove++;
					shift -= Console.BufferWidth;

					if (linesToMove > pos.Y)
						break;
				}

				if (linesToMove <= pos.Y)
					return new CursorPos(pos.X - shift, pos.Y - linesToMove);
				else
					return Origin;
			}
			public static CursorPos operator >>(CursorPos pos, int shift)
			{
				if (shift == 0)
					return pos;

				if (shift < 0)
					return pos << -shift;


				int linesToMove = 0;
				while (pos.X + shift >= Console.BufferWidth)
				{
					linesToMove++;
					shift -= Console.BufferWidth;
				}

				return new CursorPos(pos.X + shift, pos.Y + linesToMove);
			}

			public static bool operator <(CursorPos left, CursorPos right)
			{
				if (left.Y != right.Y)
					return left.Y < right.Y;
				else
					return left.X < right.X;
			}
			public static bool operator >(CursorPos left, CursorPos right)
			{
				if (left.Y != right.Y)
					return left.Y > right.Y;
				else
					return left.X > right.X;
			}

			public static bool operator <=(CursorPos left, CursorPos right) => left == right || left < right;
			public static bool operator >=(CursorPos left, CursorPos right) => left == right || left > right;

			public static implicit operator CursorPos((int x, int y) pos) => new CursorPos(pos.x, pos.y);
		}
	}
}