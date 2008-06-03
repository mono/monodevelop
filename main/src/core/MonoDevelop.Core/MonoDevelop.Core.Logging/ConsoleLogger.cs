// 
// ConsoleLogger.cs
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

namespace MonoDevelop.Core.Logging
{
	
	public class ConsoleLogger : ILogger
	{
		EnabledLoggingLevel enabledLevel = EnabledLoggingLevel.UpToWarn;
		bool useColour = false;
		
		public ConsoleLogger ()
		{
		}

		public void Log (LogLevel level, string message)
		{
			string header;
			
			switch (level) {
			case LogLevel.Fatal:
				header = GettextCatalog.GetString ("FATAL ERROR");
				if (useColour) {
					ConsoleCrayon.ForegroundColor = ConsoleColor.Yellow;
					ConsoleCrayon.BackgroundColor = ConsoleColor.Red;
				}
				break;
			case LogLevel.Error:
				header = GettextCatalog.GetString ("ERROR");
				if (useColour) {
					ConsoleCrayon.ForegroundColor = ConsoleColor.Red;
					ConsoleCrayon.BackgroundColor = ConsoleColor.Yellow;
				}
				break;
			case LogLevel.Warn:
				header = GettextCatalog.GetString ("WARNING");
				if (useColour) {
					ConsoleCrayon.ForegroundColor = ConsoleColor.Red;
				}
				break;
			case LogLevel.Info:
				header = GettextCatalog.GetString ("INFO");
				if (useColour) {
					ConsoleCrayon.ForegroundColor = ConsoleColor.Green;
				}
				break;
			case LogLevel.Debug:
				header = GettextCatalog.GetString ("DEBUG");
				if (useColour) {
					ConsoleCrayon.ForegroundColor = ConsoleColor.Blue;
				}
				break;
			default:
				header = GettextCatalog.GetString ("LOG");
				break;
			}
			
			Console.Write ("{0} [{1}]:", header, DateTime.Now.ToString ("u"));
			if (useColour) ConsoleCrayon.ResetColor ();
			Console.WriteLine (" " + message);
		}
		
		public EnabledLoggingLevel EnabledLevel {
			get { return enabledLevel; }
			set { enabledLevel = value; }
		}

		public string Name {
			get { return "ConsoleLogger"; }
		}
		
		public bool UseColour {
			get { return useColour; }
			set {
				useColour = false;
				if (value) try {
					ConsoleCrayon.ForegroundColor = ConsoleColor.Red;
					ConsoleCrayon.ResetColor ();
					useColour = true;
				} catch { }
			}
		}
	}
}
