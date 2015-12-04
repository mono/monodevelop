// MonoSolutionItemHandler.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Extensions;

namespace MonoDeveloper
{
	public class MonoSolutionItemHandler: ISolutionItemHandler
	{
		DotNetProject project;
		string outFile;
		ArrayList refNames = new ArrayList ();
		bool loading;
		string testFileBase;
		object unitTest;
		
		public MonoSolutionItemHandler (DotNetProject project)
		{
			this.project = project;
			project.FileAddedToProject += OnFileAddedToProject;
			project.FileRemovedFromProject += OnFileRemovedFromProject;
			project.FileRenamedInProject += OnFileRenamedInProject;
		}
		
		public string SourcesFile {
			get { return outFile + ".sources"; }
		}
		
		public bool SyncFileName {
			get { return false; }
		}
		
		public string ItemId {
			get {
				if (project.ParentSolution != null)
					return project.ParentSolution.GetRelativeChildPath (project.FileName);
				else
					return project.Name;
			}
		}

		public void Save (MonoDevelop.Core.IProgressMonitor monitor)
		{
		}
		
		internal void Read (MonoMakefile mkfile)
		{
			loading = true;
			
			string basePath = Path.GetDirectoryName (mkfile.FileName);
			string aname;
			
			string targetAssembly = mkfile.GetVariable ("LIBRARY");
			if (targetAssembly == null) {
				targetAssembly = mkfile.GetVariable ("PROGRAM");
				if (Path.GetDirectoryName (targetAssembly) == "")
					targetAssembly = Path.Combine (basePath, targetAssembly);
				aname = Path.GetFileName (targetAssembly);
			} else {
				aname = Path.GetFileName (targetAssembly);
				string targetName = mkfile.GetVariable ("LIBRARY_NAME");
				if (targetName != null) targetAssembly = targetName;
				targetAssembly = "$(topdir)/class/lib/$(PROFILE)/" + targetAssembly;
			}
			
			outFile = Path.Combine (basePath, aname);
			project.FileName = mkfile.FileName;
			
			ArrayList checkedFolders = new ArrayList ();
			
			// Parse projects
			string sources = outFile + ".sources";
			StreamReader sr = new StreamReader (sources);
			string line;
			while ((line = sr.ReadLine ()) != null) {
				line = line.Trim (' ','\t');
				if (line != "") {
					string fname = Path.Combine (basePath, line);
					project.Files.Add (new ProjectFile (fname));
					
					string dir = Path.GetDirectoryName (fname);
					if (!checkedFolders.Contains (dir)) {
						checkedFolders.Add (dir);
						fname = Path.Combine (dir, "ChangeLog");
						if (File.Exists (fname))
							project.Files.Add (new ProjectFile (fname, BuildAction.Content));
					}
				}
			}
			
			sr.Close ();
			
			// Project references
			string refs = mkfile.GetVariable ("LIB_MCS_FLAGS");
			if (refs == null || refs == "") refs = mkfile.GetVariable ("LOCAL_MCS_FLAGS");
			
			if (refs != null && refs != "") {
				Regex var = new Regex(@"(.*?/r:(?<ref>.*?)(( |\t)|$).*?)*");
				Match match = var.Match (refs);
				if (match.Success) {
					foreach (Capture c in match.Groups["ref"].Captures)
						refNames.Add (Path.GetFileNameWithoutExtension (c.Value));
				}
			}
			
			int i = basePath.LastIndexOf ("/mcs/", basePath.Length - 2);
			string topdir = basePath.Substring (0, i + 4);
			targetAssembly = targetAssembly.Replace ("$(topdir)", topdir);
			
			if (mkfile.GetVariable ("NO_TEST") != "yes") {
				string tname = Path.GetFileNameWithoutExtension (aname) + "_test_";
				testFileBase = Path.Combine (basePath, tname);
			}
			
			foreach (string sconf in MonoMakefileFormat.Configurations) {
				DotNetProjectConfiguration conf = new DotNetProjectConfiguration (sconf);
				conf.CompilationParameters = project.LanguageBinding.CreateCompilationParameters (null);
				conf.OutputDirectory = basePath;
				conf.OutputAssembly = Path.GetFileName (targetAssembly);
				project.Configurations.Add (conf);
			}
			
			loading = false;
			IdeApp.Workspace.SolutionLoaded += CombineOpened;
		}
		
		public void CombineOpened (object sender, SolutionEventArgs args)
		{
			if (args.Solution == project.ParentSolution) {
				foreach (string pref in refNames) {
					Project p = project.ParentSolution.FindProjectByName (pref);
					if (p != null) project.References.Add (new ProjectReference (p));
				}
			}
		}

