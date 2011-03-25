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
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Core.Assemblies;
using System.Reflection;

namespace MonoDevelop.Projects.Dom.Serialization
{
	internal class ProjectCodeCompletionDatabase : SerializationCodeCompletionDatabase
	{
		Project project;
		bool initialFileCheck;
		int parseCount;
		
		public ProjectCodeCompletionDatabase (Project project, ParserDatabase pdb): base (pdb, true)
		{
			Counters.LiveProjectDatabases++;
			this.project = project;
			SetLocation (project.BaseDirectory, project.Name);
			
			Read ();
			
			UpdateFromProject ();
			
			project.FileChangedInProject   += new ProjectFileEventHandler (OnFileChanged);
			project.FileAddedToProject     += new ProjectFileEventHandler (OnFileAdded);
			project.FileRemovedFromProject += new ProjectFileEventHandler (OnFileRemoved);
			project.FileRenamedInProject   += new ProjectFileRenamedEventHandler (OnFileRenamed);
			project.Modified               += new SolutionItemModifiedEventHandler (OnProjectModified);

			initialFileCheck = true;
		}
		
		public override Project Project {
			get { return project; }
		}
		
		public override void Dispose ()
		{
			Counters.LiveProjectDatabases--;
			base.Dispose ();
			project.FileChangedInProject -= new ProjectFileEventHandler (OnFileChanged);
			project.FileAddedToProject -= new ProjectFileEventHandler (OnFileAdded);
			project.FileRemovedFromProject -= new ProjectFileEventHandler (OnFileRemoved);
			project.FileRenamedInProject -= new ProjectFileRenamedEventHandler (OnFileRenamed);
			project.Modified -= new SolutionItemModifiedEventHandler (OnProjectModified);
		}
		
		public override void CheckModifiedFiles ()
		{
			// Once the first modification check is done, change detection
			// is done through project events
			
			if (initialFileCheck)
				base.CheckModifiedFiles ();
			initialFileCheck = false;
		}
		
		void OnFileChanged (object sender, ProjectFileEventArgs args)
		{
			foreach (ProjectFileEventInfo fargs in args) {
				FileEntry file = GetFile (fargs.ProjectFile.Name);
				if (file != null) {
					file.ParseErrorRetries = 0;
					QueueParseJob (file);
				}
			}
		}
		
		void OnFileAdded (object sender, ProjectFileEventArgs args)
		{
			foreach (ProjectFileEventInfo fargs in args) {
				FileEntry file = AddFile (fargs.ProjectFile.Name);
				// CheckModifiedFiles won't detect new files, so parsing
				// must be manyally signaled
				QueueParseJob (file);
			}
		}

		void OnFileRemoved (object sender, ProjectFileEventArgs args)
		{
			foreach (ProjectFileEventInfo fargs in args)
				RemoveFile (fargs.ProjectFile.Name);
		}

		void OnFileRenamed (object sender, ProjectFileRenamedEventArgs args)
		{
			foreach (ProjectFileRenamedEventInfo fargs in args) {
				RemoveFile (fargs.OldName);
				FileEntry file = AddFile (fargs.NewName);
				// CheckModifiedFiles won't detect new files, so parsing
				// must be manyally signaled
				QueueParseJob (file);
			}
		}
		
		void OnProjectModified (object s, SolutionItemModifiedEventArgs args)
		{
			UpdateCorlibReference ();
			if (UpdateCorlibReference ())
				SourceProjectDom.UpdateReferences ();
		}

		public void UpdateFromProject ()
		{
			Hashtable fs = new Hashtable ();
			foreach (ProjectFile file in project.Files)
			{
				if (GetFile (file.Name) == null) AddFile (file.Name);
				fs [file.Name] = null;
			}

			foreach (string file in files.Keys.ToArray ())
			{
				if (!fs.Contains (file))
					RemoveFile (file);
			}
			
			fs.Clear ();
			if (project is DotNetProject) {
				DotNetProject netProject = (DotNetProject) project;
				foreach (SolutionItem pr in netProject.GetReferencedItems (ConfigurationSelector.Default)) {
					if (pr is Project) {
						string refId = "Project:" + ((Project)pr).FileName;
						fs[refId] = null;
						if (!HasReference (refId))
							AddReference (refId);
					}
				}
				
				// Get the assembly references throught the project, since it may have custom references
				foreach (string file in netProject.GetReferencedAssemblies (ConfigurationSelector.Default, false)) {
					string refId = "Assembly:" + netProject.TargetRuntime.Id + ":" + Path.GetFullPath (file);
					fs[refId] = null;
					if (!HasReference (refId))
						AddReference (refId);
				}
			}
			
			foreach (ReferenceEntry re in References)
			{
				// Don't delete corlib references. They are implicit to projects, but not to pidbs.
				if (!fs.Contains (re.Uri) && !IsCorlibReference (re))
					RemoveReference (re.Uri);
			}
			UpdateCorlibReference ();
		}
		
