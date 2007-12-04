//
// ConsoleProgressMonitor.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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

namespace Mono.Addins.Setup.ProgressMonitoring
{
	internal class ConsoleProgressMonitor: NullProgressMonitor
	{
		int columns = 80;
		bool indent = true;
		bool wrap = true;
		int ilevel = 0;
		int isize = 3;
		int col = -1;
		int logLevel;
		LogTextWriter logger;
		
		public ConsoleProgressMonitor (): this (1)
		{
		}
		
		public ConsoleProgressMonitor (int logLevel)
		{
			this.logLevel = logLevel;
			logger = new LogTextWriter ();
			logger.TextWritten += new LogTextEventHandler (WriteLog);
		}
		
		public bool WrapText {
			get { return wrap; }
			set { wrap = value; }
		}
		
		public int WrapColumns {
			get { return columns; }
			set { columns = value; }
		}
		
		public bool IndentTasks {
			get { return indent; }
			set { indent = value; }
		}
		
		public override int LogLevel {
			get { return logLevel; }
		}
		
		public override void BeginTask (string name, int totalWork)
		{
			WriteText (name);
			Indent ();
		}
		
		public override void BeginStepTask (string name, int totalWork, int stepSize)
		{
			BeginTask (name, totalWork);
		}
		
		public override void EndTask ()
		{
			Unindent ();
		}
		
		void WriteLog (string text)
		{
			WriteText (text);
		}
		
		public override TextWriter Log {
			get { return logger; }
		}
		
		public override void ReportSuccess (string message)
		{
			WriteText (message);
		}
		
		public override void ReportWarning (string message)
		{
			if (logLevel != 0)
				WriteText ("WARNING: " + message + "\n");
		}
		
		public override void ReportError (string message, Exception ex)
		{
			if (logLevel == 0)
				return;
			
			if (message != null && ex != null) {
				WriteText ("ERROR: " + message + "\n");
				if (logLevel > 1)
					WriteText (ex + "\n");
			}
			if (message != null)
				WriteText ("ERROR: " + message + "\n");
			else if (ex != null) {
				if (logLevel > 1)
					WriteText ("ERROR: " + ex + "\n");
				else
					WriteText ("ERROR: " + ex.Message + "\n");
			}
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
			if (text == null || text.Length == 0)
				return;

			int n = 0;
			int maxCols = wrap ? columns : int.MaxValue;

			while (n < text.Length)
			{
				if (col == -1) {
					Console.Write (new String (' ', leftMargin));
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
				
				Console.Write (text.Substring (sn, lastWhite - sn));
				
				if (eol || col >= maxCols) {
					col = -1;
					Console.WriteLine ();
					if (eol) n++;
				}
			}
		}
		
		void Indent ()
		{
			ilevel += isize;
			if (col != -1) {
				Console.WriteLine ();
				col = -1;
			}
		}
		
		void Unindent ()
		{
			ilevel -= isize;
			if (ilevel < 0) ilevel = 0;
			if (col != -1) {
				Console.WriteLine ();
				col = -1;
			}
		}
	}
}
