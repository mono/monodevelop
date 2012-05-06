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
using System.Collections;
using System.CodeDom;
using System.CodeDom.Compiler;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Deployment;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.TypeSystem;
using Mono.TextEditor;
using System.Collections.Generic;


namespace MonoDevelop.GtkCore.GuiBuilder
{
	class GuiBuilderService
	{
		static string GuiBuilderLayout = "Visual Design";
		
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
				// Stetic is not thread safe, so all has to be done in the gui thread
				DispatchService.AssertGuiThread ();
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
			return Runtime.SystemAssemblyService.DefaultAssemblyContext.GetAssemblyLocation (assemblyName, null);
		}
		
		static string OnMimeResolve (string url)
		{
			return DesktopService.GetMimeTypeForUri (url);
		}
		
		static void OnShowUrl (string url)
		{
			DesktopService.ShowUrl (url);
		}
		
		internal static void StoreConfiguration ()
		{
			PropertyService.Set ("MonoDevelop.GtkCore.ShowNonContainerWarning", steticApp.ShowNonContainerWarning);
			PropertyService.SaveProperties ();
		}
		
		public static bool AutoSwitchGuiLayout {
			get {
				return PropertyService.Get ("MonoDevelop.GtkCore.AutoSwitchGuiLayout", false);
			}
			set {
				PropertyService.Set ("MonoDevelop.GtkCore.AutoSwitchGuiLayout", value);
			}
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
			if (AutoSwitchGuiLayout && IdeApp.Workbench.CurrentLayout != GuiBuilderLayout) {
				bool exists = IdeApp.Workbench.Layouts.Contains (GuiBuilderLayout);
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
			if (AutoSwitchGuiLayout && defaultLayout != null) {
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
						if (!HasOpenDesigners (prj, false)) {
							GtkDesignInfo info = GtkDesignInfo.FromProject (prj);
							info.ReloadGuiBuilderProject ();
						}
					}
				}
				
				SteticApp.UpdateWidgetLibraries (false);
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
			return GetBuildCodeFileName (project, componentName, string.Empty);
		}
		
		public static string GetBuildCodeFileName (Project project, string componentName, string nameSpace)
		{
			GtkDesignInfo info = GtkDesignInfo.FromProject (project);
			string componentFile = info.GetComponentFile (componentName);
			
			if (componentFile == null) {
				if (nameSpace == "Stetic") {
					return info.GetBuildFileInSteticFolder (componentName);
				} 
//					else
//						throw new UserException ("Cannot find component file for " + componentName);
			} else
				return info.GetBuildFileFromComponent (componentFile);
			
			return null;
		}

		/// <summary>
		/// Generates an in memory stub class used to update the code completion members.
		/// </summary>
		public static void GenerateStubClass (DotNetProject project, Stetic.ProjectItemInfo item)
		{
			GenerateStubClass (project, item, null, null);
		}
		
		/// <summary>
		/// Generates an in memory stub class used to update the code completion members.
		/// </summary>
		public static void GenerateStubClass (DotNetProject project, Stetic.Component component, Stetic.ComponentNameEventArgs args)
		{
			GenerateStubClass (project, null, component, args);
		}
		
		static void GenerateStubClass (DotNetProject project, Stetic.ProjectItemInfo item, Stetic.Component component, Stetic.ComponentNameEventArgs args)
		{
			// Generate a class which contains fields for all bound widgets of the component
			
			string name = item != null ? item.Name : component.Name;
			string fileName = GetBuildCodeFileName (project, name);
			if (fileName == null)
				return;
			
			string ns = "";
			int i = name.LastIndexOf ('.');
			if (i != -1) {
				ns = name.Substring (0, i);
				name = name.Substring (i + 1);
			}
			
			if (item != null)
				component = item.Component;
			
			CodeCompileUnit cu = new CodeCompileUnit ();
			
			CodeNamespace cns = new CodeNamespace (ns);
			cu.Namespaces.Add (cns);
			
			CodeTypeDeclaration type = new CodeTypeDeclaration (name);
			type.IsPartial = true;
			type.Attributes = MemberAttributes.Public;
			type.TypeAttributes = System.Reflection.TypeAttributes.Public;
			cns.Types.Add (type);

			var stubBuildMethod = new CodeMemberMethod ();
			stubBuildMethod.Name = "Build";
			stubBuildMethod.Attributes = MemberAttributes.Family;
			type.Members.Add (stubBuildMethod);

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

			CodeDomProvider provider = project.LanguageBinding.GetCodeDomProvider ();
			if (provider == null)
				throw new UserException ("Code generation not supported for language: " + project.LanguageName);

			var sw = new StringWriter ();
			provider.GenerateCodeFromCompileUnit (cu, sw, new CodeGeneratorOptions ());
			TypeSystemService.ParseFile (
				project,
				fileName,
				DesktopService.GetMimeTypeForUri (fileName),
				sw.ToString ()
			);
		}
		
