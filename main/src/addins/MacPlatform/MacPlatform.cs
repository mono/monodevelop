//
// MacPlatform.cs
//
// Author:
//   Geoff Norton  <gnorton@novell.com>
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

using MonoDevelop.Core.Gui;

namespace MonoDevelop.Platform
{
	public class MacPlatform : PlatformService
	{
		static Dictionary<string, string> mimemap;

		static MacPlatform () {
			mimemap = new Dictionary<string, string> ();
			LoadMimeMap ();
		}

		public override DesktopApplication GetDefaultApplication (string mimetype) {
			return new DesktopApplication ();
		}
		
		public override DesktopApplication [] GetAllApplications (string mimetype) {
			return new DesktopApplication [] {new DesktopApplication ()};
		}

		protected override string OnGetMimeTypeForUri (string uri)
		{
			FileInfo file = new FileInfo (uri);
			
			if (mimemap.ContainsKey (file.Extension))
				return mimemap [file.Extension];

			return null;
		}

		public override void ShowUrl (string url)
		{
			Process.Start (url);
		}

		public override string DefaultMonospaceFont {
			get { return "Lucida Grande 14"; }
		}
		
		public override string Name {
			get { return "OSX"; }
		}
		
		private static void LoadMimeMap () {
			// All recent Macs should have this file; if not we'll just die silently
			try {
				StreamReader reader = new StreamReader (File.OpenRead ("/etc/apache2/mime.types"));
				Regex mime = new Regex ("([a-zA-Z]+/[a-zA-z0-9+-_.]+)\t+([a-zA-Z]+)");
				string line;
				while ((line = reader.ReadLine ()) != null) {
					Match m = mime.Match (line);
					if (m.Success)
						mimemap ["." + m.Groups [2].Captures [0].Value] = m.Groups [1].Captures [0].Value; 
				}
			} catch (Exception ex){
				MonoDevelop.Core.LoggingService.LogError ("Could not load Apache mime database", ex);
			}
		}
	}
}
