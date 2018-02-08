// 
// FileLogger.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
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

namespace MonoDevelop.Core.Logging
{
	
	public class FileLogger : ILogger, IDisposable
	{
		TextWriter writer;
		string name;
		EnabledLoggingLevel enabledLevel = EnabledLoggingLevel.UpToInfo;
		
		public FileLogger (string filename)
			: this (filename, false)
		{
		}
		
		public FileLogger (string filename, bool append)
		{
			writer = new StreamWriter (filename, append) {
				AutoFlush = true	
			};
			name = filename;
		}

		public void Log (LogLevel level, string message)
		{
			string header;
			
			switch (level) {
			case LogLevel.Fatal:
				header = "FATAL ERROR";
				break;
			case LogLevel.Error:
				header = "ERROR";
				break;
			case LogLevel.Warn:
				header = "WARNING";
				break;
			case LogLevel.Info:
				header = "INFO";
				break;
			case LogLevel.Debug:
				header = "DEBUG";
				break;
			default:
				header = "LOG";
				break;
			}
			
			writer.WriteLine ("{0}[{1}]: {2}", header, DateTime.Now.ToString ("u"), message);
		}
		
		public EnabledLoggingLevel EnabledLevel {
			get { return enabledLevel; }
			set { enabledLevel = value; }
		}

		public string Name {
			get { return name; }
			set { name = value; }
		}
		
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected void Dispose (bool disposing)
		{
			if (disposing && writer != null) {
				writer.Dispose ();
				writer = null;
			}
		}
		
		~FileLogger ()
		{
			Dispose (false);
		}
	}
}
