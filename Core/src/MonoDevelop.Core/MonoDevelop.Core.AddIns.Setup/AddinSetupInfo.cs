//
// AddinSetupInfo.cs
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
using System.Collections;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Specialized;

namespace MonoDevelop.Core.AddIns.Setup
{
	public class AddinSetupInfo
	{
		AddinInfo addin;
		string configFile;
		string directory;
		
		public AddinSetupInfo (string file)
		{
			configFile = file;
			directory = Path.GetDirectoryName (file);
			using (StreamReader s = new StreamReader (file)) {
				addin = AddinInfo.ReadFromAddinFile (s);
			}
		}
		
		public AddinInfo Addin {
			get { return addin; }
		}
		
		public bool Enabled {
			get { return Runtime.SetupService.IsAddinEnabled (addin.Id); }
			set {
				if (value)
					Runtime.SetupService.EnableAddin (addin.Id);
				else
					Runtime.SetupService.DisableAddin (addin.Id);
			}
		}
		
		public string ConfigFile {
			get { return configFile; }
		}
		
		public string Directory {
			get { return directory; }
		}
		
		public bool IsUserAddin {
			get { return ConfigFile.StartsWith (Environment.GetFolderPath (Environment.SpecialFolder.Personal)); }
		}
		
		public AddinConfiguration GetConfiguration ()
		{
			return AddinConfiguration.Read (ConfigFile);
		}
	}
}
