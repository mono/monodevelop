//
// GuiBuilderService.cs
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
using System.IO;
using System.Reflection;
using System.Collections;
using System.CodeDom;
using System.CodeDom.Compiler;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects.Text;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using Mono.Cecil;

using MonoDevelop.GtkCore.WidgetLibrary;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	class GuiBuilderService
	{
		static GuiBuilderProjectPad widgetTreePad;
		static string GuiBuilderLayout = "GUI Builder";
		static string defaultLayout;
	
		static Hashtable assemblyLibs = new Hashtable ();
		static Stetic.Application steticApp;
		
		static bool generating;
		static Stetic.CodeGenerationResult generationResult = null;
		static Exception generatedException = null;
		
		static Stetic.IsolationMode IsolationMode = Stetic.IsolationMode.None;
//		static Stetic.IsolationMode IsolationMode = Stetic.IsolationMode.ProcessUnix;
		
		static GuiBuilderService ()
		{
			IdeApp.ProjectOperations.ReferenceAddedToProject += OnReferencesChanged;
			IdeApp.ProjectOperations.ReferenceRemovedFromProject += OnReferencesChanged;
			
			IdeApp.Workbench.ActiveDocumentChanged += new EventHandler (OnActiveDocumentChanged);
			IdeApp.ProjectOperations.StartBuild += OnBeforeCompile;
			IdeApp.ProjectOperations.EndBuild += OnProjectCompiled;
			IdeApp.ProjectOperations.ParserDatabase.AssemblyInformationChanged += (AssemblyInformationEventHandler) MonoDevelop.Core.Gui.Services.DispatchService.GuiDispatch (new AssemblyInformationEventHandler (OnAssemblyInfoChanged));
			
			IdeApp.Exited += delegate {
				if (steticApp != null)
					steticApp.Dispose ();
			};
		}
		
		internal static GuiBuilderProjectPad WidgetTreePad {
			get { return widgetTreePad; }
			set { widgetTreePad = value; }
		}
		
		public static Stetic.Application SteticApp {
			get {
				if (steticApp == null) {
					steticApp = new Stetic.Application (IsolationMode);
					if (IsolationMode == Stetic.IsolationMode.None)
						steticApp.WidgetLibraryResolver = OnResolveWidgetLibrary;
				}
				return steticApp;
			}
		}
		
		public static GuiBuilderProject GetGuiBuilderProject (Project project)
		{
			GtkDesignInfo info = GtkCoreService.GetGtkInfo (project);
			if (info != null)
				return info.GuiBuilderProject;
			else
				return null;
		}
		
		public static ActionGroupView OpenActionGroup (Project project, Stetic.ActionGroupComponent group)
		{
			GuiBuilderProject p = GetGuiBuilderProject (project);
			string file = p != null ? p.GetSourceCodeFile (group) : null;
			if (file == null) {
				file = ActionGroupDisplayBinding.BindToClass (project, group);
			}
			
			Document doc = IdeApp.Workbench.OpenDocument (file, true);
			if (doc != null) {
				ActionGroupView view = doc.Content as ActionGroupView;
				if (view != null) {
					view.ShowDesignerView ();
					return view;
				}
			}
			return null;
		}
		
		static void OnActiveDocumentChanged (object s, EventArgs args)
		{
			if (IdeApp.Workbench.ActiveDocument == null) {
				if (SteticApp.ActiveProject != null) {
					SteticApp.ActiveProject = null;
					RestoreLayout ();
				}
				return;
			}

			GuiBuilderView view = IdeApp.Workbench.ActiveDocument.Content as GuiBuilderView;
			if (view != null) {
				view.SetActive ();
				SetDesignerLayout ();
			}
			else if (IdeApp.Workbench.ActiveDocument.Content is ActionGroupView) {
				if (SteticApp.ActiveProject != null) {
					SteticApp.ActiveProject = null;
					SetDesignerLayout ();
				}
			} else {
				if (SteticApp.ActiveProject != null) {
					SteticApp.ActiveProject = null;
					RestoreLayout ();
				}
			}
		}
		
		static void SetDesignerLayout ()
		{
			if (IdeApp.Workbench.CurrentLayout != GuiBuilderLayout) {
				bool exists = Array.IndexOf (IdeApp.Workbench.Layouts, GuiBuilderLayout) != -1;
				defaultLayout = IdeApp.Workbench.CurrentLayout;
				IdeApp.Workbench.CurrentLayout = GuiBuilderLayout;
				if (!exists) {
					Pad p = IdeApp.Workbench.Pads [typeof(GuiBuilderPalettePad)];
					if (p != null) p.Visible = true;
					p = IdeApp.Workbench.Pads [typeof(GuiBuilderPropertiesPad)];
					if (p != null) p.Visible = true;
				}
			}
		}
		
		static void RestoreLayout ()
		{
			if (defaultLayout != null) {
				IdeApp.Workbench.CurrentLayout = defaultLayout;
				defaultLayout = null;
			}
		}
		
		static void OnReferencesChanged (object sender, ProjectReferenceEventArgs e)
		{
			CleanUnusedAssemblyLibs ();
		}
		
		static void OnBeforeCompile (object s, BuildEventArgs args)
		{
			if (IdeApp.ProjectOperations.CurrentOpenCombine == null)
				return;

			// Generate stetic files for all modified projects
			GtkProjectServiceExtension.GenerateSteticCode = true;
		}

		static void OnProjectCompiled (object s, BuildEventArgs args)
		{
			if (args.Success) {
				// Unload stetic projects which are not currently
				// being used by the IDE. This will avoid unnecessary updates.
				if (IdeApp.ProjectOperations.CurrentOpenCombine != null) {
					foreach (Project prj in IdeApp.ProjectOperations.CurrentOpenCombine.GetAllProjects ()) {
						GtkDesignInfo info = GtkCoreService.GetGtkInfo (prj);
						if (info != null && !HasOpenDesigners (prj)) {
							info.ReloadGuiBuilderProject ();
						}
					}
				}
				
				SteticApp.UpdateWidgetLibraries (false);
			}
		}
		
		static bool HasOpenDesigners (Project project)
		{
			foreach (Document doc in IdeApp.Workbench.Documents) {
				if ((doc.Content is GuiBuilderView || doc.Content is ActionGroupView) && doc.Project == project)
					return true;
			}
			return false;
		}
		
		static Stetic.WidgetLibrary OnResolveWidgetLibrary (string name)
		{
			if (name.StartsWith ("libstetic,"))
				return null;

			return GetAssemblyLibrary (name, true);
		}
		
		public static AssemblyReferenceWidgetLibrary GetAssemblyLibrary (string assemblyReference, bool alwaysRegister)
		{
			object lib = assemblyLibs [assemblyReference];
			if (lib == null) {
				string aname = IdeApp.ProjectOperations.ParserDatabase.LoadAssembly (assemblyReference);
				AssemblyReferenceWidgetLibrary wlib = new AssemblyReferenceWidgetLibrary (assemblyReference, aname);
				if (!wlib.ExportsWidgets && !alwaysRegister)
					lib = new object ();
				else
					lib = wlib;

				assemblyLibs [assemblyReference] = lib;
			}
			
			// We are registering here all assembly references. Not all of them are widget libraries.
			return lib as AssemblyReferenceWidgetLibrary;
		}
		
		public static bool IsWidgetLibrary (string assemblyReference)
		{
			return GetAssemblyLibrary (assemblyReference, false) != null;
		}
		
		static void CleanUnusedAssemblyLibs ()
		{
			ArrayList toDelete = new ArrayList ();
			CombineEntryCollection col = IdeApp.ProjectOperations.CurrentOpenCombine.GetAllProjects ();
			foreach (string assemblyReference in assemblyLibs.Keys) {
				bool found = false;
				foreach (Project p in col) {
					foreach (ProjectReference pref in p.ProjectReferences) {
						if (pref.Reference == assemblyReference) {
							found = true;
							break;
						}
					}
					if (found) break;
				}
				if (!found)
					toDelete.Add (assemblyReference);
			}
			
			foreach (string name in toDelete) {
				IdeApp.ProjectOperations.ParserDatabase.UnloadAssembly (name);
				assemblyLibs.Remove (name);
			}
		}
		
		static void OnAssemblyInfoChanged (object s, AssemblyInformationEventArgs args)
		{
			//SteticApp.UpdateWidgetLibraries (false);
		}

		internal static void AddCurrentWidgetToClass ()
		{
			if (IdeApp.Workbench.ActiveDocument != null) {
				GuiBuilderView view = IdeApp.Workbench.ActiveDocument.Content as GuiBuilderView;
				if (view != null)
					view.AddCurrentWidgetToClass ();
			}
		}
		
		internal static void JumpToSignalHandler (Stetic.Signal signal)
		{
			if (IdeApp.Workbench.ActiveDocument != null) {
				CombinedDesignView view = IdeApp.Workbench.ActiveDocument.Content as CombinedDesignView;
				if (view != null)
					view.JumpToSignalHandler (signal);
			}
		}
		
		public static void ImportGladeFile (Project project)
		{
			GtkDesignInfo info = GtkCoreService.GetGtkInfo (project);
			if (info == null) info = GtkCoreService.EnableGtkSupport (project);
			info.GuiBuilderProject.ImportGladeFile ();
		}
		
		public static string GetBuildCodeFileName (Project project, Stetic.Component component)
		{
			GtkDesignInfo info = GtkCoreService.GetGtkInfo (project);
			return Path.Combine (info.GtkGuiFolder, component.Name + Path.GetExtension (info.SteticGeneratedFile));
		}
		
		public static string GenerateSteticCodeStructure (DotNetProject project, Stetic.Component component, bool saveToFile, bool overwrite)
		{
			// Generate a class which contains fields for all bound widgets of the component
			
			string name = component.Name;
			string ns = "";
			int i = name.LastIndexOf ('.');
			if (i != -1) {
				ns = name.Substring (0, i);
				name = name.Substring (i+1);
			}
			
			GtkDesignInfo info = GtkCoreService.GetGtkInfo (project);
			string fileName = GetBuildCodeFileName (project, component);
			
			if (saveToFile && !overwrite && File.Exists (fileName))
				return fileName;
			
			CodeCompileUnit cu = new CodeCompileUnit ();
			
			if (info.GeneratePartialClasses) {
				CodeNamespace cns = new CodeNamespace (ns);
				cu.Namespaces.Add (cns);
				
				CodeTypeDeclaration type = new CodeTypeDeclaration (name);
				type.IsPartial = true;
				type.Attributes = MemberAttributes.Public;
				type.TypeAttributes = System.Reflection.TypeAttributes.Public;
				cns.Types.Add (type);
				
				foreach (Stetic.ObjectBindInfo binfo in component.GetObjectBindInfo ()) {
					type.Members.Add (
						new CodeMemberField (
							binfo.TypeName,
							binfo.Name
						)
					);
				}
			}
			else {
				if (!saveToFile)
					return fileName;
				CodeNamespace cns = new CodeNamespace ();
				cns.Comments.Add (new CodeCommentStatement ("Generated code for component " + component.Name));
				cu.Namespaces.Add (cns);
			}
			
			CodeDomProvider provider = project.LanguageBinding.GetCodeDomProvider ();
			if (provider == null)
				throw new UserException ("Code generation not supported for language: " + project.LanguageName);
			
			ICodeGenerator gen = provider.CreateGenerator ();
			TextWriter fileStream;
			if (saveToFile)
				fileStream = new StreamWriter (fileName);
			else
				fileStream = new StringWriter ();
			
			try {
				gen.GenerateCodeFromCompileUnit (cu, fileStream, new CodeGeneratorOptions ());
			} finally {
				fileStream.Close ();
			}

			if (IdeApp.ProjectOperations.ParserDatabase.IsLoaded (project)) {
				// Only update the parser database if the project is actually loaded in the IDE.
				if (saveToFile)
					IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (project).UpdateDatabase ();
				else
					IdeApp.ProjectOperations.ParserDatabase.UpdateFile (project, fileName, ((StringWriter)fileStream).ToString ());
			}

			return fileName;
		}
		
		
		public static Stetic.CodeGenerationResult GenerateSteticCode (IProgressMonitor monitor, Project prj)
		{
			if (generating)
				return null;

			DotNetProject project = prj as DotNetProject;
			if (project == null)
				return null;
				
			GtkDesignInfo info = GtkCoreService.GetGtkInfo (project);
			if (info == null)
				return null;
			
			// Check if the stetic file has been modified since last generation
			if (File.Exists (info.SteticGeneratedFile) && File.Exists (info.SteticFile)) {
				if (File.GetLastWriteTime (info.SteticGeneratedFile) > File.GetLastWriteTime (info.SteticFile))
					return null;
			}
			
			if (info.GuiBuilderProject.IsEmpty) 
				return null;

			monitor.Log.WriteLine (GettextCatalog.GetString ("Generating GUI code for project '{0}'...", project.Name));
			
			// Make sure the referenced assemblies are up to date. It is necessary to do
			// it now since they may contain widget libraries.
			prj.CopyReferencesToOutputPath (false);
			
			ArrayList libs = new ArrayList ();
			
			info.GuiBuilderProject.UpdateLibraries ();
			
			if (info.IsWidgetLibrary) {
				// Make sure the widget export file is up to date.
				GtkCoreService.UpdateObjectsFile (project);
			}

			// Use Gettext for labels if there is a reference to Mono.Posix.
			bool useGettext = false;
			foreach (ProjectReference pref in project.ProjectReferences) {
				if (pref.Reference.StartsWith ("Mono.Posix")) {
					useGettext = true;
					break;
				}
			}
			
			ArrayList projects = new ArrayList ();
			projects.Add (info.GuiBuilderProject.File);
			
			foreach (string lib in info.GuiBuilderProject.SteticProject.GetWidgetLibraries())
				libs.Add (lib);
			
			generating = true;
			generationResult = null;
			generatedException = null;
			
			// Run the generation in another thread to avoid freezing the GUI
			System.Threading.ThreadPool.QueueUserWorkItem ( delegate {
				try {
					if (IsolationMode == Stetic.IsolationMode.None) {
						// Generate the code in another process if stetic is not isolated
						CodeGeneratorProcess cob = (CodeGeneratorProcess) Runtime.ProcessService.CreateExternalProcessObject (typeof (CodeGeneratorProcess), false);
						using (cob) {
							generationResult = cob.GenerateCode (projects, libs, useGettext, info.GeneratePartialClasses);
						}
					} else {
						// No need to create another process, since stetic has its own backend process
						Stetic.GenerationOptions options = new Stetic.GenerationOptions ();
						options.UseGettext = useGettext;
						options.UsePartialClasses = info.GeneratePartialClasses;
						options.GenerateSingleFile = false;
						generationResult = SteticApp.GenerateProjectCode (options, info.GuiBuilderProject.SteticProject);
					}
				} catch (Exception ex) {
					generatedException = ex;
				} finally {
					generating = false;
				}
			});
			
			while (generating) {
				IdeApp.Services.DispatchService.RunPendingEvents ();
				System.Threading.Thread.Sleep (100);
			}
			
			if (generatedException != null)
				throw new UserException ("GUI code generation failed: " + generatedException.Message);
			
			if (generationResult == null)
				return null;
				
			CodeDomProvider provider = project.LanguageBinding.GetCodeDomProvider ();
			if (provider == null)
				throw new UserException ("Code generation not supported in language: " + project.LanguageName);
			
			ICodeGenerator gen = provider.CreateGenerator ();
			string basePath = Path.GetDirectoryName (info.SteticGeneratedFile);
			string ext = Path.GetExtension (info.SteticGeneratedFile);
			
			foreach (Stetic.SteticCompilationUnit unit in generationResult.Units) {
				string fname;
				if (unit.Name.Length == 0)
					fname = info.SteticGeneratedFile;
				else
					fname = Path.Combine (basePath, unit.Name) + ext;
				StreamWriter fileStream = new StreamWriter (fname);
				try {
					gen.GenerateCodeFromCompileUnit (unit, fileStream, new CodeGeneratorOptions ());
				} finally {
					fileStream.Close ();
				}
			}
			
			// Make sure the generated files are added to the project
			info.UpdateGtkFolder ();
			
			return generationResult;
		}
	}


	public class CodeGeneratorProcess: RemoteProcessObject
	{
		public Stetic.CodeGenerationResult GenerateCode (ArrayList projectFiles, ArrayList libraries, bool useGettext, bool usePartialClasses)
		{
			Gtk.Application.Init ();
			
			Stetic.Application app = new Stetic.Application (Stetic.IsolationMode.None);
			
			foreach (string lib in libraries)
				app.AddWidgetLibrary (lib);

			Stetic.Project[] projects = new Stetic.Project [projectFiles.Count];
			for (int n=0; n < projectFiles.Count; n++) {
				projects [n] = app.CreateProject ();
				projects [n].Load ((string) projectFiles [n]);
			}
			
			Stetic.GenerationOptions options = new Stetic.GenerationOptions ();
			options.UseGettext = useGettext;
			options.UsePartialClasses = usePartialClasses;
			options.GenerateSingleFile = false;
			
			return app.GenerateProjectCode (options, projects);
		}
	}
}
