//
// CodeBehindService.cs: Links codebehind classes to their parent files.
//
// Authors:
//   Michael Hutchinson <mhutchinson@novell.com>
//
// Copyright (C) 2006 Michael Hutchinson
// Copyright (C) 2007 Novell, Inc.
//
//
// This source code is licenced under The MIT License:
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

//#define DEBUGCODEBEHINDGROUPING

using System;
using System.Collections.Generic;

using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.DesignerSupport.CodeBehind;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.DesignerSupport
{
	
	
	public class CodeBehindService : IDisposable
	{
		
		Dictionary<ProjectFile, string> codeBehindBindings = new Dictionary<ProjectFile, string> ();
		
		#region Extension loading
		
		internal void Initialise ()
		{
			IdeApp.ProjectOperations.FileRemovedFromProject += onFileEvent;
			IdeApp.ProjectOperations.CombineClosed += onCombineClosed;
			IdeApp.ProjectOperations.CombineOpened += onCombineOpened;
			
			IdeApp.ProjectOperations.ParserDatabase.ParseInformationChanged += onParseInformationChanged;
			IdeApp.ProjectOperations.ParserDatabase.ClassInformationChanged += onClassInformationChanged;
		}
		
		public void Dispose ()
		{
			//FIXME: clear up all the codeBehindBindings
			if (codeBehindBindings != null) {
				codeBehindBindings = null;
				
				IdeApp.ProjectOperations.FileRemovedFromProject -= onFileEvent;
				IdeApp.ProjectOperations.CombineClosed -= onCombineClosed;
				IdeApp.ProjectOperations.CombineOpened -= onCombineOpened;
				
				IdeApp.ProjectOperations.ParserDatabase.ParseInformationChanged -= onParseInformationChanged;
				IdeApp.ProjectOperations.ParserDatabase.ClassInformationChanged -= onClassInformationChanged;
			}
		}
		
		~CodeBehindService ()
		{
			Dispose ();
		}
		
		#endregion
		
		#region file event handlers
		
		void onFileEvent (object sender, ProjectFileEventArgs e)
		{
			updateCodeBehind (e.ProjectFile);
		}
		
		void onClassInformationChanged (object sender, ClassInformationEventArgs e)
		{
#if DEBUGCODEBEHINDGROUPING
			System.Console.WriteLine("onClassInformationChanged");
			foreach (IClass cls in e.ClassInformation.Added)
				System.Console.WriteLine("Added:{0}", cls.FullyQualifiedName);
			foreach (IClass cls in e.ClassInformation.Modified)
				System.Console.WriteLine("Modified:{0}", cls.FullyQualifiedName);
			foreach (IClass cls in e.ClassInformation.Removed)
				System.Console.WriteLine("Removed:{0}", cls.FullyQualifiedName);
#endif
			if (e.Project == null)
				return;
			
			//have to queue up operations outside the foreaches or the collections get out of synch
			List<ProjectFile> affectedChildren = new List<ProjectFile> ();
			List<ProjectFile> affectedParents = new List<ProjectFile> ();
			
			//find all ProjectFiles affected by the relevant class updates
			foreach (KeyValuePair<ProjectFile, string> kvp in codeBehindBindings) {		
				//codebehind must be in same project as file
				if (e.Project != kvp.Key.Project) continue;
				bool affected = false;
				
				foreach (IClass cls in e.ClassInformation.Removed) {
					if (cls.FullyQualifiedName == kvp.Value) {
						AddAffectedFilesFromClass (e.Project, cls, affectedChildren);
						affected = true;
					}
				}
				
				foreach (IClass cls in e.ClassInformation.Added) {
					if (cls.FullyQualifiedName == kvp.Value) {
						AddAffectedFilesFromClass (e.Project, cls, affectedChildren);
						affected = true;
					}
				}
				
				if (affected) {
#if DEBUGCODEBEHINDGROUPING
					System.Console.WriteLine ("File affected {0}", kvp.Key.FilePath);
#endif
					affectedParents.Add (kvp.Key);
				}
			}
			
			if (CodeBehindClassUpdated != null && affectedParents.Count > 0)
				CodeBehindClassUpdated (this, new CodeBehindClassEventArgs (e.Project, affectedParents, affectedChildren));
			
		}
		
		void onParseInformationChanged (object sender, ParseInformationEventArgs args)
		{
			ICodeBehindParent parent = args.ParseInformation.BestCompilationUnit as ICodeBehindParent;
			if (parent == null)
				return;
			
			Project prj = IdeApp.ProjectOperations.GetProjectContaining (args.FileName);
			if (prj != null) {
				ProjectFile file = prj.ProjectFiles.GetFile (args.FileName);
				IParserContext ctx = IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (file.Project);
				updateCodeBehind (file, parent.CodeBehindClassName, ctx);
			}
		}
		
		void AddAffectedFilesFromClass (Project project, IClass cls, List<ProjectFile> list)
		{
			foreach (IClass part in cls.Parts) {
				ProjectFile pf = project.ProjectFiles.GetFile (part.Region.FileName);
				if (pf != null && !list.Contains (pf)) {
					list.Add (pf);
#if DEBUGCODEBEHINDGROUPING
					System.Console.WriteLine ("Added affected file {0}", pf.FilePath);
#endif					
				}
			}
		}
		
		void onCombineOpened (object sender, CombineEventArgs e)
		{
			//loop through all project files in all combines and check for CodeBehind
			foreach (CombineEntry entry in e.Combine.Entries) {
				Project proj = entry as Project;
				if (proj != null)
					foreach (ProjectFile pf in proj.ProjectFiles)
						updateCodeBehind (pf);
			}
		}
		
		void onCombineClosed (object sender, CombineEventArgs e)
		{
			//loop through all project files in all combines and remove their Projectfiles from our list
			foreach (CombineEntry entry in e.Combine.Entries) {
				Project proj = entry as Project;
				if (proj != null)
					foreach (ProjectFile pf in proj.ProjectFiles)
						if (codeBehindBindings.ContainsKey (pf))
							codeBehindBindings.Remove (pf);
			}
		}
		
		#endregion
		
		void updateCodeBehind (ProjectFile file)
		{
			if (file.Project == null)
				return;
			
			string newCodeBehind = null;
			IParserContext ctx = IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (file.Project);
			IParseInformation pi = ctx.GetParseInformation (file.FilePath);
			if (pi != null) {
				ICodeBehindParent parent = pi.BestCompilationUnit as ICodeBehindParent;
				if (parent != null)
					newCodeBehind = parent.CodeBehindClassName;
			}
			
			updateCodeBehind (file, newCodeBehind, ctx);
		}
		
		void updateCodeBehind (ProjectFile file, string newCodeBehind, IParserContext ctx)
		{
			string oldCodeBehind = null;
			codeBehindBindings.TryGetValue (file, out oldCodeBehind);
			
			//if no changes have happened, bail early
			if (newCodeBehind == oldCodeBehind)
				return;
			
			//update the bindings list
			if (newCodeBehind == null)
				codeBehindBindings.Remove (file);
			else
				codeBehindBindings[file] = newCodeBehind;
			
			//build a list of affected "child" files
			List<ProjectFile> affectedChildren = new List<ProjectFile> ();
			if (newCodeBehind != null) {
				IClass cls = ctx.GetClass (newCodeBehind);
				if (cls != null)
					AddAffectedFilesFromClass (file.Project, cls, affectedChildren);
			}
			if (oldCodeBehind != null) {
				IClass cls = ctx.GetClass (oldCodeBehind);
				if (cls != null)
					AddAffectedFilesFromClass (file.Project, cls, affectedChildren);
			}
			
			if (CodeBehindClassUpdated != null)
				CodeBehindClassUpdated (this, new CodeBehindClassEventArgs (file.Project, new ProjectFile[] { file } , affectedChildren));
		}
		
		#region public API for finding CodeBehind files
		
		public bool HasChildren (ProjectFile file)
		{
#if DEBUGCODEBEHINDGROUPING
			System.Console.WriteLine("Checking whether {0} has children",file.FilePath);
#endif
			CodeBehindClass cls = GetChildClass (file);
			if (cls == null) return false;
			if (cls.IClass == null) return true;
			IList<ProjectFile> children = GetProjectFileChildren (file, cls.IClass);
			return children != null && children.Count > 0;
		}
		
		public CodeBehindClass GetChildClass (ProjectFile file)
		{
#if DEBUGCODEBEHINDGROUPING
			System.Console.WriteLine("Getting child class for {0}", file.FilePath);
#endif
			IClass cls = null;
			if (file != null && file.Project != null) {
				string clsName = null;
				codeBehindBindings.TryGetValue (file, out clsName);
				if (clsName != null) {
					IParserContext ctx = IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (file.Project);
					cls = ctx.GetClass (clsName);
					if (cls != null) {
						return new CodeBehindClass (cls);
					} else {
						return new CodeBehindClass (clsName);
					}
				}
			}
			return null;
		}
		
		internal IList<ProjectFile> GetProjectFileChildren (ProjectFile parent, IClass child)
		{
			List<ProjectFile> files = new List<ProjectFile> ();
			
			//IClass.SourceProject is sometimes null (not sure why), so special-case it
			Project proj = child.SourceProject as Project;
			if (proj == null)
				proj = parent.Project;
			if (proj == null) {
				LoggingService.LogWarning ("CodeBehind grouping: Could not find project for class {0}", child.FullyQualifiedName);
				return files;
			}
			
			foreach (IClass part in child.Parts) {
				if (part.Region == null || string.IsNullOrEmpty (part.Region.FileName)) {
					LoggingService.LogError ("CodeBehind grouping: IClass {0} has not defined a valid region.",
					                           part.FullyQualifiedName);
					continue;
				}
				ProjectFile partFile = proj.ProjectFiles.GetFile (part.Region.FileName);
				if (partFile == parent)
					continue;
				if (partFile != null)
					files.Add (partFile);
				else
					LoggingService.LogError ("CodeBehind grouping: The file {0} for IClass {1} was not found in the project.",
					                           part.Region.FileName, part.FullyQualifiedName);
			}
			return files;
		}
		
		//determines whether a file contains only codebehind classes
		public bool ContainsCodeBehind (ProjectFile file)
		{
			IParserContext ctx = IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (file.Project);
			if (ctx == null)
				return false;
			
			IClass[] classes = ctx.GetFileContents (file.FilePath);
			if ((classes == null) || (classes.Length == 0))
				return false;
			
			foreach (IClass cls in classes)
				foreach (KeyValuePair<ProjectFile, string> kvp in codeBehindBindings)
					if (kvp.Key.Project == file.Project && kvp.Value == cls.FullyQualifiedName)
						return true;
			
			return false;
		}
		
		//fired when a CodeBehind association is updated 
		public event CodeBehindClassEventHandler CodeBehindClassUpdated;
		public delegate void CodeBehindClassEventHandler (object sender, CodeBehindClassEventArgs e);
		
		#endregion
	}
}
