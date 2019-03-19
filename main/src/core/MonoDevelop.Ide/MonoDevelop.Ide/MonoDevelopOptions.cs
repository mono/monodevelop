//
// IdeStartup.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2011 Xamarin Inc (http://xamarin.com)
// Copyright (C) 2005-2011 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using System.Collections.Generic;
using MonoDevelop.Ide.Extensions;

namespace MonoDevelop.Ide
{
	public class MonoDevelopOptions
	{
		MonoDevelopOptions ()
		{
			IpcTcp = (PlatformID.Unix != Environment.OSVersion.Platform);
			RedirectOutput = true;
		}

		Mono.Options.OptionSet GetOptionSet ()
		{
			return new Mono.Options.OptionSet {
				{ "no-splash", "Do not display splash screen (deprecated).", s => {} },
				{ "no-start-window", "Do not display start window", s => NoStartWindow = true },
				{ "ipc-tcp", "Use the Tcp channel for inter-process communication.", s => IpcTcp = true },
				{ "new-window", "Do not open in an existing instance of " + BrandingService.ApplicationName, s => NewWindow = true },
				{ "h|?|help", "Show help", s => ShowHelp = true },
				{ "perf-log", "Enable performance counter logging", s => PerfLog = true },
				{ "no-redirect", "Disable redirection of stdout/stderr to a log file", s => RedirectOutput = false },
			};
		}

		public static MonoDevelopOptions Parse (string [] args)
		{
			var opt = new MonoDevelopOptions ();
			var optSet = opt.GetOptionSet ();

			try {
				opt.RemainingArgs = optSet.Parse (args);
			} catch (Mono.Options.OptionException ex) {
				opt.Error = ex.ToString ();
			}

			if (opt.Error != null) {
				Console.WriteLine ("ERROR: {0}", opt.Error);
				Console.WriteLine ("Pass --help for usage information.");
			}

			if (opt.ShowHelp) {
				Console.WriteLine (BrandingService.ApplicationName + " " + BuildInfo.VersionLabel);
				Console.WriteLine ("Options:");
				optSet.WriteOptionDescriptions (Console.Out);
				const string openFileText = "      file.ext;line;column";
				Console.Write (openFileText);
				Console.Write (new string (' ', 29 - openFileText.Length));
				Console.WriteLine ("Opens a file at specified integer line and column");
			}

			return opt;
		}

		public bool NoStartWindow { get; set; }
		public bool IpcTcp { get; set; }
		public bool NewWindow { get; set; }
		public bool ShowHelp { get; set; }
		public bool PerfLog { get; set; }
		public bool RedirectOutput { get; set; }
		public string Error { get; set; }
		public IList<string> RemainingArgs { get; set; }
		public IdeCustomizer IdeCustomizer { get; set; }
	}
}
