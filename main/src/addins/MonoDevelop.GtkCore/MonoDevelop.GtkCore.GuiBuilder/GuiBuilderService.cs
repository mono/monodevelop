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
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects.Text;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Deployment;
using Mono.Cecil;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	class GuiBuilderService
	{
		static string GuiBuilderLayout = "GUI Builder";
		
#if DUMMY_STRINGS_FOR_TRANSLATION_DO_NOT_COMPILE
		private void DoNotCompile ()
		{
			//The default GUI Builder layout, translated indirectly because it's used as an ID
			GettextCatalog.GetString ("GUI Builder");
		}
#endif
		
		static string defaultLayout;
		
		static Stetic.Application steticApp;
		
		static bool generating;
		static Stetic.CodeGenerationResult generationResult = null;
		static Exception generatedException = null;
		
		static Stetic.IsolationMode IsolationMode = Stetic.IsolationMode.None;
//		static Stetic.IsolationMode IsolationMode = Stetic.IsolationMode.ProcessUnix;
		
		static GuiBuilderService ()
		{
			if (IdeApp.Workbench == null)
				return;
			IdeApp.Workbench.ActiveDocumentChanged += new EventHandler (OnActiveDocumentChanged);
			IdeApp.ProjectOperations.EndBuild += OnProjectCompiled;
//			IdeApp.Workspace.ParserDatabase.AssemblyInformationChanged += (AssemblyInformationEventHandler) DispatchService.GuiDispatch (new AssemblyInformationEventHandler (OnAssemblyInfoChanged));
			
			IdeApp.Exited += delegate {
				if (steticApp != null) {
					StoreConfiguration ();
					steticApp.Dispose ();
				}
			};
		}
		
		public static Stetic.Application SteticApp {
			get {
				if (steticApp == null) {
					steticApp = Stetic.ApplicationFactory.CreateApplication (Stetic.IsolationMode.None);
					steticApp.AllowInProcLibraries = false;
					steticApp.ShowNonContainerWarning = PropertyService.Get ("MonoDevelop.GtkCore.ShowNonContainerWarning", true);
					steticApp.MimeResolver = OnMimeResolve;
					steticApp.ShowUrl = OnShowUrl;
					steticApp.WidgetLibraryResolver = OnAssemblyResolve;
				}
				return steticApp;
			}
		}
		
		static string OnAssemblyResolve (string assemblyName)
		{
			return Runtime.SystemAssemblyService.GetAssemblyLocation (assemblyName);
		}
		
		static string OnMimeResolve (string url)
		{
			return MonoDevelop.Core.Gui.Services.PlatformService.GetMimeTypeForUri (url);
		}
		
		static void OnShowUrl (string url)
		{
			MonoDevelop.Core.Gui.Services.PlatformService.ShowUrl (url);
		}
		
		internal static void StoreConfiguration ()
		{
			PropertyService.Set ("MonoDevelop.GtkCore.ShowNonContainerWarning", steticApp.ShowNonContainerWarning);
			PropertyService.SaveProperties ();
		}

		
		public static ActionGroupView OpenActionGroup (Project project, Stetic.ActionGroupInfo group)
		{
			GuiBuilderProject p = GtkDesignInfo.FromProject (project).GuiBuilderProject ;
			string file = p != null ? p.GetSourceCodeFile (group) : null;
			if (file == null) {
				file = ActionGroupDisplayBinding.BindToClass (project, group);
			}
			
			Document doc = IdeApp.Workbench.OpenDocument (file, true);
			if (doc != null) {
				ActionGroupView view = doc.GetContent<ActionGroupView> ();
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
				if (SteticApp.ActiveDesigner != null) {
					SteticApp.ActiveDesigner = null;
					RestoreLayout ();
				}
				return;
			}

			CombinedDesignView view = IdeApp.Workbench.ActiveDocument.GetContent<CombinedDesignView> ();
			if (view != null) {
				SteticApp.ActiveDesigner = view.Designer;
				SetDesignerLayout ();
				return;
			}
			else if (SteticApp.ActiveDesigner != null) {
				SteticApp.ActiveDesigner = null;
				RestoreLayout ();
			}
		}
		
		static void SetDesignerLayout ()
		{
			if (IdeApp.Workbench.CurrentLayout != GuiBuilderLayout) {
				bool exists = Array.IndexOf (IdeApp.Workbench.Layouts, GuiBuilderLayout) != -1;
				defaultLayout = IdeApp.Workbench.CurrentLayout;
				IdeApp.Workbench.CurrentLayout = GuiBuilderLayout;
				if (!exists) {
					Pad p = IdeApp.Workbench.GetPad<MonoDevelop.DesignerSupport.ToolboxPad> ();
					if (p != null) p.Visible = true;
					p = IdeApp.Workbench.GetPad<MonoDevelop.DesignerSupport.PropertyPad> ();
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
		
		static void OnProjectCompiled (object s, BuildEventArgs args)
		{
			if (args.Success) {
				// Unload stetic projects which are not currently
				// being used by the IDE. This will avoid unnecessary updates.
				if (IdeApp.Workspace.IsOpen) {
					foreach (Project prj in IdeApp.Workspace.GetAllProjects ()) {
						GtkDesignInfo info = GtkDesignInfo.FromProject (prj);
						if (!HasOpenDesigners (prj, false)) {
							info.ReloadGuiBuilderProject ();
						}
					}
				}
				
				SteticApp.UpdateWidgetLibraries (false);
			}
			else {
				// Some gtk# packages don't include the .pc file unless you install gtk-sharp-devel
				if (Runtime.SystemAssemblyService.GetPackage ("gtk-sharp-2.0") == null) {
					string msg = GettextCatalog.GetString ("ERROR: MonoDevelop could not find the Gtk# 2.0 development package. Compilation of projects depending on Gtk# libraries will fail. You may need to install development packages for gtk-sharp-2.0.");
					args.ProgressMonitor.Log.WriteLine ();
					args.ProgressMonitor.Log.WriteLine (msg);
				}
			}
		}
		
		internal static bool HasOpenDesigners (Project project, bool modifiedOnly)
		{
			foreach (Document doc in IdeApp.Workbench.Documents) {
				if ((doc.GetContent<GuiBuilderView>() != null || doc.GetContent<ActionGroupView>() != null) && doc.Project == project && (!modifiedOnly || doc.IsDirty))
					return true;
			}
			return false;
		}
		
		//static void OnAssemblyInfoChanged (object s, AssemblyInformationEventArgs args)
//		{
			//SteticApp.UpdateWidgetLibraries (false);
//		}

		internal static void AddCurrentWidgetToClass ()
		{
			if (IdeApp.Workbench.ActiveDocument != null) {
				GuiBuilderView view = IdeApp.Workbench.ActiveDocument.GetContent<GuiBuilderView> ();
				if (view != null)
					view.AddCurrentWidgetToClass ();
			}
		}
		
		internal static void JumpToSignalHandler (Stetic.Signal signal)
		{
			if (IdeApp.Workbench.ActiveDocument != null) {
				CombinedDesignView view = IdeApp.Workbench.ActiveDocument.GetContent<CombinedDesignView> ();
				if (view != null)
					view.JumpToSignalHandler (signal);
			}
		}
		
		public static void ImportGladeFile (Project project)
		{
			GtkDesignInfo info = GtkDesignInfo.FromProject (project);
			info.GuiBuilderProject.ImportGladeFile ();
		}
		
		public static string GetBuildCodeFileName (Project project, string componentName)
		{
			GtkDesignInfo info = GtkDesignInfo.FromProject (project);
			return Path.Combine (info.GtkGuiFolder, componentName + Path.GetExtension (info.SteticGeneratedFile));
		}
		
		public static string GenerateSteticCodeStructure (DotNetProject project, Stetic.ProjectItemInfo item, bool saveToFile, bool overwrite)
		{
			return GenerateSteticCodeStructure (project, item, null, null, saveToFile, overwrite);
		}
		
		public static string GenerateSteticCodeStructure (DotNetProject project, Stetic.Component component, Stetic.ComponentNameEventArgs args, bool saveToFile, bool overwrite)
		{
			return GenerateSteticCodeStructure (project, null, component, args, saveToFile, overwrite);
		}
		
		static string GenerateSteticCodeStructure (DotNetProject project, Stetic.ProjectItemInfo item, Stetic.Component component, Stetic.ComponentNameEventArgs args, bool saveToFile, bool overwrite)
		{
			// Generate a class which contains fields for all bound widgets of the component
			
			string name = item != null ? item.Name : component.Name;
			string fileName = GetBuildCodeFileName (project, name);
			
			string ns = "";
			int i = name.LastIndexOf ('.');
			if (i != -1) {
				ns = name.Substring (0, i);
				name = name.Substring (i+1);
			}
			
			if (saveToFile && !overwrite && File.Exists (fileName))
				return fileName;
			
			if (item != null)
				component = item.Component;
			
			CodeCompileUnit cu = new CodeCompileUnit ();
			
			if (project.UsePartialTypes) {
				CodeNamespace cns = new CodeNamespace (ns);
				cu.Namespaces.Add (cns);
				
				CodeTypeDeclaration type = new CodeTypeDeclaration (name);
				type.IsPartial = true;
				type.Attributes = MemberAttributes.Public;
				type.TypeAttributes = System.Reflection.TypeAttributes.Public;
				cns.Types.Add (type);
				
				foreach (Stetic.ObjectBindInfo binfo in component.GetObjectBindInfo ()) {
					// When a component is being renamed, we have to generate the 
					// corresponding field using the old name, since it will be renamed
					// later using refactory
					string nname = args != null && args.NewName == binfo.Name ? args.OldName : binfo.Name;
					type.Members.Add (
						new CodeMemberField (
							binfo.TypeName,
							nname
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
			
			TextWriter fileStream;
			if (saveToFile)
				fileStream = new StreamWriter (fileName);
			else
				fileStream = new StringWriter ();
			
			try {
				provider.GenerateCodeFromCompileUnit (cu, fileStream, new CodeGeneratorOptions ());
			} finally {
				fileStream.Close ();
			}

			if (ProjectDomService.HasDom (project)) {
				// Only update the parser database if the project is actually loaded in the IDE.
				if (saveToFile) {
					ProjectDomService.Parse (project, fileName, "");
					FileService.NotifyFileChanged (fileName);
				}
				else
					ProjectDomService.Parse (project, fileName, "", ((StringWriter)fileStream).ToString ());
			}

			return fileName;
		}
		
		
		public static Stetic.CodeGenerationResult GenerateSteticCode (IProgressMonitor monitor, DotNetProject project, string configuration)
		{
			if (generating || !GtkDesignInfo.HasDesignedObjects (project))
				return null;

			GtkDesignInfo info = GtkDesignInfo.FromProject (project);

			// Check if generated code is already up to date.
			if (File.Exists (info.SteticGeneratedFile) && File.GetLastWriteTime (info.SteticGeneratedFile) > File.GetLastWriteTime (info.SteticFile))
				return null;
			
			if (info.GuiBuilderProject.HasError) {
				monitor.ReportError (GettextCatalog.GetString ("GUI code generation failed for project '{0}'. The file '{1}' could not be loaded.", project.Name, info.SteticFile), null);
				monitor.AsyncOperation.Cancel ();
				return null;
			}
			
			if (info.GuiBuilderProject.IsEmpty) 
				return null;

			monitor.Log.WriteLine (GettextCatalog.GetString ("Generating GUI code for project '{0}'...", project.Name));
			
			// Make sure the referenced assemblies are up to date. It is necessary to do
			// it now since they may contain widget libraries.
			project.CopySupportFiles (monitor, configuration);
			
			info.GuiBuilderProject.UpdateLibraries ();
			
			ArrayList projects = new ArrayList ();
			projects.Add (info.GuiBuilderProject.File);
			
			generating = true;
			generationResult = null;
			generatedException = null;
			
			bool canGenerateInProcess = IsolationMode != Stetic.IsolationMode.None || info.GuiBuilderProject.SteticProject.CanGenerateCode;
			
			if (!canGenerateInProcess) {
				// Run the generation in another thread to avoid freezing the GUI
				System.Threading.ThreadPool.QueueUserWorkItem ( delegate {
					try {
						// Generate the code in another process if stetic is not isolated
						CodeGeneratorProcess cob = (CodeGeneratorProcess) Runtime.ProcessService.CreateExternalProcessObject (typeof (CodeGeneratorProcess), false);
						using (cob) {
							generationResult = cob.GenerateCode (projects, info.GenerateGettext, info.GettextClass, project.UsePartialTypes);
						}
					} catch (Exception ex) {
						generatedException = ex;
					} finally {
						generating = false;
					}
				});
			
				while (generating) {
					DispatchService.RunPendingEvents ();
					System.Threading.Thread.Sleep (100);
				}
			} else {
				// No need to create another process, since stetic has its own backend process
				// or the widget libraries have no custom wrappers
				try {
					Stetic.GenerationOptions options = new Stetic.GenerationOptions ();
					options.UseGettext = info.GenerateGettext;
					options.GettextClass = info.GettextClass;
					options.UsePartialClasses = project.UsePartialTypes;
					options.GenerateSingleFile = false;
					generationResult = SteticApp.GenerateProjectCode (options, info.GuiBuilderProject.SteticProject);
				} catch (Exception ex) {
					generatedException = ex;
				}
				generating = false;
			}
			
			if (generatedException != null) {
				LoggingService.LogError ("GUI code generation failed", generatedException);
				throw new UserException ("GUI code generation failed: " + generatedException.Message);
			}
			
			if (generationResult == null)
				return null;
				
			CodeDomProvider provider = project.LanguageBinding.GetCodeDomProvider ();
			if (provider == null)
				throw new UserException ("Code generation not supported for language: " + project.LanguageName);
			
			string basePath = Path.GetDirectoryName (info.SteticGeneratedFile);
			string ext = Path.GetExtension (info.SteticGeneratedFile);
			
			foreach (Stetic.SteticCompilationUnit unit in generationResult.Units) {
				string fname;
				if (unit.Name.Length == 0)
					fname = info.SteticGeneratedFile;
				else
					fname = Path.Combine (basePath, unit.Name) + ext;
				StringWriter sw = new StringWriter ();
				try {
					provider.GenerateCodeFromCompileUnit (unit, sw, new CodeGeneratorOptions ());
					// Remove the runtime version number from the file. It may generate unnecessary
					// version control changes when upgrading the mono version.
					string content = sw.ToString ();
					content = content.Replace ("Mono Runtime Version: " + Environment.Version, "");
					File.WriteAllText (fname, content);
				} finally {
					FileService.NotifyFileChanged (fname);
				}
			}
			
			// Make sure the generated files are added to the project
			if (info.UpdateGtkFolder ()) {
				Gtk.Application.Invoke (delegate {
					IdeApp.ProjectOperations.Save (project);
				});
			}
			
			return generationResult;
		}
		
		internal static string ImportFile (Project prj, string file)
		{
			ProjectFile pfile = prj.Files.GetFile (file);
			if (pfile == null) {
				string[] files = IdeApp.ProjectOperations.AddFilesToProject (prj, new string[] { file }, prj.BaseDirectory);
				if (files.Length == 0)
					return null;
				if (files [0] == null)
					return null;
				pfile = prj.Files.GetFile (files[0]);
			}
			if (pfile.BuildAction == BuildAction.EmbeddedResource) {
				AlertButton embedButton = new AlertButton (GettextCatalog.GetString ("_Use as Source"));
				if (MessageService.AskQuestion (GettextCatalog.GetString ("You are requesting the file '{0}' to be used as source for an image. However, this file is already added to the project as a resource. Are you sure you want to continue (the file will have to be removed from the resource list)?"), AlertButton.Cancel, embedButton) == embedButton)
					return null;
			}
			pfile.BuildAction = BuildAction.Content;
			DeployProperties props = DeployService.GetDeployProperties (pfile);
			props.UseProjectRelativePath = true;
			return pfile.FilePath;
		}
		
	}


	public class CodeGeneratorProcess: RemoteProcessObject
	{
		public Stetic.CodeGenerationResult GenerateCode (ArrayList projectFiles, bool useGettext, string gettextClass, bool usePartialClasses)
		{
			Gtk.Application.Init ();
			
			Stetic.Application app = Stetic.ApplicationFactory.CreateApplication (Stetic.IsolationMode.None);
			
			Stetic.Project[] projects = new Stetic.Project [projectFiles.Count];
			for (int n=0; n < projectFiles.Count; n++) {
				projects [n] = app.CreateProject ();
				projects [n].Load ((string) projectFiles [n]);
			}
			
			Stetic.GenerationOptions options = new Stetic.GenerationOptions ();
			options.UseGettext = useGettext;
			options.GettextClass = gettextClass;
			options.UsePartialClasses = usePartialClasses;
			options.GenerateSingleFile = false;
			
			return app.GenerateProjectCode (options, projects);
		}
	}
}
