//
// CodeBehindService.cs: Links codebehind classes to their parent files.
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
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

using System;
using System.Collections.Generic;

using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.DesignerSupport.CodeBehind;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.DesignerSupport
{
	
	
	public class CodeBehindService
	{
		
		Dictionary<ProjectFile, IClass> codeBehindBindings = new Dictionary<ProjectFile, IClass> ();
		
		#region Extension loading
		
		readonly static string codeBehindProviderPath = "/MonoDevelop/DesignerSupport/CodeBehindProviders";
		List<ICodeBehindProvider> providers = new List<ICodeBehindProvider> ();
		
		internal CodeBehindService ()
		{
			AddinManager.AddExtensionNodeHandler (codeBehindProviderPath, OnProviderExtensionChanged);
		}
		
		internal void Initialise ()
		{
// TODO: Project Conversion
//			IdeApp.ProjectOperations.FileAddedToProject += onFileEvent;
//			IdeApp.ProjectOperations.FileChangedInProject += onFileEvent;
//			IdeApp.ProjectOperations.FileRemovedFromProject += onFileEvent;
			
			ProjectService.SolutionClosed += onCombineClosed;
			ProjectService.SolutionOpened += onCombineOpened;
			
// TODO: Project Conversion
//			IdeApp.ProjectOperations.ParserDatabase.ClassInformationChanged += onClassInformationChanged;
		}
		
		void OnProviderExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			if (args.ExtensionObject == null)
				throw new Exception ("One of the CodeBehindProvider extension classes is missing");
			
			if (args.Change == ExtensionChange.Add)
				providers.Add ((ICodeBehindProvider) args.ExtensionObject);
			
			if (args.Change == ExtensionChange.Remove)
				providers.Remove ((ICodeBehindProvider) args.ExtensionObject);
			
			if (ProjectService.Solution != null) {
				SolutionEventArgs rootCombineArgs = new SolutionEventArgs (ProjectService.Solution);
				if (codeBehindBindings.Count > 0) {
					onCombineClosed (this, rootCombineArgs);
					codeBehindBindings.Clear ();
				}
				onCombineOpened (this, rootCombineArgs);
			}
		}
		
		~CodeBehindService ()
		{
			AddinManager.RemoveExtensionNodeHandler (codeBehindProviderPath, OnProviderExtensionChanged);
// TODO: Project Conversion
//			IdeApp.ProjectOperations.FileAddedToProject -= onFileEvent;
//			IdeApp.ProjectOperations.FileChangedInProject -= onFileEvent;
//			IdeApp.ProjectOperations.FileRemovedFromProject -= onFileEvent;
			
			ProjectService.SolutionClosed -= onCombineClosed;
			ProjectService.SolutionOpened -= onCombineOpened;
			
// TODO: Project Conversion
//			IdeApp.ProjectOperations.ParserDatabase.ClassInformationChanged -= onClassInformationChanged;
		}
		
		#endregion
		
		#region file event handlers
// TODO: Project Conversion		
//		void onFileEvent (object sender, ProjectFileEventArgs e)
//		{
//			updateCodeBehind (e.ProjectFile);
//		}
		
		void onClassInformationChanged (object sender, ClassInformationEventArgs e)
		{
			//have to queue up operations outside the foreaches or the collections get out of synch
			List<KeyValuePair<ProjectFile, IClass>> updates = new List<KeyValuePair<ProjectFile, IClass>> ();
			
			//build a list of all relevant class changes
			foreach (KeyValuePair<ProjectFile, IClass> kvp in codeBehindBindings) {				
				
				foreach (IClass cls in e.ClassInformation.Removed) {
					if (cls.FullyQualifiedName == kvp.Value.FullyQualifiedName) {
						//if class has gone missing, create a dummy one						
						NotFoundClass dummy = new NotFoundClass ();
						dummy.FullyQualifiedName = cls.FullyQualifiedName;
						updates.Add (new KeyValuePair<ProjectFile, IClass> (kvp.Key, dummy));
						break;
					}
				}
				
				foreach (IClass cls in e.ClassInformation.Added) {
					if (cls.FullyQualifiedName == kvp.Value.FullyQualifiedName) {
						updates.Add (new KeyValuePair<ProjectFile, IClass> (kvp.Key, cls));
						break;
					}
				}
				
				foreach (IClass cls in e.ClassInformation.Modified) {
					if (cls.FullyQualifiedName == kvp.Value.FullyQualifiedName) {
						updates.Add (new KeyValuePair<ProjectFile, IClass> (kvp.Key, cls));
						break;
					}
				}
			}
			
			//apply class changes to the codeBehindBindings collection
			foreach (KeyValuePair<ProjectFile, IClass> update in updates) {
				IClass oldCB = codeBehindBindings[update.Key];
				IClass newCB = update.Value;
				
				//skip on if no change in class
				if ( !(oldCB is NotFoundClass ^ newCB is NotFoundClass)) continue;
				
				codeBehindBindings[update.Key] = newCB;
				
				if (CodeBehindClassUpdated != null) {
					if ( !(oldCB is NotFoundClass))
						CodeBehindClassUpdated (oldCB);
					
					if ( !(newCB is NotFoundClass))
						CodeBehindClassUpdated (newCB);
				}
			}
			
		}
		
		void onCombineOpened (object sender, SolutionEventArgs e)
		{
			//loop through all project files in all combines and check for CodeBehind
			foreach (IProject proj in e.Solution.AllProjects) {
				if (proj != null)
					foreach (ProjectItem item in proj.Items) {
						ProjectFile pf = item as ProjectFile;
						if (pf == null)
							continue;
						updateCodeBehind (pf);
					}
			}
		}
		
		void onCombineClosed (object sender, SolutionEventArgs e)
		{
			//loop through all project files in all combines and remove their Projectfiles from our list
			foreach (IProject proj in e.Solution.AllProjects) {
				if (proj != null)
					foreach (ProjectItem item in proj.Items) {
						ProjectFile pf = item as ProjectFile;
						if (pf == null)
							continue;
						if (codeBehindBindings.ContainsKey (pf))
							codeBehindBindings.Remove (pf);
					}
			}
		}
		
		#endregion
		
		void updateCodeBehind (ProjectFile file)
		{
			IClass newCodeBehind = null;
			IClass oldCodeBehind = null;
			
			foreach (ICodeBehindProvider provider in providers) {
				//get the fully-qualified name of the codebehind class if present
				string name = provider.GetCodeBehindClassName (file);
				
				if (name != null) {
// TODO: Project Conversion
//					//look it up in the parser database
//					IParserContext ctx = IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (file.Project);
//					newCodeBehind = ctx.GetClass (name);
//					
					//if class was not found, create a dummy one
					if (newCodeBehind == null) {
						NotFoundClass dummy = new NotFoundClass ();
						dummy.FullyQualifiedName = name;
						newCodeBehind = dummy;
					}
					break;
				}
			}
			
			bool containsKey = this.codeBehindBindings.ContainsKey (file);
			bool nullCB = (newCodeBehind == null);
			
			if (nullCB) {
				if (containsKey) {
					//was codebehind, but no longer
					oldCodeBehind = this.codeBehindBindings[file];
					this.codeBehindBindings.Remove (file);
				} else {
					//not codebehind, no updates
					return;
				}	
			} else {
				if (containsKey) {
					//updating an existing binding
					oldCodeBehind = this.codeBehindBindings[file];
					
					//if no changes have happened, bail early
					if (oldCodeBehind == newCodeBehind) return;
				}
				
				this.codeBehindBindings[file] = newCodeBehind;
				if (CodeBehindClassUpdated != null)
					CodeBehindClassUpdated (newCodeBehind);
			}
			
			if (CodeBehindFileUpdated != null)
				CodeBehindFileUpdated (file);
				
			if (oldCodeBehind != null && CodeBehindClassUpdated != null)
				CodeBehindClassUpdated (oldCodeBehind);
		}
		
		#region public API for finding CodeBehind files
		
		public IClass GetCodeBehind (ProjectFile file)
		{
			if (codeBehindBindings.ContainsKey (file))
				return codeBehindBindings[file];
			return null;
		}
		
		public bool IsCodeBehind (IClass cls)
		{
			return codeBehindBindings.ContainsValue (cls);
		}
		
		//determines whether a file contains only codebehind classes
		public bool ContainsOnlyCodeBehind (ProjectFile file)
		{
// TODO: Project Conversion
			IParserContext ctx = null; //IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (file.Project);
			if (ctx == null)
				return false;
			
			IClass[] classes = ctx.GetFileContents (file.FullPath);
			if ((classes == null) || (classes.Length == 0))
				return false;
			
			bool allClassesAreCodeBehind = true;
			foreach (IClass cls in classes)
				if (codeBehindBindings.ContainsValue (cls) == false) {
					allClassesAreCodeBehind = false;
					break;
				}
			
			return allClassesAreCodeBehind;
		}
		
		public IList<IClass> GetAllCodeBehindClasses (IProject project)
		{
			List<IClass> matches = new List<IClass> ();
			 
			foreach (ProjectItem item in project.Items){
				ProjectFile pf = item as ProjectFile;
				if (pf == null)
					continue;
				IClass match = codeBehindBindings[pf];
				if (match == null)
					matches.Add (match);
			}
			
			return matches;
		}
		
		//fired when a CodeBehind class is updated 
		public event CodeBehindClassEventHandler CodeBehindClassUpdated;
		public delegate void CodeBehindClassEventHandler (IClass cls);
		
		//fired when a codebehind 'host' file is updated
		public event CodeBehindFileEventHandler CodeBehindFileUpdated;
		public delegate void CodeBehindFileEventHandler (ProjectFile file);
		
		#endregion
		
		//used for references to classes not found in the parse database
		private class NotFoundClass : DefaultClass
		{
		}
		
	}
}
