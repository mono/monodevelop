//
// GtkDesignInfo.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
using System.Xml;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Collections;
using System.Collections.Specialized;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.Serialization;

using MonoDevelop.GtkCore.GuiBuilder;

namespace MonoDevelop.GtkCore
{
	public class GtkDesignInfo: IDisposable
	{
		ArrayList exportedWidgets = new ArrayList ();
		DotNetProject project;
		GuiBuilderProject builderProject;
		IDotNetLanguageBinding binding;
		ProjectResourceProvider resourceProvider;
		
		[ItemProperty (DefaultValue=false)]
		bool partialTypes;
		
		[ItemProperty (DefaultValue=true)]
		bool generateGettext = true;
		
		[ItemProperty (DefaultValue=false)]
		bool isWidgetLibrary = false;
		
		[ItemProperty (DefaultValue="Mono.Unix.Catalog")]
		string gettextClass = "Mono.Unix.Catalog";
		
		public GtkDesignInfo ()
		{
		}
		
		public GtkDesignInfo (DotNetProject project)
		{
			partialTypes = GtkCoreService.SupportsPartialTypes (project);
			IExtendedDataItem item = (IExtendedDataItem) project;
			item.ExtendedProperties ["GtkDesignInfo"] = this;
			Bind (project);
		}
		
		public void Bind (DotNetProject project)
		{
			this.project = project;
			binding = Services.Languages.GetBindingPerLanguageName (project.LanguageName) as IDotNetLanguageBinding;
		}
		
		public void Dispose ()
		{
			if (resourceProvider != null)
				System.Runtime.Remoting.RemotingServices.Disconnect (resourceProvider);
			if (builderProject != null)
				builderProject.Dispose ();
		}
		
		public GuiBuilderProject GuiBuilderProject {
			get {
				if (builderProject == null)
					builderProject = new GuiBuilderProject (project, SteticFile);
				return builderProject;
			}
		}
		
		public void ReloadGuiBuilderProject ()
		{
			if (builderProject != null)
				builderProject.Reload ();
		}
		
		public ProjectResourceProvider ResourceProvider {
			get {
				if (resourceProvider == null) {
					resourceProvider = new ProjectResourceProvider (project);
					System.Runtime.Remoting.RemotingServices.Marshal (resourceProvider, null, typeof(Stetic.IResourceProvider));
				}
				return resourceProvider;
			}
		}
		
		public string ObjectsFile {
			get { return Path.Combine (GtkGuiFolder, "objects.xml"); }
		}
		
		public string SteticGeneratedFile {
			get { return Path.Combine (GtkGuiFolder, binding.GetFileName ("generated")); }
		}
		
		public string SteticFile {
			get { return Path.Combine (GtkGuiFolder, "gui.stetic"); }
		}
		
		public string GtkGuiFolder {
			get { return Path.Combine (project.BaseDirectory, "gtk-gui"); }
		}
		
		public bool IsWidgetLibrary {
			get { return isWidgetLibrary || exportedWidgets.Count > 0; }
			set {
				isWidgetLibrary = value;
				if (!isWidgetLibrary)
					exportedWidgets.Clear ();
			}
		}
		
		public bool GeneratePartialClasses {
			get { return partialTypes; }
		}
		
		public bool GenerateGettext {
			get { return generateGettext; }
			set {
				generateGettext = value;
				// Set to default value if gettext is not enabled
				if (!generateGettext) 
					gettextClass = "Mono.Unix.Catalog";
			}
		}
		
		public string GettextClass {
			get { return gettextClass; }
			set { gettextClass = value; }
		}
		
		public bool IsExported (string name)
		{
			return exportedWidgets.Contains (name);
		}
		
		public void AddExportedWidget (string name)
		{
			if (!exportedWidgets.Contains (name))
				exportedWidgets.Add (name);
		}

		public void RemoveExportedWidget (string name)
		{
			exportedWidgets.Remove (name);
		}
		
		public IClass[] GetExportedClasses ()
		{
			IParserContext pctx = GuiBuilderProject.GetParserContext ();
			ArrayList list = new ArrayList ();
			foreach (string cls in exportedWidgets) {
				IClass c = pctx.GetClass (cls);
				if (c != null) list.Add (c);
			}
			return (IClass[]) list.ToArray (typeof(IClass));
		}
		
		[ItemProperty]
		[ItemProperty (Scope=1, Name="Widget")]
		public string[] ExportedWidgets {
			get {
				return (string[]) exportedWidgets.ToArray (typeof(string)); 
			}
			set { 
				exportedWidgets.Clear ();
				exportedWidgets.AddRange (value);
				UpdateGtkFolder ();
			}
		}
		
		public void ForceCodeGenerationOnBuild ()
		{
			try {
				FileInfo fi = new FileInfo (SteticFile);
				fi.LastWriteTime = DateTime.Now;
				fi = new FileInfo (SteticGeneratedFile);
				fi.LastWriteTime = DateTime.Now;
			} catch {
				// Ignore errors here
			}
		}
		
