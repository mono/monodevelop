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
using MonoDevelop.GtkCore.WidgetLibrary;

namespace MonoDevelop.GtkCore
{
	public class GtkDesignInfo: IDisposable
	{
		ArrayList exportedWidgets = new ArrayList ();
		DotNetProject project;
		ProjectWidgetLibrary widgetLibrary;
		GuiBuilderProject builderProject;
		IDotNetLanguageBinding binding;
		
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
			this.project = project;
			binding = Services.Languages.GetBindingPerLanguageName (project.LanguageName) as IDotNetLanguageBinding;
		}
		
		public void Dispose ()
		{
			if (widgetLibrary != null)
				widgetLibrary.Dispose ();
			if (builderProject != null)
				builderProject.Dispose ();
		}
		
		public GuiBuilderProject GuiBuilderProject {
			get {
				if (builderProject == null) {
					UpdateGtkFolder ();
					builderProject = new GuiBuilderProject (project, SteticFile);
				}
				return builderProject;
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
			get { return exportedWidgets.Count > 0; }
		}
		
		public ProjectWidgetLibrary ProjectWidgetLibrary {
			get {
				if (!IsWidgetLibrary)
					return null;
				if (widgetLibrary == null)
					widgetLibrary = new ProjectWidgetLibrary (project);
				return widgetLibrary;
			}
		}
		
		public Stetic.WidgetLibrary[] GetReferencedWidgetLibraries ()
		{
			ArrayList list = new ArrayList ();

			foreach (ProjectReference pref in project.ProjectReferences) {
				if (pref.ReferenceType == ReferenceType.Project) {
					Project p = project.RootCombine.FindProject (pref.Reference);
					if (p != null) {
						GtkDesignInfo info = GtkCoreService.GetGtkInfo (p);
						if (info != null && info.IsWidgetLibrary)
							list.Add (info.ProjectWidgetLibrary);
					}
				} else if (pref.ReferenceType == ReferenceType.Gac || 
						pref.ReferenceType == ReferenceType.Assembly) {
					
					Stetic.WidgetLibrary lib = GuiBuilderService.GetAssemblyLibrary (pref.Reference);
					if (lib != null)
						list.Add (lib);
				}
			}
			if (IsWidgetLibrary)
				list.Add (ProjectWidgetLibrary);

			return (Stetic.WidgetLibrary[]) list.ToArray (typeof(Stetic.WidgetLibrary));
		}
		
		public bool IsExported (IClass cls)
		{
			return exportedWidgets.Contains (cls.FullyQualifiedName);
		}
		
		public void AddExportedWidget (string name)
		{
			if (!exportedWidgets.Contains (name))
				exportedWidgets.Add (name);
		}

		public IClass[] GetExportedClasses ()
		{
			IParserContext pctx = IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (project);
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
		
		public void UpdateGtkFolder ()
		{
			if (project == null)	// This happens when deserializing
				return;

			// This method synchronizes the current gtk project configuration info
			// with the needed support files in the gtk-gui folder.

			Directory.CreateDirectory (GtkGuiFolder);
				
			// Create the stetic file if not found
			if (!File.Exists (SteticFile)) {
				StreamWriter sw = new StreamWriter (SteticFile);
				sw.WriteLine ("<stetic-interface>");
				sw.WriteLine ("</stetic-interface>");
				sw.Close ();
			}
			
			// The the stetic file to the project as a resource
			if (!project.IsFileInProject (SteticFile))
				project.AddFile (SteticFile, BuildAction.EmbedAsResource);
			
			if (!File.Exists (SteticGeneratedFile)) {
				// Generate an empty build class
				CodeDomProvider provider = GetCodeDomProvider ();
				Stetic.CodeGenerator.GenerateProjectCode (SteticGeneratedFile, "Stetic", provider);
			}

			// Add the generated file to the project, if not already there
			if (!project.IsFileInProject (SteticGeneratedFile))
				project.AddFile (SteticGeneratedFile, BuildAction.Compile);

			// If the project is exporting widgets, make sure the objects.xml file exists
			if (exportedWidgets.Count > 0) {
				if (!File.Exists (ObjectsFile)) {
					XmlDocument doc = new XmlDocument ();
					doc.AppendChild (doc.CreateElement ("objects"));
					doc.Save (ObjectsFile);
				}
				
				ProjectFile file = project.GetProjectFile (ObjectsFile);
				if (file == null)
					project.AddFile (ObjectsFile, BuildAction.EmbedAsResource);
				else if (file.BuildAction != BuildAction.EmbedAsResource)
					file.BuildAction = BuildAction.EmbedAsResource;
			}
			
			// Add gtk-sharp and gdk-sharp references, if not already added.
			
			bool gtk=false, gdk=false;
			foreach (ProjectReference r in project.ProjectReferences) {
				if (r.Reference.StartsWith ("gtk-sharp") && r.ReferenceType == ReferenceType.Gac)
					gtk = true;
				else if (r.Reference.StartsWith ("gdk-sharp") && r.ReferenceType == ReferenceType.Gac)
					gdk = true;
			}
			if (!gtk)
				project.ProjectReferences.Add (new ProjectReference (ReferenceType.Gac, typeof(Gtk.Widget).Assembly.FullName));
			if (!gdk)
				project.ProjectReferences.Add (new ProjectReference (ReferenceType.Gac, typeof(Gdk.Window).Assembly.FullName));
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
