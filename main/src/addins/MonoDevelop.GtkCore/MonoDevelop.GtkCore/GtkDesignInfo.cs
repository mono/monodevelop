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
using System.Collections.Generic;
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
		
		static string[] GtkSharpAssemblies = new string [] { 
			"art-sharp", "atk-sharp", "gconf-sharp", "gdk-sharp", "glade-sharp","glib-sharp","gnome-sharp",
			"gnome-vfs-sharp", "gtk-dotnet", "gtkhtml-sharp", "gtk-sharp", "pango-sharp", "rsvg-sharp", "vte-sharp"
		};
		
		[ItemProperty (DefaultValue=true)]
		bool generateGettext = true;
		
		[ItemProperty (DefaultValue=false)]
		bool isWidgetLibrary = false;
		
		[ItemProperty (DefaultValue="Mono.Unix.Catalog")]
		string gettextClass = "Mono.Unix.Catalog";
		
		[ItemProperty]
		string gtkVersion;
		
		public GtkDesignInfo ()
		{
		}
		
		public GtkDesignInfo (DotNetProject project)
		{
			IExtendedDataItem item = (IExtendedDataItem) project;
			item.ExtendedProperties ["GtkDesignInfo"] = this;
			Bind (project);
		}
		
		public void Bind (DotNetProject project)
		{
			if (this.project == null) {
				this.project = project;
				binding = Services.Languages.GetBindingPerLanguageName (project.LanguageName) as IDotNetLanguageBinding;
				project.NameChanged += OnProjectRenamed;
			}
		}
		
		public void Dispose ()
		{
			if (resourceProvider != null)
				System.Runtime.Remoting.RemotingServices.Disconnect (resourceProvider);
			if (builderProject != null)
				builderProject.Dispose ();
			if (project != null)
				project.NameChanged -= OnProjectRenamed;
		}
		
		public GuiBuilderProject GuiBuilderProject {
			get {
				if (builderProject == null) {
					if (!SupportsDesigner)
						builderProject = new GuiBuilderProject (project, null);
					else
						builderProject = new GuiBuilderProject (project, SteticFile);
				}
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
			get { return GtkCoreService.SupportsPartialTypes (project); }
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
		
		public string TargetGtkVersion {
			get {
				if (gtkVersion != null)
					return gtkVersion;
				else
					return GtkCoreService.DefaultGtkVersion;
			}
			set {
				if (gtkVersion != value) {
					gtkVersion = value;
					if (SupportsDesigner) {
						GuiBuilderProject.SteticProject.TargetGtkVersion = TargetGtkVersion;
						GuiBuilderProject.Save (false);
					}
				}
			}
		}
		
		public bool SupportsDesigner {
			get { return GtkCoreService.SupportsGtkDesigner (project); }
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
			IParserContext pctx;
			if (builderProject != null)
				pctx = builderProject.GetParserContext ();
			else
				pctx = IdeApp.Workspace.ParserDatabase.GetProjectParserContext (project);
			
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
			if (!SupportsDesigner)
				return;
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

			FileService.CreateDirectory (GtkGuiFolder);
			bool projectModified = false;
			
			if (SupportsDesigner) {
				// Create the stetic file if not found
				if (!File.Exists (SteticFile)) {
					StreamWriter sw = new StreamWriter (SteticFile);
					sw.WriteLine ("<stetic-interface>");
					sw.WriteLine ("</stetic-interface>");
					sw.Close ();
					FileService.NotifyFileChanged (SteticFile);
				}
				
				// Add the stetic file to the project
				if (!project.IsFileInProject (SteticFile)) {
					ProjectFile pf = project.AddFile (SteticFile, BuildAction.EmbedAsResource);
					pf.ResourceId = "gui.stetic";
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

				if (!GuiBuilderProject.HasError)
				{
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
					
					foreach (Stetic.ActionGroupInfo ag in GuiBuilderProject.SteticProject.ActionGroups) {
						string fn = GuiBuilderService.GenerateSteticCodeStructure (project, ag, true, false);
						partialFiles.Add (fn);
						if (!project.IsFileInProject (fn)) {
							project.AddFile (fn, BuildAction.Compile);
							projectModified = true;
						}
					}
					
					// Remove all project files which are not in the generated list
					foreach (ProjectFile pf in project.Files.GetFilesInPath (GtkGuiFolder)) {
						if (pf.FilePath != SteticGeneratedFile && pf.FilePath != ObjectsFile && pf.FilePath != SteticFile && !partialFiles.Contains (pf.FilePath)) {
							project.Files.Remove (pf);
							FileService.DeleteFile (pf.FilePath);
							projectModified = true;
						}
					}
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
					file = project.AddFile (ObjectsFile, BuildAction.EmbedAsResource);
					file.ResourceId = "objects.xml";
					projectModified = true;
				}
				else if (file.BuildAction != BuildAction.EmbedAsResource || file.ResourceId != "objects.xml") {
					file.BuildAction = BuildAction.EmbedAsResource;
					file.ResourceId = "objects.xml";
					projectModified = true;
				}
			}
			
			// Add gtk-sharp and gdk-sharp references, if not already added.
			
			string gtkAsmVersion = "";
			
			if (gtkVersion != null) {
				foreach (SystemPackage p in Runtime.SystemAssemblyService.GetPackages ()) {
					if (p.Name == "gtk-sharp-2.0" && p.Version == TargetGtkVersion) {
						string fn = Runtime.SystemAssemblyService.GetAssemblyFullName (p.Assemblies[0]);
						int i = fn.IndexOf (',');
						gtkAsmVersion = fn.Substring (i+1).Trim ();
						break;
					}
				}
			}	
			
			bool gtk=false, gdk=false, posix=false;
			
			if (!SupportsDesigner) // No need to check posix in this case
				posix = true;
			
			foreach (ProjectReference r in new List<ProjectReference> (project.References)) {
				if (r.ReferenceType != ReferenceType.Gac)
					continue;
				int i = r.StoredReference.IndexOf (',');
				if (i == -1)
					continue;

				string aname = r.StoredReference.Substring (0,i).Trim ();
				if (aname == "gtk-sharp")
					gtk = true;
				else if (aname == "gdk-sharp")
					gdk = true;
				else if (aname == "Mono.Posix")
					posix = true;
				
				// Is a gtk-sharp-2.0 assembly?
				if (Array.IndexOf (GtkSharpAssemblies, aname) == -1)
					continue;
				
				// Check and correct the assembly version only if a version is set
				if (!string.IsNullOrEmpty (gtkAsmVersion) && r.StoredReference.Substring (i+1).Trim() != gtkAsmVersion) {
					project.References.Remove (r);
					project.References.Add (new ProjectReference (ReferenceType.Gac, aname + ", " + gtkAsmVersion));
				}
			}
			if (!gtk)
				project.References.Add (new ProjectReference (ReferenceType.Gac, typeof(Gtk.Widget).Assembly.FullName));
			if (!gdk)
				project.References.Add (new ProjectReference (ReferenceType.Gac, typeof(Gdk.Window).Assembly.FullName));
				
			if (!posix && GenerateGettext && GettextClass == "Mono.Unix.Catalog") {
				// Add a reference to Mono.Posix. Use the version for the selected project's runtime version.
				string aname = Runtime.SystemAssemblyService.FindInstalledAssembly ("Mono.Posix, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756");
				aname = Runtime.SystemAssemblyService.GetAssemblyNameForVersion (aname, project.ClrVersion);
				project.References.Add (new ProjectReference (ReferenceType.Gac, aname));
			}
			
			return projectModified || !gtk || !gdk || !posix;
		}
		
		CodeDomProvider GetCodeDomProvider ()
		{
			IDotNetLanguageBinding binding = Services.Languages.GetBindingPerLanguageName (project.LanguageName) as IDotNetLanguageBinding;
			CodeDomProvider provider = binding.GetCodeDomProvider ();
			if (provider == null)
				throw new UserException ("Code generation not supported for language: " + project.LanguageName);
			return provider;
		}
		
		void OnProjectRenamed (object s, SolutionItemRenamedEventArgs a)
		{
			GtkCoreService.UpdateProjectName (project, a.OldName, a.NewName);
		}
	}	
}
