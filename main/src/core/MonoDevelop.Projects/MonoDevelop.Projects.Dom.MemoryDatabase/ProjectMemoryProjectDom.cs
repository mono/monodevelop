// 
// ProjectMemoryProjectDom.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Dom.MemoryDatabase
{
	public class ProjectMemoryProjectDom : MemoryProjectDom
	{
		
		public ProjectMemoryProjectDom (Project p)
		{
			this.Project = p;
			
			Project.FileChangedInProject   += OnFileChanged;
			Project.FileAddedToProject     += OnFileAdded;
			Project.FileRemovedFromProject += OnFileRemoved;
			Project.FileRenamedInProject   += OnFileRenamed;
			Project.Modified               += OnProjectModified;

			UpdateFromProject ();
			
		}
		
		public override void Dispose ()
		{
			base.Dispose ();
			Project.FileChangedInProject   -= OnFileChanged;
			Project.FileAddedToProject     -= OnFileAdded;
			Project.FileRemovedFromProject -= OnFileRemoved;
			Project.FileRenamedInProject   -= OnFileRenamed;
			Project.Modified               -= OnProjectModified;
		}
		
		void OnFileChanged (object sender, ProjectFileEventArgs args)
		{
//			FileEntry file = GetFile (args.ProjectFile.Name);
//			if (file != null) {
//				file.ParseErrorRetries = 0;
//				QueueParseJob (file);
//			}
		}
		
		void OnFileAdded (object sender, ProjectFileEventArgs args)
		{
//			if (args.ProjectFile.BuildAction == BuildAction.Compile) {
//				FileEntry file = AddFile (args.ProjectFile.Name);
//				// CheckModifiedFiles won't detect new files, so parsing
//				// must be manyally signaled
//				QueueParseJob (file);
//			}
		}

		void OnFileRemoved (object sender, ProjectFileEventArgs args)
		{
			//RemoveFile (args.ProjectFile.Name);
		}

		void OnFileRenamed (object sender, ProjectFileRenamedEventArgs args)
		{
//			if (args.ProjectFile.BuildAction == BuildAction.Compile) {
//				RemoveFile (args.OldName);
//				FileEntry file = AddFile (args.NewName);
//				// CheckModifiedFiles won't detect new files, so parsing
//				// must be manyally signaled
//				QueueParseJob (file);
//			}
		}
		
		void OnProjectModified (object s, SolutionItemModifiedEventArgs args)
		{
			UpdateCorlibReference ();
			if (UpdateCorlibReference ())
				UpdateReferences ();
		}
		
		List<string> files = new List<string> ();
		void UpdateFromProject ()
		{
			files.Clear ();
			foreach (string fileName in from pf in Project.Files where pf.BuildAction == BuildAction.Compile select pf.Name) {
				if (!files.Contains (fileName)) {
					files.Add (fileName);
					ProjectDomService.QueueParseJob (this, new JobCallback (ParseCallback), fileName);
				}
			}
			
			if (Project is DotNetProject) {
				DotNetProject netProject = (DotNetProject)Project;
				referenceUris.Clear ();
				foreach (ProjectReference pr in netProject.References) {
					foreach (string refId in GetReferenceKeys (pr)) {
						referenceUris.Add (refId);
					}
				}
				UpdateReferences ();
			}
			UpdateCorlibReference ();
		}
		
		void ParseCallback (object ob, IProgressMonitor monitor)
		{
			string fileName = (string) ob;
			
			ProjectDomService.Parse (Project, 
			                         fileName,
			                         null,
			                         delegate () { 
			                            return File.ReadAllText (fileName); 
			                         });

			
			/*
			ParseFile (fileName, monitor);
			lock (rwlock) {
				FileEntry file = GetFile (fileName);
				if (file != null) {
					file.InParseQueue = false;
					FileInfo fi = new FileInfo (fileName);
					file.LastParseTime = fi.LastWriteTime;
				}
			}*/
		}
		
		bool UpdateCorlibReference ()
		{
			// Creates a reference to the correct version of mscorlib, depending
			// on the target runtime version. Returns true if the references
			// have changed.
			DotNetProject prj = Project as DotNetProject;
			if (prj == null) 
				return false;
			
		//	if (prj.TargetFramework.Id == lastVersion)
		//		return false;

			// Look for an existing mscorlib reference
			string currentRefUri = referenceUris.Find (uri => IsCorlibReference (uri));
			
			// Gets the name and version of the mscorlib assembly required by the project
			string requiredRefUri = "Assembly:";
			SystemAssembly asm = Runtime.SystemAssemblyService.CurrentRuntime.GetAssemblyForVersion (typeof(object).Assembly.FullName, null, prj.TargetFramework);
			requiredRefUri += asm.Location;
			
			// Replace the old reference if the target version has changed
			if (currentRefUri != null) {
				if (currentRefUri != requiredRefUri) {
					referenceUris.Remove (currentRefUri);
					referenceUris.Add (requiredRefUri);
					return true;
				}
			} else {
				referenceUris.Add (requiredRefUri);
				return true;
			}
			
			return false;
		}
		
		static bool IsCorlibReference (string uri)
		{
			if (uri.StartsWith ("Assembly:"))
				return Path.GetFileNameWithoutExtension (uri.Substring (9)) == "mscorlib";
			return false;
		}
		
		List<string> referenceUris = new List<string> ();
		internal override IEnumerable<string> OnGetReferences ()
		{
			referenceUris.ForEach (u => Console.WriteLine (u));
			return referenceUris;
		}
		
		IEnumerable<string> GetReferenceKeys (ProjectReference pr)
		{
			if (pr.ReferenceType == ReferenceType.Project) {
				Project referencedProject = Project.ParentSolution.FindProjectByName (pr.Reference);
				yield return "Project:" + (referencedProject != null ? referencedProject.FileName : "null");
			} else {
				foreach (string s in pr.GetReferencedFileNames (ProjectService.DefaultConfiguration))
					yield return "Assembly:" + Path.GetFullPath (s);
			}
		}
		
		
		internal override void OnProjectReferenceAdded (ProjectReference pref)
		{
			foreach (string refId in GetReferenceKeys (pref)) {
				referenceUris.Add (refId);
			}
			this.UpdateReferences ();
		}

		internal override void OnProjectReferenceRemoved (ProjectReference pref)
		{
			foreach (string refId in GetReferenceKeys (pref)) {
				referenceUris.Remove (refId);
			}
			this.UpdateReferences ();
		}

	}
}
