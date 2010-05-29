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
using System.Xml.Linq;
using System.Linq;

namespace MonoDevelop.AspNet
{
	
	class RegistrationCache : FileInfoCache<RegistrationInfo>
	{
		AspNetAppProject project;
		RegistrationInfo machineRegistrationInfo;
		
		public RegistrationCache (AspNetAppProject project)
		{
			this.project = project;
			machineRegistrationInfo = GetMachineInfo ();
		}
		
		public IList<RegistrationInfo> GetInfosForPath (string webDirectory)
		{
			var infos = new List<RegistrationInfo> ();
			DirectoryInfo dir = new DirectoryInfo (webDirectory);
			string projectRootParent = new DirectoryInfo (project.BaseDirectory).Parent.FullName;
			while (dir != null && dir.FullName.Length > projectRootParent.Length && dir.FullName != projectRootParent) {
				string configPath = Path.Combine (dir.FullName, "web.config");
				try {
					if (!File.Exists (configPath))
						configPath = Path.Combine (dir.FullName, "Web.config");
					if (File.Exists (configPath))
						infos.Add (Get (configPath));
					dir = dir.Parent;
				} catch (IOException ex) {
					MonoDevelop.Core.LoggingService.LogError ("Error querying web.config file '" + configPath + "'", ex);
				}
			}
			infos.Add (machineRegistrationInfo);
			return infos;
		}
		
		protected override RegistrationInfo GenerateInfo (string filename)
		{
			var info = new RegistrationInfo ();
			using (var reader = new XmlTextReader (filename) { WhitespaceHandling = WhitespaceHandling.None }) {
				var doc = XDocument.Load (reader);
				
				if (doc.Root.Name != "configuration")
					return info;
				
				var systWeb = doc.Root.Element ("system.web");
				if (systWeb == null)
					return info;
				
				var pages = systWeb.Element ("pages");
				if (pages != null) {
					var controls = pages.Element ("controls");
					if (controls != null) {
						foreach (var element in controls.Elements ()) {
							bool add = element.Name == "add";
							if (add || element.Name == "remove")
								info.Controls.Add (new ControlRegistration (filename, add,
									(string) element.Attribute ("tagPrefix"),
									(string) element.Attribute ("namespace"),
									(string) element.Attribute ("assembly"),
									(string) element.Attribute ("tagName"),
									(string) element.Attribute ("src")));
						}
					}
					var namespaces = pages.Element ("namespaces");
					if (namespaces != null) {
						foreach (var element in namespaces.Elements ()) {
							bool add = element.Name == "add";
							if (add || element.Name == "remove")
								info.Namespaces.Add (new NamespaceRegistration (add, (string) element.Attribute ("namespace")));
						}
					}
				}
				
				var compilation = systWeb.Element ("compilation");
				if (compilation != null) {
					var assemblies = compilation.Element ("assemblies");
					if (assemblies != null) {
						foreach (var element in assemblies.Elements ()) {
							bool add = element.Name == "add";
							if (add || element.Name == "remove")
								info.Assemblies.Add (new AssemblyRegistration (add, (string) element.Attribute ("assembly")));
						}
					}
				}
			}
			return info;
		}
		
		public IEnumerable<ControlRegistration> GetControlsForPath (string path)
		{
			//FIXME: handle removes as well as adds
			return GetInfosForPath (path).SelectMany (x => x.Controls).Where (c => c.Add);
		}
		
		public IEnumerable<string> GetAssembliesForPath (string path)
		{
			//FIXME: handle removes as well as adds
			return GetInfosForPath (path).SelectMany (x => x.Assemblies).Where (c => c.Add).Select (c => c.Name);
		}
		
		public IEnumerable<string> GetNamespacesForPath (string path)
		{
			//FIXME: handle removes as well as adds
			return GetInfosForPath (path).SelectMany (x => x.Namespaces).Where (c => c.Add).Select (c => c.Namespace);
		}
		
