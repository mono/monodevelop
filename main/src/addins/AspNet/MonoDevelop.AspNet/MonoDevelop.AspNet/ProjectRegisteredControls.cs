// 
// ProjectRegisteredControlList.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace MonoDevelop.AspNet
{
	
	class ProjectRegisteredControls
	{
		Dictionary<string,WebConfig> cache = new Dictionary<string, WebConfig> ();
		AspNetAppProject project;
		
		public ProjectRegisteredControls (AspNetAppProject project)
		{
			this.project = project;
		}
		
		public IList<RegistrationInfo> GetInfosForPath (string webDirectory)
		{
			List<RegistrationInfo> infos = new List<RegistrationInfo> ();
			DirectoryInfo dir = new DirectoryInfo (webDirectory);
			string projectRootParent = new DirectoryInfo (project.BaseDirectory).Parent.FullName;
			while (dir != null && dir.FullName.Length > projectRootParent.Length && dir.FullName != projectRootParent)
			{
				string configPath = Path.Combine (dir.FullName, "web.config");
				try {
					if (!File.Exists (configPath))
						configPath = Path.Combine (dir.FullName, "Web.config");
					if (File.Exists (configPath))
						infos.AddRange (GetInfosForFile (configPath));
					dir = dir.Parent;
				} catch (IOException ex) {
					MonoDevelop.Core.LoggingService.LogError ("Error querying web.config file '" + configPath + "'", ex);
				}
			}
			return infos;
		}
		
		IEnumerable<RegistrationInfo> GetInfosForFile (string filename)
		{
			WebConfig cached;
			DateTime lastWriteUtc = File.GetLastWriteTimeUtc (filename);
			if (cache.TryGetValue (filename, out cached) && lastWriteUtc <= cached.LastWriteUtc)
				return cached.Infos;
			
			cached = new WebConfig () { LastWriteUtc = lastWriteUtc };
			cached.Infos = LoadWebConfig (filename);
			cache[filename] = cached;
			return cached.Infos;
		}
		
		RegistrationInfo[] LoadWebConfig (string configFile)
		{
			List<RegistrationInfo> list = new List<RegistrationInfo> ();
			using (XmlTextReader reader = new XmlTextReader (configFile))
			{
				reader.WhitespaceHandling = WhitespaceHandling.None;
				reader.MoveToContent();
				if (reader.Name == "configuration"
				    && reader.ReadToDescendant ("system.web") && reader.NodeType == XmlNodeType.Element
				    && reader.ReadToDescendant ("pages") && reader.NodeType == XmlNodeType.Element
					&& reader.ReadToDescendant ("controls") && reader.NodeType == XmlNodeType.Element
				    && reader.ReadToDescendant ("add") && reader.NodeType == XmlNodeType.Element) {
					do {
						list.Add (new RegistrationInfo (
							configFile,
							reader.GetAttribute ("tagPrefix"),
							reader.GetAttribute ("namespace"),
							reader.GetAttribute ("assembly"),
							reader.GetAttribute ("tagName"),
							reader.GetAttribute ("src")
						));
						//Console.WriteLine (list[list.Count -1]);
					} while (reader.ReadToNextSibling ("add"));
				}
			}
			return list.ToArray ();	
		}
		
		
		class WebConfig
		{
			public DateTime LastWriteUtc;
			public RegistrationInfo[] Infos;
		}
	}
	
	class RegistrationInfo
	{
		public string TagPrefix { get; set; }
		public string Namespace { get; set; }
		public string Assembly { get; set; }
		public string TagName { get; set; }
		public string Source { get; set; }
		public string ConfigFile { get; set; }
		
		public bool IsAssembly {
			get {
				return !string.IsNullOrEmpty (Assembly) && !String.IsNullOrEmpty (TagPrefix) && !string.IsNullOrEmpty (Namespace);
			}
		}
		
		public bool IsUserControl {
			get {
				return !string.IsNullOrEmpty (TagName) && !String.IsNullOrEmpty (TagPrefix) && !string.IsNullOrEmpty (Source);
			}
		}
		
		public bool PrefixMatches (string prefix)
		{
			 return 0 == string.Compare (TagPrefix, prefix, StringComparison.InvariantCultureIgnoreCase);
		}
		
		public bool NameMatches (string name)
		{
			 return 0 == string.Compare (TagName, name, StringComparison.InvariantCultureIgnoreCase);
		}
		
		public RegistrationInfo (string configFile, string tagPrefix, string _namespace, string assembly, string tagName, string src)
		{
			ConfigFile = configFile;
			TagPrefix = tagPrefix;
			Namespace = _namespace;
			Assembly = assembly;
			TagName = tagName;
			Source = src;
		}
		
		public override string ToString ()
		{
			return string.Format("[RegistrationInfo: TagPrefix={0}, Namespace={1}, Assembly={2}, TagName={3}, Source={4}, ConfigFile={5}]",
			                     TagPrefix, Namespace, Assembly, TagName, Source, ConfigFile);
		}

	}
}
