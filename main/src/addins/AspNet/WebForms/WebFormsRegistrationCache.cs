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
using System.Xml;
using System.Xml.Linq;
using System.Linq;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.AspNet.Projects;

namespace MonoDevelop.AspNet.WebForms
{	
	class WebFormsRegistrationCache : ProjectFileCache<AspNetAppProject,RegistrationInfo>
	{
		public WebFormsRegistrationCache (AspNetAppProject project) : base (project)
		{
		}
		
		public IList<RegistrationInfo> GetInfosForPath (FilePath dir)
		{
			var infos = new List<RegistrationInfo> ();
			var projectRootParent = Project.BaseDirectory.ParentDirectory;
			while (dir != null && dir.IsChildPathOf (projectRootParent)) {
				var conf = dir.Combine ("web.config");
				var reg = Get (conf);
				if (reg == null) {
					conf = dir.Combine ("Web.config");
					reg = Get (conf);
				}
				if (reg != null)
					infos.Add (reg);
				dir = dir.ParentDirectory;
			}
			infos.Add (MachineRegistrationInfo);
			return infos;
		}
		
		protected override RegistrationInfo GenerateInfo (string filename)
		{
			try {
				using (var reader = new XmlTextReader (filename) { WhitespaceHandling = WhitespaceHandling.None }) {
					var doc = XDocument.Load (reader);
					return ReadFrom (filename, doc);
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Error reading registration info from file '" + filename + "'", ex);
			}
			return null;
		}
		
		static RegistrationInfo ReadFrom (string filename, XDocument doc)
		{
			var info = new RegistrationInfo ();
			
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
			return info;
		}

		
		//FIXME: add more default values, controls, etc. make version dependent.
		//FIXME: use actual machine web.config?
		static RegistrationInfo GetMachineInfo ()
		{
			var info = new RegistrationInfo ();

 			info.Namespaces.AddRange (defaultNamespaces.Select (ns => new NamespaceRegistration (true, ns)));
			info.Assemblies.AddRange (defaultAssemblies.Select (asm => new AssemblyRegistration (true, asm)));

			// from Mono's 4.5 machine web.config
			info.Controls.AddRange (new [] {
				MachineControlReg ("asp", "System.Web.UI.WebControls.WebParts", "System.Web"),
				MachineControlReg ("asp", "System.Web.UI", "System.Web.Extensions"),
				MachineControlReg ("asp", "System.Web.UI.WebControls", "System.Web.Extensions"),
				MachineControlReg ("asp", "System.Web.UI.WebControls.Expressions", "System.Web.Extensions"),
				MachineControlReg ("asp", "System.Web.DynamicData", "System.Web.DynamicData"),
				MachineControlReg ("asp", "System.Web.UI.WebControls", "System.Web.Entity"),
			});
 			
 			return info;
 		}

		static readonly string[] defaultAssemblies = {
			//from Mono's 4.5 web.config
			"mscorlib",
			"Microsoft.CSharp",
			"System",
			"System.Configuration",
			"System.Web",
			"System.Data",
			"System.Web.Services",
			"System.Xml",
			"System.Drawing",
			"System.EnterpriseServices",
			"System.Web.Mobile",
			"System.IdentityModel",
			"System.Runtime.Serialization",
			"System.Xaml",
			"System.ServiceModel",
			"System.ServiceModel.Activation",
			"System.ServiceModel.Channels",
			"System.ServiceModel.Web",
			"System.Activities",
			"System.ServiceModel.Activities",
			"System.WorkflowServices",
			"System.Xaml.Hosting",
			"System.Core",
			"System.Web.Extensions",
			"System.Data.DataSetExtensions",
			"System.Xml.Linq",
			"System.ComponentModel.DataAnnotations",
			"System.Web.DynamicData",
			"System.Data.Entity",
			"System.Web.Entity",
			"System.Data.Linq",
			"System.Data.Entity.Design",
			"System.Web.ApplicationServices",
			//these don't seem to be in Mono as of 3.12
			"System.Web.Abstractions",
			"System.Web.Routing",
			"System.Net.Http",
		};

		//see http://msdn.microsoft.com/en-us/library/eb44kack.aspx
		static readonly string[] defaultNamespaces = {
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

		static ControlRegistration MachineControlReg(string prefix, string ns, string asm)
		{
			return new ControlRegistration (null, true, prefix, ns, asm, null, null);
		}

		public static bool IsDefaultReference (string reference)
		{
			return defaultAssemblies.Any (r =>
				reference.StartsWith (r, StringComparison.Ordinal) &&
				(reference.Length == r.Length || reference [r.Length] == ',')
			);
		}

		public static readonly RegistrationInfo MachineRegistrationInfo = GetMachineInfo ();
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
	
	/// <summary>
	/// Caches items for filename keys. Files may not exist, which doesn't matter.
	/// When a project file with that name is cached in any way, the cache item will be flushed.
	/// </summary>
	/// <remarks>Not safe for multithreaded access.</remarks>
	abstract class ProjectFileCache<T,U> : IDisposable where T : Project
	{
		protected T Project { get; private set; }
		
		Dictionary<string, U> cache;
		
		/// <summary>Creates a ProjectFileCache</summary>
		/// <param name="project">The project the cache is bound to</param>
		protected ProjectFileCache (T project)
		{
			this.Project = project;
			cache =  new Dictionary<string, U> ();
			Project.FileChangedInProject += FileChangedInProject;
			Project.FileRemovedFromProject += FileChangedInProject;
			Project.FileAddedToProject += FileChangedInProject;
			Project.FileRenamedInProject += FileRenamedInProject;
		}

		void FileRenamedInProject (object sender, ProjectFileRenamedEventArgs args)
		{
			foreach (ProjectFileRenamedEventInfo e in args)
				cache.Remove (e.OldName);
		}

		void FileChangedInProject (object sender, ProjectFileEventArgs args)
		{
			foreach (ProjectFileEventInfo e in args)
				cache.Remove (e.ProjectFile.Name);
		}
		
		/// <summary>
		/// Queries the cache for an item. If the file does not exist in the project, returns null.
		/// </summary>
		protected U Get (string filename)
		{
			U value;
			if (cache.TryGetValue (filename, out value))
				return value;
			
			var pf = Project.GetProjectFile (filename);
			if (pf != null)
				value = GenerateInfo (filename);
			
			return cache[filename] = value;
		}
		
		/// <summary>
		/// Detaches from the project's events.
		/// </summary>
		public void Dispose ()
		{
			Project.FileChangedInProject -= FileChangedInProject;
			Project.FileRemovedFromProject -= FileChangedInProject;
			Project.FileAddedToProject -= FileChangedInProject;
			Project.FileRenamedInProject -= FileRenamedInProject;
		}
		
		/// <summary>
		/// Generates info for a given filename.
		/// </summary>
		/// <returns>Null if no info could be generated for the requested filename, e.g. if it did not exist.</returns>
		protected abstract U GenerateInfo (string filename);
	}
}