		public static Stetic.CodeGenerationResult GenerateSteticCode (IProgressMonitor monitor, DotNetProject project, string rootWidget, ConfigurationSelector configuration)
		{
			if (generating || !GtkDesignInfo.HasDesignedObjects (project))
				return null;

			GtkDesignInfo info = GtkDesignInfo.FromProject (project);
			info.CheckGtkFolder ();

			DateTime last_gen_time = File.Exists (info.SteticGeneratedFile) ? File.GetLastWriteTime (info.SteticGeneratedFile) : DateTime.MinValue;
			
			bool ref_changed = false;
			foreach (ProjectReference pref in project.References) {
				if (!pref.IsValid)
					continue;
				foreach (string filename in pref.GetReferencedFileNames (configuration)) {
					if (File.GetLastWriteTime (filename) > last_gen_time) {
						ref_changed = true;
						break;
					}
				}
				if (ref_changed)
					break;
			}

			// Check if generated code is already up to date.
//			if (!ref_changed && last_gen_time >= File.GetLastWriteTime (info.SteticFile))
//				return null;

			if (info.GuiBuilderProject.HasError) {
				monitor.ReportError (
					GettextCatalog.GetString (
					"GUI code generation failed for project '{0}'. The file '{1}' could not be loaded.",
					project.Name,
					info.SteticFile
				),
					null
				);
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
			
			ArrayList projectFolders = new ArrayList ();
			projectFolders.Add (info.SteticFolder.FullPath);
			
			generating = true;
			Stetic.CodeGenerationResult generationResult = null;
			Exception generatedException = null;
			
			bool canGenerateInProcess = IsolationMode != Stetic.IsolationMode.None || info.GuiBuilderProject.SteticProject.CanGenerateCode;
			
			if (!canGenerateInProcess) {
				// Run the generation in another thread to avoid freezing the GUI
				System.Threading.ThreadPool.QueueUserWorkItem (delegate {
					try {
						// Generate the code in another process if stetic is not isolated
						CodeGeneratorProcess cob = (CodeGeneratorProcess)Runtime.ProcessService.CreateExternalProcessObject (typeof(CodeGeneratorProcess), false);
						using (cob) {
							generationResult = cob.GenerateCode (projectFolders, info.GenerateGettext, info.GettextClass, project.UsePartialTypes, info, rootWidget);
						}
					} catch (Exception ex) {
						generatedException = ex;
					} finally {
						generating = false;
					}
				}
				);
			
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
					generationResult = SteticApp.GenerateProjectCode (options, rootWidget, info.GuiBuilderProject.SteticProject);
				} catch (Exception ex) {
					generatedException = ex;
				}
				generating = false;
			}
			
			if (generatedException != null) {
				string msg = string.Empty;
				
				if (generatedException.InnerException != null) {
					msg = string.Format ("{0}\n{1}\nInner Exception {2}\n{3}",
				                        generatedException.Message,
				                        generatedException.StackTrace,
					                    generatedException.InnerException.Message,
					                    generatedException.InnerException.StackTrace);
				} else {
					msg = string.Format ("{0}\n{1}",
				                        generatedException.Message,
				                        generatedException.StackTrace);
				}
				                           
//				LoggingService.LogError ("GUI code generation failed", generatedException);
				LoggingService.LogError ("GUI code generation failed: " + msg);
				throw new UserException ("GUI code generation failed: " + msg);
			}
			
			if (generationResult == null)
				return null;
				
			CodeDomProvider provider = project.LanguageBinding.GetCodeDomProvider ();
			if (provider == null)
				throw new UserException ("Code generation not supported for language: " + project.LanguageName);
			
			foreach (Stetic.SteticCompilationUnit unit in generationResult.Units) {
				string fname;
				if (unit.Name.Length == 0)
					fname = info.SteticGeneratedFile;
				else
					fname = GetBuildCodeFileName (project, 
					                              unit.Name, 
					                              (unit.Namespace != null) ? unit.Namespace.Name : string.Empty);
				
				StringWriter sw = new StringWriter ();
				try {
					foreach (CodeNamespace ns in unit.Namespaces)
						ns.Comments.Add (new CodeCommentStatement ("This file has been generated by the GUI designer. Do not modify."));
					provider.GenerateCodeFromCompileUnit (unit, sw, new CodeGeneratorOptions ());
					string content = sw.ToString ();
					content = FormatGeneratedFile (fname, content, project, provider);
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
				var files = IdeApp.ProjectOperations.AddFilesToProject (prj, new string[] { file }, prj.BaseDirectory);
				if (files.Count == 0 || files[0] == null)
					return null;
				pfile = files [0];
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
		
		static string FormatGeneratedFile (string file, string content, Project project, CodeDomProvider provider)
		{
			content = StripHeaderAndBlankLines (content, provider);

			string mt = DesktopService.GetMimeTypeForUri (file);
			var formatter = MonoDevelop.Ide.CodeFormatting.CodeFormatterService.GetFormatter (mt);
			if (formatter != null)
				content = formatter.FormatText (project.Policies, content);
			
			// The project policies should be taken for generated files (windows git eol problem)
			var pol = project.Policies.Get<TextStylePolicy> (DesktopService.GetMimeTypeForUri (file));
			string eol = pol.GetEolMarker ();
			if (Environment.NewLine != eol)
				content = content.Replace (Environment.NewLine, eol);
			
			return content;
		}
		
		static string StripHeaderAndBlankLines (string text, CodeDomProvider provider)
		{
			var doc = new TextDocument ();
			doc.Text = text;
			int realStartLine = 0;
			for (int i = 0; i < doc.LineCount; i++) {
				var lineSegment = doc.GetLine (i);
				if (lineSegment == null)
					continue;
				string lineText = doc.GetTextAt (lineSegment);
				// Microsoft.NET generates "auto-generated" tags where Mono generates "autogenerated" tags.
				if (lineText.Contains ("</autogenerated>") || lineText.Contains ("</auto-generated>")) {
					realStartLine = i + 2;
					break;
				}
			}
			
			var realLine = doc.GetLine (realStartLine);
			if (realLine == null)
				return text;
			int offset = realLine.Offset;

			// The Mono provider inserts additional blank lines, so strip them out
			// But blank lines might actually be significant in other languages.
			// We reformat the C# generated output to the user's coding style anyway, but the reformatter preserves blank lines
			if (provider.GetType ().FullName == "MonoDevelop.CSharp.CSharpEnhancedCodeProvider") {
				return doc.GetTextAt (offset, doc.TextLength - offset).TrimStart ();
			}

			return doc.GetTextAt (offset, doc.TextLength - offset);
		}

		static void CheckLine (TextDocument doc, DocumentLine line, out bool isBlank, out bool isBracket)
		{	
			isBlank = true;
			isBracket = false;
			if (line == null)
				return;
			
			for (int i = 0; i < line.Length; i++) {
				char c = doc.GetCharAt (line.Offset + i);
				if (c == '{') {
					isBracket = true;
					isBlank = false;
				}
				else if (!Char.IsWhiteSpace (c)) {
					isBlank = false;
					if (isBracket) {
						isBracket = false;
						break;
					}
				}
			}
		}
	}

	 
	public class CodeGeneratorProcess: RemoteProcessObject
	{
		public Stetic.CodeGenerationResult GenerateCode (ArrayList projectFolders, bool useGettext, string gettextClass, bool usePartialClasses, GtkDesignInfo info, string rootWidget)
		{
			Gtk.Application.Init ();
			
			Stetic.Application app = Stetic.ApplicationFactory.CreateApplication (Stetic.IsolationMode.None);
			
			Stetic.Project[] projects = new Stetic.Project [projectFolders.Count];
			for (int n=0; n < projectFolders.Count; n++) {
				projects [n] = app.CreateProject (info);
				projects [n].Load ((string) projectFolders [n]);
			}
			
			Stetic.GenerationOptions options = new Stetic.GenerationOptions ();
			options.UseGettext = useGettext;
			options.GettextClass = gettextClass;
			
			return app.GenerateProjectCode (options, rootWidget, projects);
		}
	}
}
