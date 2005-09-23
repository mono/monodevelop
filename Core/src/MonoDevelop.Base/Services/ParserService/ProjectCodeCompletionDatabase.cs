//
// ProjectCodeCompletionDatabase.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.IO;

using MonoDevelop.Internal.Project;
using MonoDevelop.Internal.Parser;
using System.Reflection;

namespace MonoDevelop.Services
{
	internal class ProjectCodeCompletionDatabase: CodeCompletionDatabase
	{
		Project project;
		bool initialFileCheck;
		
		public ProjectCodeCompletionDatabase (Project project, ParserDatabase parserDatabase)
		: base (parserDatabase)
		{
			initialFileCheck = true;
			
			SetLocation (project.BaseDirectory, project.Name);
			
			this.project = project;
			Read ();
			
			UpdateFromProject ();
			
			project.FileChangedInProject += new ProjectFileEventHandler (OnFileChanged);
			project.FileAddedToProject += new ProjectFileEventHandler (OnFileAdded);
			project.FileRemovedFromProject += new ProjectFileEventHandler (OnFileRemoved);
			project.FileRenamedInProject += new ProjectFileRenamedEventHandler (OnFileRenamed);
		}
		
		public Project Project {
			get { return project; }
		}
		
		public override void Dispose ()
		{
			project.FileChangedInProject -= new ProjectFileEventHandler (OnFileChanged);
			project.FileAddedToProject -= new ProjectFileEventHandler (OnFileAdded);
			project.FileRemovedFromProject -= new ProjectFileEventHandler (OnFileRemoved);
			project.FileRenamedInProject -= new ProjectFileRenamedEventHandler (OnFileRenamed);
		}
		
		public override void CheckModifiedFiles ()
		{
			// Once the first modification check is done, change detection
			// is done through project events
			
			if (initialFileCheck) {
				base.CheckModifiedFiles ();
				initialFileCheck = false;
			}
		}
		
		void OnFileChanged (object sender, ProjectFileEventArgs args)
		{
			FileEntry file = GetFile (args.ProjectFile.Name);
			if (file != null) QueueParseJob (file);
		}
		
		void OnFileAdded (object sender, ProjectFileEventArgs args)
		{
			if (args.ProjectFile.BuildAction == BuildAction.Compile)
				AddFile (args.ProjectFile.Name);
		}

		void OnFileRemoved (object sender, ProjectFileEventArgs args)
		{
			RemoveFile (args.ProjectFile.Name);
		}

		void OnFileRenamed (object sender, ProjectFileRenamedEventArgs args)
		{
			if (args.ProjectFile.BuildAction == BuildAction.Compile) {
				RemoveFile (args.OldName);
				AddFile (args.NewName);
			}
		}

		public void UpdateFromProject ()
		{
			Hashtable fs = new Hashtable ();
			foreach (ProjectFile file in project.ProjectFiles)
			{
				if (file.BuildAction != BuildAction.Compile) continue;
				if (GetFile (file.Name) == null) AddFile (file.Name);
				fs [file.Name] = null;
			}
			
			ArrayList keys = new ArrayList ();
			keys.AddRange (files.Keys);
			foreach (string file in keys)
			{
				if (!fs.Contains (file))
					RemoveFile (file);
			}
			
			fs.Clear ();
			foreach (ProjectReference pr in project.ProjectReferences)
			{
				string refId = GetReferenceKey (pr);
				fs[refId] = null;
				if (!HasReference (refId))
					AddReference (refId);
			}
			
			keys.Clear();
			keys.AddRange (references);
			foreach (ReferenceEntry re in keys)
			{
				if (!fs.Contains (re.Uri))
					RemoveReference (re.Uri);
			}
		}
		
		string GetReferenceKey (ProjectReference pr)
		{
			string refId = pr.ReferenceType == ReferenceType.Project ? "Project" : "Assembly";
			refId += ":" + pr.Reference;

			if (pr.ReferenceType == ReferenceType.Gac && refId.ToLower().EndsWith (".dll"))
				refId = refId.Substring (0, refId.Length - 4);
			return refId;
		}
		
		protected override void ParseFile (string fileName, IProgressMonitor monitor)
		{
			if (monitor != null) monitor.BeginTask ("Parsing file: " + Path.GetFileName (fileName), 1);
			
			try {
				IParseInformation parserInfo = parserDatabase.DoParseFile ((string)fileName, null);
				if (parserInfo != null) {
					ClassUpdateInformation res = UpdateFromParseInfo (parserInfo, fileName);
					if (res != null) parserDatabase.NotifyParseInfoChange (fileName, res, project);
				}
			} finally {
				if (monitor != null) monitor.EndTask ();
			}
		}
		
		public ClassUpdateInformation UpdateFromParseInfo (IParseInformation parserInfo, string fileName)
		{
			ICompilationUnit cu = (ICompilationUnit)parserInfo.BestCompilationUnit;

			ClassCollection resolved;
			bool allResolved = parserDatabase.ResolveTypes (project, cu, cu.Classes, out resolved);
			ClassUpdateInformation res = UpdateClassInformation (resolved, fileName);
			
			FileEntry file = files [fileName] as FileEntry;
			if (file == null) return res;
			
			if (!allResolved) {
				if (file.ParseErrorRetries > 0) {
					file.ParseErrorRetries--;
				}
				else
					file.ParseErrorRetries = 3;
			}
			else
				file.ParseErrorRetries = 0;

			return res;
		}
	}
}