		public BuildResult RunTarget (MonoDevelop.Core.IProgressMonitor monitor, string target, ConfigurationSelector configuration)
		{
			if (target == ProjectService.BuildTarget)
				target = "all";
			else if (target == ProjectService.CleanTarget)
				target = "clean";
			
			DotNetProjectConfiguration conf = (DotNetProjectConfiguration) project.GetConfiguration (configuration);

			using (var output = new StringWriter ()) {
				using (var tw = new LogTextWriter ()) {
					tw.ChainWriter (output);
					tw.ChainWriter (monitor.Log);

					using (ProcessWrapper proc = Runtime.ProcessService.StartProcess ("make", "PROFILE=" + conf.Id + " " + target, conf.OutputDirectory, monitor.Log, tw, null))
						proc.WaitForOutput ();

					tw.UnchainWriter (output);
					tw.UnchainWriter (monitor.Log);

					var result = new BuildResult (output.ToString (), 1, 0);

					string[] lines = result.CompilerOutput.Split ('\n');
					foreach (string line in lines) {
						var err = CreateErrorFromString (line);
						if (err != null)
							result.Append (err);
					}

					return result;
				}
			}

		}
		
		private BuildError CreateErrorFromString (string error_string)
		{
			// When IncludeDebugInformation is true, prevents the debug symbols stats from braeking this.
			if (error_string.StartsWith ("WROTE SYMFILE") ||
			    error_string.StartsWith ("make[") ||
			    error_string.StartsWith ("OffsetTable") ||
			    error_string.StartsWith ("Compilation succeeded") ||
			    error_string.StartsWith ("Compilation failed"))
				return null;

			return BuildError.FromMSBuildErrorFormat (error_string);
		}
		
		void OnFileAddedToProject (object s, ProjectFileEventArgs args)
		{
			if (loading) return;
			
			foreach (ProjectFileEventInfo e in args) {
				if (e.ProjectFile.BuildAction != BuildAction.Compile)
					continue;
				AddSourceFile (e.ProjectFile.Name);
			}
		}
		
		void OnFileRemovedFromProject (object s, ProjectFileEventArgs args)
		{
			if (loading) return;
			
			foreach (ProjectFileEventInfo e in args) {
				if (e.ProjectFile.BuildAction != BuildAction.Compile)
					continue;

				RemoveSourceFile (e.ProjectFile.Name);
			}
		}
		
 		void OnFileRenamedInProject (object s, ProjectFileRenamedEventArgs args)
		{
			if (loading) return;
			
			foreach (ProjectFileRenamedEventInfo e in args) {
				if (e.ProjectFile.BuildAction != BuildAction.Compile)
					continue;
				
				if (RemoveSourceFile (e.OldName))
					AddSourceFile (e.NewName);
			}
		}

		void AddSourceFile (string sourceFile)
		{
			StreamReader sr = null;
			StreamWriter sw = null;
			
			try {
				sr = new StreamReader (outFile + ".sources");
				sw = new StreamWriter (outFile + ".sources.new");

				string newFile = project.GetRelativeChildPath (sourceFile);
				if (newFile.StartsWith ("./")) newFile = newFile.Substring (2);
				
				string line;
				while ((line = sr.ReadLine ()) != null) {
					string file = line.Trim (' ','\t');
					if (newFile != null && (file == "" || string.Compare (file, newFile) > 0)) {
						sw.WriteLine (newFile);
						newFile = null;
					}
					sw.WriteLine (line);
				}
				if (newFile != null)
					sw.WriteLine (newFile);
			} finally {
				if (sr != null) sr.Close ();
				if (sw != null) sw.Close ();
			}
			File.Delete (outFile + ".sources");
			File.Move (outFile + ".sources.new", outFile + ".sources");
		}
		
		bool RemoveSourceFile (string sourceFile)
		{
			StreamReader sr = null;
			StreamWriter sw = null;
			bool found = false;

			try {
				sr = new StreamReader (outFile + ".sources");
				sw = new StreamWriter (outFile + ".sources.new");

				string oldFile = project.GetRelativeChildPath (sourceFile);
				if (oldFile.StartsWith ("./")) oldFile = oldFile.Substring (2);
				
				string line;
				while ((line = sr.ReadLine ()) != null) {
					string file = line.Trim (' ','\t');
					if (oldFile != file)
						sw.WriteLine (line);
					else
						found = true;
				}
			} finally {
				if (sr != null) sr.Close ();
				if (sw != null) sw.Close ();
			}
			if (found) {
				File.Delete (outFile + ".sources");
				File.Move (outFile + ".sources.new", outFile + ".sources");
			}
			return found;
		}
		
		public void Dispose ()
		{
			project.FileAddedToProject -= OnFileAddedToProject;
			project.FileRemovedFromProject -= OnFileRemovedFromProject;
			project.FileRenamedInProject -= OnFileRenamedInProject;
			IdeApp.Workspace.SolutionLoaded -= CombineOpened;
		}

		public void OnModified (string hint)
		{
		}
		
		public string GetTestFileBase ()
		{
			return testFileBase;
		}
		
		public object UnitTest {
			get { return unitTest; }
			set { unitTest = value; }
		}

		public object GetService (Type t)
		{
			return null;
		}
	}
}
