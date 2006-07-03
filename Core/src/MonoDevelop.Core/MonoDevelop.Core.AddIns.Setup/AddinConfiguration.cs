//
// AddinConfiguration.cs
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
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Specialized;

namespace MonoDevelop.Core.AddIns.Setup
{
	public class AddinConfiguration
	{
		XmlDocument configDoc;
		string configFile;
		AddinConfigurationModule mainModule;
		List<AddinConfigurationModule> optionalModules;
		
		public XmlDocument Content {
			get { return configDoc; }
		}
		
		public IEnumerable<string> AllFiles {
			get {
				foreach (string s in MainModule.AllFiles)
					yield return s;

				foreach (AddinConfigurationModule mod in OptionalModules) {
					foreach (string s in mod.AllFiles)
						yield return s;
				}
			}
		}
		
		public AddinConfigurationModule MainModule {
			get {
				if (mainModule == null) {
					if (configDoc.DocumentElement == null)
						CreateDefaultFile ();
					mainModule = new AddinConfigurationModule (configDoc.DocumentElement);
				}
				return mainModule;
			}
		}
		
		public IEnumerable<AddinConfigurationModule> OptionalModules {
			get {
				if (optionalModules == null) {
					optionalModules = new List<AddinConfigurationModule> ();
					foreach (XmlElement mod in GetRootElement ().SelectNodes ("Module")) {
						optionalModules.Add (new AddinConfigurationModule (mod));
					}
				}
				return optionalModules;
			}
		}
		
		public IEnumerable<AddinConfigurationModule> AllModules {
			get {
				yield return MainModule;
				foreach (AddinConfigurationModule mod in OptionalModules)
					yield return mod;
			}
		}
		
		XmlElement GetRootElement ()
		{
			if (configDoc.DocumentElement == null)
				CreateDefaultFile ();
			return configDoc.DocumentElement;
		}
		
		void CreateDefaultFile ()
		{
			configDoc.LoadXml (template);
		}
		
		public void Save ()
		{
			if (mainModule != null)
				mainModule.Save ();
			if (optionalModules != null) {
				foreach (AddinConfigurationModule mod in optionalModules)
					mod.Save ();
			}
			configDoc.Save (configFile);
		}
		
		public static void Check (string configFile)
		{
			Read (configFile, true);
		}
		
		public static AddinConfiguration Create (string configFile)
		{
			AddinConfiguration config = new AddinConfiguration ();
			config.configDoc = new XmlDocument ();
			config.configFile = configFile;
			config.CreateDefaultFile ();
			config.Save ();
			return config;
		}
		
		public static AddinConfiguration Read (string configFile)
		{
			return Read (configFile, false);
		}
		
		public static AddinConfiguration Read (string configFile, bool check)
		{
			AddinConfiguration config = new AddinConfiguration ();
			
			string tempFolder = Path.GetDirectoryName (configFile);
			try {
				config.configDoc = new XmlDocument ();
				config.configDoc.PreserveWhitespace = true;
				config.configDoc.Load (configFile);
				config.configDoc.PreserveWhitespace = false;
				config.configFile = configFile;
			} catch (Exception ex) {
				Console.WriteLine (ex);
				throw new InstallException ("The add-in configuration file is invalid.", ex);
			}
			
			foreach (string file in config.AllFiles) {
				string asmFile = Path.Combine (tempFolder, file);
				if (check && !File.Exists (asmFile))
					throw new InstallException ("The file '" + file + "' is referenced in the configuration file but it was not found in package.");
			}
			
			return config;
		}

		const string template = 		
			"<AddIn id	 = 'ID'\n" + 
			"       name	 = 'NAME'\n" + 
			"       author	 = 'Unknown author'\n" + 
			"       copyright = 'GPL'\n" + 
			"       url       = 'http://monodevelop.com'\n" + 
			"       description = ''\n" + 
			"	   category    = 'IDE extensions'\n" + 
			"       version   = '0.1.0'>\n" + 
			"\n" + 
			"	<Runtime>\n" + 
			"	</Runtime>\n" + 
			"\n" + 
			"	<Dependencies>\n" + 
			"	    <AddIn id='MonoDevelop.Core' version='0.10.0'/>\n" + 
			"	</Dependencies>\n" +
			"	\n" + 
			"	</AddIn>";
	}
	
	public class AddinConfigurationModule
	{
		XmlElement root;
		List<string> assemblies;
		List<string> dataFiles;
		List<PackageDependency> dependencies;
		
		public AddinConfigurationModule (XmlElement element)
		{
			root = element;
		}

		public IEnumerable<string> AllFiles {
			get {
				foreach (string s in Assemblies)
					yield return s;

				foreach (string d in DataFiles)
					yield return d;
			}
		}
		