		bool UpdateCorlibReference ()
		{
			// Creates a reference to the correct version of mscorlib, depending
			// on the target runtime version. Returns true if the references
			// have changed.
			
			DotNetProject prj = project as DotNetProject;
			if (prj == null) return false;

			// Look for an existing mscorlib reference
			string currentRefUri = null;
			foreach (ReferenceEntry re in References) {
				if (IsCorlibReference (re)) {
					currentRefUri = re.Uri;
					break;
				}
			}
			
			// Gets the name and version of the mscorlib assembly required by the project
			string requiredRefUri = "Assembly:" + prj.TargetRuntime.Id + ":";
			SystemAssembly asm = prj.TargetRuntime.AssemblyContext.GetAssemblyForVersion (typeof(object).Assembly.FullName, null, prj.TargetFramework);
			if (asm == null) {
				LoggingService.LogWarning ("mscorlib assembly not found for framework '" + prj.TargetFramework.Id + "'. The framework may not be installed.");
				return false;
			}
			requiredRefUri += asm.Location;
			
			// Replace the old reference if the target version has changed
			if (currentRefUri != null) {
				if (currentRefUri != requiredRefUri) {
					RemoveReference (currentRefUri);
					AddReference (requiredRefUri);
					return true;
				}
			} else {
				AddReference (requiredRefUri);
				return true;
			}
			return false;
		}
		
		bool IsCorlibReference (ReferenceEntry re)
		{
			TargetRuntime tr;
			TargetFramework fx;
			string file;
			if (ProjectDomService.ParseAssemblyUri (re.Uri, out tr, out fx, out file))
				return Path.GetFileNameWithoutExtension (file) == "mscorlib";
			else
				return false;
		}
		
		protected override void ParseFile (string fileName, IProgressMonitor monitor)
		{
			if (monitor != null) monitor.BeginTask (string.Format (GettextCatalog.GetString ("Parsing file: {0}"), Path.GetFileName (fileName)), 1);
			
			try {
				ProjectDomService.Parse (project, fileName);
				// The call to ProjectDomService.Parse will call UpdateFromParseInfo when done
			} finally {
				if (monitor != null) monitor.EndTask ();
			}
		}
		
		int totalUnresolvedCount;
		
		public TypeUpdateInformation UpdateFromParseInfo (ICompilationUnit parserInfo, string fileName, bool isFromFile)
		{
			lock (rwlock) {
				ICompilationUnit cu = parserInfo;
	
				List<IType> resolved;
				List<IAttribute> resolvedAtts;
				
				int unresolvedCount = ResolveTypes (cu, cu.Types, cu.Attributes, out resolved, out resolvedAtts);
				totalUnresolvedCount += unresolvedCount;
				
				TypeUpdateInformation res = UpdateTypeInformation (resolved, resolvedAtts, parserInfo.FileName);
				
				FileEntry file;
				if (files.TryGetValue (fileName, out file)) {
					if (unresolvedCount > 0) {
						if (file.ParseErrorRetries != 1) {
							file.ParseErrorRetries = 1;
							
							// Enqueue the file for quickly reparse. Types can't be resolved most probably because
							// the file that implements them is not yet parsed.
							
							if (isFromFile) {
								// To reduce memory usage, we don't keep the compilation unit in memory if the
								// content comes from a saved file. We'll just reparse the file.
								file.InParseQueue = false;
								QueueParseJob (file);
							} else {
								ProjectDomService.QueueParseJob (SourceProjectDom, 
								                                 delegate { UpdateFromParseInfo (parserInfo, fileName, false); },
								                                 file.FileName);
							}
						}
					}
					else {
						file.ParseErrorRetries = 0;
					}
				}
				
				if ((++parseCount % MAX_ACTIVE_COUNT) == 0)
					Flush ();
				return res;
			}
		}
		
		protected override void OnFileRemoved (string fileName, TypeUpdateInformation classInfo)
		{
			if (classInfo.Removed.Count > 0)
				ProjectDomService.NotifyTypeUpdate (project, fileName, classInfo);
		}
		
		protected internal override void ForceUpdateBROKEN ()
		{
			int lastCount;
			totalUnresolvedCount = int.MaxValue;

			do {
				// Keep trying updating the db while types are being resolved
				lastCount = totalUnresolvedCount;
				totalUnresolvedCount = 0;
				base.ForceUpdateBROKEN ();
			}
			while (totalUnresolvedCount != 0 && totalUnresolvedCount < lastCount);
		}
	}
}
