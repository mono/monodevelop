//
// ConsoleProgressStatus.cs
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

namespace Mono.Addins
{
	public class ConsoleProgressStatus: MarshalByRefObject, IProgressStatus
	{
		bool canceled;
		int logLevel;
		
		public ConsoleProgressStatus (bool verboseLog)
		{
			if (verboseLog)
				logLevel = 2;
			else
				logLevel = 1;
		}
		
		public ConsoleProgressStatus (int logLevel)
		{
			this.logLevel = logLevel;
		}
		
		public void SetMessage (string msg)
		{
		}
		
		public void SetProgress (double progress)
		{
		}
		
		public void Log (string msg)
		{
			Console.WriteLine (msg);
		}
		
		public void ReportWarning (string message)
		{
			if (logLevel > 0)
				Console.WriteLine ("WARNING: " + message);
		}
		
		public void ReportError (string message, Exception exception)
		{
			if (logLevel == 0)
				return;
			Console.Write ("ERROR: ");
			if (logLevel > 1) {
				if (message != null)
					Console.WriteLine (message);
				if (exception != null)
					Console.WriteLine (exception);
			} else {
				if (message != null && exception != null)
					Console.WriteLine (message + " (" + exception.Message + ")");
				else {
					if (message != null)
						Console.WriteLine (message);
					if (exception != null)
						Console.WriteLine (exception.Message);
				}
			}
		}
		
		public bool IsCanceled {
			get { return canceled; }
		}
		
		public int LogLevel {
			get { return logLevel; }
		}
		
		public void Cancel ()
		{
			canceled = true;
		}
	}
}