		public bool UpdateGtkFolder ()
		{
			if (project == null)	// This happens when deserializing
				return false;

			// This method synchronizes the current gtk project configuration info
			// with the needed support files in the gtk-gui folder.

			Runtime.FileService.CreateDirectory (GtkGuiFolder);
			bool projectModified = false;
				
			// Create the stetic file if not found
			if (!File.Exists (SteticFile)) {
				StreamWriter sw = new StreamWriter (SteticFile);
				sw.WriteLine ("<stetic-interface>");
				sw.WriteLine ("</stetic-interface>");
				sw.Close ();
			}
			
			// Add the stetic file to the project
			if (!project.IsFileInProject (SteticFile)) {
				project.AddFile (SteticFile, BuildAction.EmbedAsResource);
				projectModified = true;
			}
		
			if (!File.Exists (SteticGeneratedFile)) {
				// Generate an empty build class
				CodeDomProvider provider = GetCodeDomProvider ();
				GuiBuilderService.SteticApp.GenerateProjectCode (SteticGeneratedFile, "Stetic", provider, null);
			}

			// Add the generated file to the project, if not already there
			if (!project.IsFileInProject (SteticGeneratedFile)) {
				project.AddFile (SteticGeneratedFile, BuildAction.Compile);
				projectModified = true;
			}

			// Create files for all widgets
			ArrayList partialFiles = new ArrayList ();
			
			foreach (GuiBuilderWindow win in GuiBuilderProject.Windows) {
				string fn = GuiBuilderService.GenerateSteticCodeStructure (project, win.RootWidget, true, false);
				partialFiles.Add (fn);
				if (!project.IsFileInProject (fn)) {
					project.AddFile (fn, BuildAction.Compile);
					projectModified = true;
				}
			}
			
			foreach (Stetic.ActionGroupComponent ag in GuiBuilderProject.SteticProject.GetActionGroups ()) {
				string fn = GuiBuilderService.GenerateSteticCodeStructure (project, ag, true, false);
				partialFiles.Add (fn);
				if (!project.IsFileInProject (fn)) {
					project.AddFile (fn, BuildAction.Compile);
					projectModified = true;
				}
			}
			
			// Remove all project files which are not in the generated list
			foreach (ProjectFile pf in project.ProjectFiles.GetFilesInPath (GtkGuiFolder)) {
				if (pf.FilePath != SteticGeneratedFile && pf.FilePath != ObjectsFile && pf.FilePath != SteticFile && !partialFiles.Contains (pf.FilePath)) {
					project.ProjectFiles.Remove (pf);
					Runtime.FileService.DeleteFile (pf.FilePath);
					projectModified = true;
				}
			}
			
			// If the project is exporting widgets, make sure the objects.xml file exists
			if (IsWidgetLibrary) {
				if (!File.Exists (ObjectsFile)) {
					XmlDocument doc = new XmlDocument ();
					doc.AppendChild (doc.CreateElement ("objects"));
					doc.Save (ObjectsFile);
				}
				
				ProjectFile file = project.GetProjectFile (ObjectsFile);
				if (file == null) {
					project.AddFile (ObjectsFile, BuildAction.EmbedAsResource);
					projectModified = true;
				}
				else if (file.BuildAction != BuildAction.EmbedAsResource) {
					file.BuildAction = BuildAction.EmbedAsResource;
					projectModified = true;
				}
			}
			
			// Add gtk-sharp and gdk-sharp references, if not already added.
			
			bool gtk=false, gdk=false, posix=false;
			foreach (ProjectReference r in project.ProjectReferences) {
				if (r.Reference.StartsWith ("gtk-sharp") && r.ReferenceType == ReferenceType.Gac)
					gtk = true;
				else if (r.Reference.StartsWith ("gdk-sharp") && r.ReferenceType == ReferenceType.Gac)
					gdk = true;
				else if (r.Reference.StartsWith ("Mono.Posix") && r.ReferenceType == ReferenceType.Gac)
					posix = true;
			}
			if (!gtk)
				project.ProjectReferences.Add (new ProjectReference (ReferenceType.Gac, typeof(Gtk.Widget).Assembly.FullName));
			if (!gdk)
				project.ProjectReferences.Add (new ProjectReference (ReferenceType.Gac, typeof(Gdk.Window).Assembly.FullName));
				
			if (!posix && GenerateGettext && GettextClass == "Mono.Unix.Catalog") {
				// Add a reference to Mono.Posix. Use the version for the selected project's runtime version.
				string aname = Runtime.SystemAssemblyService.FindInstalledAssembly ("Mono.Posix, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756");
				aname = Runtime.SystemAssemblyService.GetAssemblyNameForVersion (aname, project.ClrVersion);
				project.ProjectReferences.Add (new ProjectReference (ReferenceType.Gac, aname));
			}
			
			return projectModified || gtk || gdk || posix;
		}
		
		CodeDomProvider GetCodeDomProvider ()
		{
			IDotNetLanguageBinding binding = Services.Languages.GetBindingPerLanguageName (project.LanguageName) as IDotNetLanguageBinding;
			CodeDomProvider provider = binding.GetCodeDomProvider ();
			if (provider == null)
				throw new UserException ("Code generation not supported in language: " + project.LanguageName);
			return provider;
		}
	}	
}
