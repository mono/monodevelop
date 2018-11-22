//
// ConsoleProgressMonitor.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;

namespace MonoDevelop.Core.ProgressMonitoring
{
	public class ConsoleProgressMonitor: ProgressMonitor
	{
		int columns = 0;
		bool leaveOpen;
		bool indent = true;
		bool wrap = false;
		int ilevel = 0;
		int isize = 3;
		int col = -1;
		bool ignoreLogMessages;
		TextWriter writer;
		
		string TimeStamp {
			get { return EnableTimeStamp ? string.Format ("[{0}] ", DateTime.Now.ToString ("yyyy-MM-dd HH:mm:ss.f")) : string.Empty; }
		}
		
		public ConsoleProgressMonitor () : this (Console.Out, true)
		{
			//TODO: can we efficiently update Console.WindowWidth when it changes?
			//when the output is redirected, Mono returns 0 but .NET throws IOException
			if (Console.IsOutputRedirected)
				columns = 0;
			else
				columns = Console.WindowWidth;

			wrap = columns > 0;
		}

		public ConsoleProgressMonitor (TextWriter writer, bool leaveOpen)
		{
			this.writer = writer;
			this.leaveOpen = leaveOpen;
		}

		public ConsoleProgressMonitor (TextWriter writer) : this (writer, false)
		{
		}

		protected override void OnDispose (bool disposing)
		{
			if (!leaveOpen)
				writer.Dispose ();

			base.OnDispose (disposing);
		}
		
		public bool EnableTimeStamp {
			get; set;
		}
		
		public bool WrapText {
			get { return wrap; }
			set { wrap = value; }
		}
		
		public bool IgnoreLogMessages {
			get { return ignoreLogMessages; }
			set { ignoreLogMessages = value; }
		}
		
		public int WrapColumns {
			get { return columns; }
			set { columns = value; }
		}
		
		public bool IndentTasks {
			get { return indent; }
			set { indent = value; }
		}
		
		protected override void OnBeginTask (string name, int totalWork, int stepWork)
		{
			if (!ignoreLogMessages) {
				WriteText (name);
				Indent ();
			}
		}
		
		protected override void OnEndTask (string name, int totalWork, int stepWork)
		{
			if (!ignoreLogMessages)
				Unindent ();
		}

		protected override void OnWriteLog (string message)
		{
			if (!ignoreLogMessages)
				WriteText (message);
		}

		protected override void OnSuccessReported (string message)
		{
			WriteText (message + "\n");
		}

		protected override void OnWarningReported (string message)
		{
			WriteText ("WARNING: " + message + "\n");
		}

		protected override void OnErrorReported (string message, Exception ex)
		{
			WriteText ("ERROR: " + ErrorHelper.GetErrorMessage (message, ex) + "\n");
		}
		
		void WriteText (string text)
		{
			if (indent)
				WriteText (text, ilevel);
			else
				WriteText (text, 0);
		}

		void WriteText (string text, int leftMargin)
		{
			if (text == null)
				return;
			
			string timestamp = TimeStamp;
			int maxCols = wrap ? columns - timestamp.Length : int.MaxValue;
			int n = 0;

			while (n < text.Length)
			{
				if (col == -1) {
					writer.Write (timestamp + new string (' ', leftMargin));
					col = leftMargin;
				}
				
				int lastWhite = -1;
				int sn = n;
				bool eol = false;

				while (col < maxCols && n < text.Length) {
					char c = text [n];
					if (c == '\r') {
						n++;
						continue;
					}
					if (c == '\n') {
						eol = true;
						break;
					}
					if (char.IsWhiteSpace (c))
						lastWhite = n;
					col++;
					n++;
				}

				if (lastWhite == -1 || col < maxCols)
					lastWhite = n;
				else if (col >= maxCols)
					n = lastWhite + 1;

				writer.Write (text.Substring (sn, lastWhite - sn));
				
				if (eol || col >= maxCols) {
					col = -1;
					writer.WriteLine ();
					if (eol) n++;
				}
			}
		}
		
		void Indent ()
		{
			ilevel += isize;
			if (col != -1) {
				writer.WriteLine ();
				col = -1;
			}
		}
		
		void Unindent ()
		{
			ilevel -= isize;
			if (ilevel < 0) ilevel = 0;
			if (col != -1) {
				writer.WriteLine ();
				col = -1;
			}
		}
	}
}
