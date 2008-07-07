using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Mono.Debugger.Frontend
{
	internal class MyTextReader : TextReader
	{
		bool closed = false;
		string current_line = null;
		int pos = 0;

		public string Text {
			set {
				if (closed)
					throw new InvalidOperationException ("Reader is closed.");

				pos = 0;
				current_line = value;
			}
		}

		bool check_line ()
		{
			if (closed || (current_line == null))
				return false;

			if (pos >= current_line.Length) {
				current_line = null;
				return false;
			}

			return true;
		}

		public override int Peek ()
		{
			if (!check_line ())
				return -1;

			return current_line [pos];
		}

		public override int Read ()
		{
			if (!check_line ())
				return -1;

			return current_line [pos++];
		}

		public override string ReadLine ()
		{
			string retval;

			if (!check_line ())
				return String.Empty;

			retval = current_line;
			current_line = null;
			return retval;
		}

		public override string ReadToEnd ()
		{
			return ReadLine ();
		}

		public override void Close ()
		{
			current_line = null;
			closed = true;
			base.Close ();
		}
	}
}