		public IList<string> Assemblies {
			get {
				if (assemblies == null)
					InitCollections ();
				return assemblies;
			}
		}
		
		public IList<string> DataFiles {
			get {
				if (dataFiles == null)
					InitCollections ();
				return dataFiles;
			}
		}
		
		public IList<PackageDependency> Dependencies {
			get {
				if (dependencies == null) {
					dependencies = new List<PackageDependency> ();
					
					XmlNodeList elems = root.SelectNodes ("Runtime/Dependencies");
					foreach (XmlElement elem in elems) {
						if (elem.Name == "AddIn") {
							AddinDependency dep = new AddinDependency ();
							dep.AddinId = elem.GetAttribute ("id");
							string v = elem.GetAttribute ("version");
							if (v.Length > 0)
								dep.Version = v;
							dependencies.Add (dep);
						} else if (elem.Name == "Assembly") {
							AssemblyDependency dep = new AssemblyDependency ();
							dep.FullName = elem.GetAttribute ("name");
							dep.Package = elem.GetAttribute ("package");
						}
					}
				}
				return dependencies;
			}
		}
		
		internal void Save ()
		{
			if (assemblies != null || dataFiles != null) {
				XmlElement runtime = GetRuntimeElement ();
				
				while (runtime.FirstChild != null)
					runtime.RemoveChild (runtime.FirstChild);
					
				foreach (string s in assemblies) {
					XmlElement asm = root.OwnerDocument.CreateElement ("Import");
					asm.SetAttribute ("assembly", s);
					runtime.AppendChild (asm);
				}
				foreach (string s in dataFiles) {
					XmlElement asm = root.OwnerDocument.CreateElement ("Import");
					asm.SetAttribute ("file", s);
					runtime.AppendChild (asm);
				}
				runtime.AppendChild (root.OwnerDocument.CreateTextNode ("\n"));
			}
			
			// Save dependency information
			
			if (dependencies != null) {
				XmlElement deps = GetDependenciesElement ();
				while (deps.FirstChild != null)
					deps.RemoveChild (deps.FirstChild);

				foreach (PackageDependency dep in dependencies) {
					if (dep is AddinDependency) {
						AddinDependency adep = (AddinDependency) dep;
						XmlElement elem = root.OwnerDocument.CreateElement ("AddIn");
						elem.SetAttribute ("id", adep.AddinId);
						if (!string.IsNullOrEmpty (adep.Version))
							elem.SetAttribute ("version", adep.Version);
						deps.AppendChild (elem);
					} else if (dep is AssemblyDependency) {
						AssemblyDependency adep = (AssemblyDependency) dep;
						XmlElement elem = root.OwnerDocument.CreateElement ("Assembly");
						elem.SetAttribute ("name", adep.Name);
						if (!string.IsNullOrEmpty (adep.Package))
							elem.SetAttribute ("package", adep.Package);
						deps.AppendChild (elem);
					}
				}
				deps.AppendChild (root.OwnerDocument.CreateTextNode ("\n"));
			}
		}
		
		public void AddAssemblyReference (string id, string version)
		{
			XmlElement deps = GetDependenciesElement ();
			if (deps.SelectSingleNode ("AddIn[@id='" + id + "']") != null)
				return;
			
			XmlElement dep = root.OwnerDocument.CreateElement ("AddIn");
			dep.SetAttribute ("id", id);
			dep.SetAttribute ("version", version);
			deps.AppendChild (dep);
		}
		
		XmlElement GetDependenciesElement ()
		{
			XmlElement elem = GetAddinElement ();
			XmlElement de = elem ["Dependencies"];
			if (de != null)
				return de;

			de = root.OwnerDocument.CreateElement ("Dependencies");
			elem.AppendChild (de);
			return de;
		}
		
		XmlElement GetRuntimeElement ()
		{
			XmlElement elem = GetAddinElement ();
			XmlElement de = elem ["Runtime"];
			if (de != null)
				return de;

			de = root.OwnerDocument.CreateElement ("Runtime");
			elem.AppendChild (de);
			return de;
		}
		
		XmlElement GetAddinElement ()
		{
			return root;
		}
		
		void InitCollections ()
		{
			dataFiles = new List<string> ();
			assemblies = new List<string> ();
			
			XmlNodeList elems = root.SelectNodes ("Runtime/Import");
			foreach (XmlElement elem in elems) {
				string asm = elem.GetAttribute ("assembly");
				if (asm != "") {
					assemblies.Add (asm);
				} else {
					string file = elem.GetAttribute ("file");
					if (file != "") {
						dataFiles.Add (file);
					}
				}
			}
		}
	}
}
