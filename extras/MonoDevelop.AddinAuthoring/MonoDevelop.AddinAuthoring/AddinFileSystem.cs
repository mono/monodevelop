// 
// AddinFileSystem.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using System.Linq;
using System.Collections.Generic;
using MonoDevelop.Core;
using Mono.Addins.Database;
using System.IO;
using MonoDevelop.Projects;
using MonoDevelop.Ide;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.AddinAuthoring
{
	public class AddinFileSystem: AddinFileSystemExtension, IDisposable
	{
		DomAssemblyReflector reflector;
		Solution solution;
		Dictionary<FilePath,List<FilePath>> folders = new Dictionary<FilePath, List<FilePath>> ();
		Dictionary<FilePath,FilePath> fileMaps = new Dictionary<FilePath, FilePath> ();
		Dictionary<FilePath,DotNetProject> projectMaps = new Dictionary<FilePath, DotNetProject> ();
		Dictionary<DotNetProject,DateTime> projectTimestamps = new Dictionary<DotNetProject, DateTime> ();
		
		public event EventHandler Changed;
		
		public AddinFileSystem (Solution solution)
		{
			this.solution = solution;
			solution.FileChangedInProject += HandleSolutionFileChangedInProject;
			solution.EntrySaved += HandleSolutionEntrySaved;
			solution.SolutionItemRemoved += HandleSolutionSolutionItemRemoved;
			ProjectDomService.TypesUpdated += OnParseInfoChanged;
		}

		public void Dispose ()
		{
			solution.FileChangedInProject -= HandleSolutionFileChangedInProject;
			solution.EntrySaved -= HandleSolutionEntrySaved;
			ProjectDomService.TypesUpdated -= OnParseInfoChanged;
			solution.SolutionItemRemoved -= HandleSolutionSolutionItemRemoved;
			if (reflector != null)
				reflector.UnloadAssemblyDoms ();
		}

		void OnParseInfoChanged (object sender, MonoDevelop.Projects.Dom.TypeUpdateInformationEventArgs e)
		{
			if (e.Project is DotNetProject && e.Project.ParentSolution == solution) {
				projectTimestamps [(DotNetProject)e.Project] = DateTime.Now;
				OnChanged ();
			}
		}

		void HandleSolutionEntrySaved (object sender, SolutionItemEventArgs e)
		{
			if (e.SolutionItem is DotNetProject) {
				projectTimestamps [(DotNetProject)e.SolutionItem] = DateTime.Now;
				OnChanged ();
			}
		}

		void HandleSolutionFileChangedInProject (object sender, ProjectFileEventArgs e)
		{
			if (e.Project is DotNetProject) {
				projectTimestamps [(DotNetProject)e.Project] = DateTime.Now;
				OnChanged ();
			}
		}

		void HandleSolutionSolutionItemRemoved (object sender, SolutionItemChangeEventArgs e)
		{
			if (e.SolutionItem is DotNetProject) {
				projectTimestamps.Remove ((DotNetProject)e.SolutionItem);
				OnChanged ();
			}
		}
		
		void OnChanged ()
		{
			if (Changed != null)
				Changed (this, EventArgs.Empty);
		}
		
		public override bool RequiresIsolation {
			get {
				return false;
			}
		}
		
		public override void ScanStarted ()
		{
			base.ScanStarted ();
			
			folders.Clear ();
			fileMaps.Clear ();
			projectMaps.Clear ();
			
			// Locate all add-in folders
			
			foreach (DotNetProject p in solution.GetAllSolutionItems<DotNetProject> ()) {
				foreach (DotNetProjectConfiguration conf in p.Configurations) {
					
					FilePath asmFile = p.GetOutputFileName (conf.Selector);
					AddProjectMap (asmFile, p);
					
					// Map support files
					foreach (var file in p.GetSupportFileList (conf.Selector)) {
						FilePath tpath = conf.OutputDirectory.Combine (file.Target);
						if (file.Src != tpath)
							AddFileMap (tpath, file.Src);
					}
					
					// Map references to other projects
					foreach (ProjectReference pref in p.References) {
						if (pref.ReferenceType == ReferenceType.Project) {
							DotNetProject refProject = solution.FindProjectByName (pref.Reference) as DotNetProject;
							if (refProject != null) {
								FilePath refOutput = refProject.GetOutputFileName (conf.Selector);
								if (refOutput.IsNull)
									refOutput = refProject.GetOutputFileName (ConfigurationSelector.Default);
								if (!refOutput.IsNullOrEmpty)
									projectMaps [conf.OutputDirectory.Combine (refOutput.FileName)] = refProject;
							}
						}
					}
				}
			}
		}
		
		public override void ScanFinished ()
		{
			base.ScanFinished ();
			folders.Clear ();
			fileMaps.Clear ();
			projectMaps.Clear ();
			if (reflector != null)
				reflector.UnloadAssemblyDoms ();
		}
		
		void AddFileMap (FilePath src, FilePath dst)
		{
			fileMaps [src] = dst;
			RegPath (src);
		}
		
		void AddProjectMap (FilePath src, DotNetProject p)
		{
			projectMaps [src] = p;
			RegPath (src);
		}
		
		void RegPath (FilePath src)
		{
			List<FilePath> files;
			if (!folders.TryGetValue (src.ParentDirectory, out files))
				folders [src.ParentDirectory] = files = new List<FilePath> ();
			if (!files.Any (f => f == src))
				files.Add (src);
		}
		
		public override bool DirectoryExists (string dir)
		{
			FilePath path = dir;
			if (folders.ContainsKey (path) || base.DirectoryExists (path))
				return true;
			
			foreach (FilePath f in folders.Keys) {
				if (f.IsChildPathOf (dir))
					return true;
			}
			return false;
		}
		
		public override bool FileExists (string path)
		{
			return base.FileExists (path) || fileMaps.ContainsKey (path) || projectMaps.ContainsKey (path);
		}
		
		public override IEnumerable<string> GetDirectories (string path)
		{
			HashSet<FilePath> dirs = new HashSet<FilePath> ();
			
			if (base.DirectoryExists (path))
				dirs.UnionWith (base.GetDirectories (path).Select (s => (FilePath)s));
			
			FilePath dir = path;
			foreach (FilePath f in folders.Keys) {
				if (f.IsChildPathOf (dir)) {
					string s = f.ToRelative (dir);
					int i = s.IndexOf (Path.DirectorySeparatorChar);
					if (i != -1)
						dirs.Add (dir.Combine (s.Substring (0,i)));
					else
						dirs.Add (dir.Combine (s));
				}
			}
			return dirs.Select (p => (string)p);
		}
		
		public override IEnumerable<string> GetFiles (string path)
		{
			HashSet<FilePath> files = new HashSet<FilePath> ();
			if (base.DirectoryExists (path))
				files.UnionWith (base.GetFiles (path).Select (s => (FilePath)s));
			
			List<FilePath> dirFiles;
			if (folders.TryGetValue (path, out dirFiles))
				files.UnionWith (dirFiles);
			
			return files.Select (p => (string)p);
		}
		
		public override DateTime GetLastWriteTime (string filePath)
		{
			FilePath mapped;
			if (fileMaps.TryGetValue (filePath, out mapped))
				return base.GetLastWriteTime (mapped);
			
			DotNetProject p;
			if (projectMaps.TryGetValue (filePath, out p)) {
				DateTime t;
				if (!projectTimestamps.TryGetValue (p, out t))
					projectTimestamps [p] = t = DateTime.Now;
				return t;
			}
			
			return base.GetLastWriteTime (filePath);
		}
		
		public override Stream OpenFile (string path)
		{
			FilePath mapped;
			if (fileMaps.TryGetValue (path, out mapped))
				return base.OpenFile (mapped);
			return base.OpenFile (path);
		}
		
		public override IAssemblyReflector GetReflectorForFile (IAssemblyLocator locator, string path)
		{
			if (reflector == null)
				reflector = new DomAssemblyReflector (solution);
			return reflector;
		}
	}
}