		//FIXME: add more default values
		static RegistrationInfo GetMachineInfo ()
		{
			var info = new RegistrationInfo ();
			
			//see http://msdn.microsoft.com/en-us/library/eb44kack.aspx
			string[] defaultNamespaces = {
				"System",
				"System.Collections",
				"System.Collections.Specialized",
				"System.Configuration",
				"System.Text",
				"System.Text.RegularExpressions",
				"System.Web",
				"System.Web.Caching",
				"System.Web.Profile",
				"System.Web.Security",
				"System.Web.SessionState",
				"System.Web.UI",
				"System.Web.UI.HtmlControls",
				"System.Web.UI.WebControls",
				"System.Web.UI.WebControls.WebParts",
			};
			
			info.Namespaces.AddRange (defaultNamespaces.Select (ns => new NamespaceRegistration (true, ns)));
			
			return info;
		}
	}
	
	class RegistrationInfo
	{
		public RegistrationInfo ()
		{
			this.Controls = new List<ControlRegistration> ();
			this.Namespaces = new List<NamespaceRegistration> ();
			this.Assemblies = new List<AssemblyRegistration> ();
		}		
		
		public List<NamespaceRegistration> Namespaces { get; private set; }
		public List<ControlRegistration> Controls { get; private set; }
		public List<AssemblyRegistration> Assemblies { get; private set; }
	}
	
	class NamespaceRegistration
	{
		public NamespaceRegistration (bool add, string name)
		{
			this.Add = add;
			this.Namespace = name;
		}
		
		public bool Add { get; private set; }
		public string Namespace { get; private set; }
	}
	
	class AssemblyRegistration
	{
		public AssemblyRegistration (bool add, string name)
		{
			this.Add = add;
			this.Name = name;
		}
		
		public bool Add { get; private set; }
		public string Name { get; private set; }
	}
	
	class ControlRegistration
	{
		public bool Add { get; private set; }
		public string TagPrefix { get; private set; }
		public string Namespace { get; private set; }
		public string Assembly { get; private set; }
		public string TagName { get; private set; }
		public string Source { get; private set; }
		public string ConfigFile { get; private set; }
		
		public bool IsAssembly {
			get {
				return !string.IsNullOrEmpty (Assembly) && !string.IsNullOrEmpty (TagPrefix) && !string.IsNullOrEmpty (Namespace);
			}
		}
		
		public bool IsUserControl {
			get {
				return !string.IsNullOrEmpty (TagName) && !string.IsNullOrEmpty (TagPrefix) && !string.IsNullOrEmpty (Source);
			}
		}
		
		public bool PrefixMatches (string prefix)
		{
			 return 0 == string.Compare (TagPrefix, prefix, StringComparison.OrdinalIgnoreCase);
		}
		
		public bool NameMatches (string name)
		{
			 return 0 == string.Compare (TagName, name, StringComparison.OrdinalIgnoreCase);
		}
		
		public ControlRegistration (string configFile, bool add, string tagPrefix, string _namespace, 
		                            string assembly, string tagName, string src)
		{
			Add = add;
			ConfigFile = configFile;
			TagPrefix = tagPrefix;
			Namespace = _namespace;
			Assembly = assembly;
			TagName = tagName;
			Source = src;
		}

	}
	
	//NOTE: not safe for multithreaded access
	public abstract class FileInfoCache<T> where T : class
	{
		Dictionary<string, CacheEntry<T>> cache = new Dictionary<string, CacheEntry<T>> ();
		
		protected T Get (string filename)
		{
			CacheEntry<T> cached;
			DateTime lastWriteUtc = File.GetLastWriteTimeUtc (filename);
			
			if (!cache.TryGetValue (filename, out cached) || lastWriteUtc <= cached.LastWriteUtc) {
				cache[filename] = cached = new CacheEntry<T> {
					LastWriteUtc = lastWriteUtc,
					Info = GenerateInfo (filename)
				};
			}
			
			return cached.Info;
		}
		
		public void Flush ()
		{
			foreach (string filename in new List<string> (cache.Keys))
				if (!File.Exists (filename) || cache[filename].LastWriteUtc < File.GetLastWriteTimeUtc (filename))
					cache.Remove (filename);
		}
		
		protected abstract T GenerateInfo (string filename);
		
		class CacheEntry<T>
		{
			public DateTime LastWriteUtc;
			public T Info;
		}
	}
}
