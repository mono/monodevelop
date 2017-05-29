// 
// Scope.cs
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
using System.Linq;
using System.IO;
using System.Collections.Generic;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using System.Security.Permissions;
using System.Security;
using System.Threading.Tasks;

namespace MonoDevelop.Ide.FindInFiles
{
	public abstract class Scope
	{
		public bool IncludeBinaryFiles {
			get;
			set;
		}

		public virtual PathMode PathMode {
			get {
				var workspace = IdeApp.Workspace;
				var solutions = workspace != null ? workspace.GetAllSolutions () : null;

				if (solutions != null && solutions.Count () == 1)
					return PathMode.Relative;

				return PathMode.Absolute;
			}
		}

		public abstract int GetTotalWork (FilterOptions filterOptions);
		public abstract IEnumerable<FileProvider> GetFiles (ProgressMonitor monitor, FilterOptions filterOptions);
		public abstract string GetDescription (FilterOptions filterOptions, string pattern, string replacePattern);
	}

	public class DocumentScope : Scope
	{
		public override PathMode PathMode {
			get { return PathMode.Hidden; }
		}

		public override int GetTotalWork (FilterOptions filterOptions)
		{
			return 1;
		}

		public override IEnumerable<FileProvider> GetFiles (ProgressMonitor monitor, FilterOptions filterOptions)
		{
			monitor.Log.WriteLine (GettextCatalog.GetString ("Looking in '{0}'", IdeApp.Workbench.ActiveDocument.FileName));
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc.Editor != null)
				yield return new OpenFileProvider (doc.Editor, doc.Project);
		}

		public override string GetDescription(FilterOptions filterOptions, string pattern, string replacePattern)
		{
			if (replacePattern == null)
				return GettextCatalog.GetString("Looking for '{0}' in current document", pattern);
			return GettextCatalog.GetString("Replacing '{0}' in current document", pattern);
		}

	}

	public class SelectionScope : Scope
	{
		public override PathMode PathMode {
			get { return PathMode.Hidden; }
		}

		public override int GetTotalWork (FilterOptions filterOptions)
		{
			return 1;
		}

		public override IEnumerable<FileProvider> GetFiles (ProgressMonitor monitor, FilterOptions filterOptions)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc.Editor != null) {
				var selection = doc.Editor.SelectionRange;
				yield return new OpenFileProvider (doc.Editor, doc.Project, selection.Offset, selection.EndOffset);
			}
		}

		public override string GetDescription(FilterOptions filterOptions, string pattern, string replacePattern)
		{
			if (replacePattern == null)
				return GettextCatalog.GetString("Looking for '{0}' in current selection", pattern);
			return GettextCatalog.GetString("Replacing '{0}' in current selection", pattern);
		}

	}

	public class WholeSolutionScope : Scope
	{
		public override int GetTotalWork (FilterOptions filterOptions)
		{
			int result = 0;
			if (IdeApp.Workspace.IsOpen) 
				result = IdeApp.Workspace.GetAllProjects ().Sum (p => p.Files.Count);
			return result;
		}

		public override IEnumerable<FileProvider> GetFiles (ProgressMonitor monitor, FilterOptions filterOptions)
		{
			if (!IdeApp.Workspace.IsOpen) {
				return null;
			}

			var alreadyVisited = new HashSet<string> ();
			var results = new List<FileProvider> ();

			var options = new ParallelOptions ();
			options.MaxDegreeOfParallelism = 4;

			Parallel.ForEach (IdeApp.Workspace.GetAllSolutionItems ().OfType<SolutionFolder> (),
							  options,
							  () => new List<FileProvider> (),
							  (folder, loop, providers) => {
								  foreach (var file in folder.Files.Where (f => filterOptions.NameMatches (f.FileName) && File.Exists (f.FullPath))) {
									  if (!IncludeBinaryFiles && !DesktopService.GetFileIsText (file.FullPath))
										  continue;
									  lock (alreadyVisited) {
										  if (alreadyVisited.Contains (file.FullPath))
											  continue;
										  alreadyVisited.Add (file.FullPath);
									  }
									  providers.Add (new FileProvider (file.FullPath));
								  }
								  return providers;
							  },
							  (providers) => {
								  lock (results) {
									  results.AddRange (providers);
								  }
							  });

			Parallel.ForEach (IdeApp.Workspace.GetAllProjects (),
							  options,
							  () => new List<FileProvider> (),
							  (project, loop, providers) => {
								  var conf = project.DefaultConfiguration?.Selector;

								  foreach (ProjectFile file in project.GetSourceFilesAsync (conf).Result.Where (f => filterOptions.NameMatches (f.Name) && File.Exists (f.Name))) {
									  if ((file.Flags & ProjectItemFlags.Hidden) == ProjectItemFlags.Hidden)
										  continue;
									  if (!IncludeBinaryFiles && !DesktopService.GetFileIsText (file.FilePath))
										  continue;

									  lock (alreadyVisited) {
										  if (alreadyVisited.Contains (file.FilePath.FullPath))
											  continue;
										  alreadyVisited.Add (file.FilePath.FullPath);
									  }

									  providers.Add (new FileProvider (file.Name, project));
								  }
								  return providers;
							  },
							  (providers) => {
								  lock (results) {
									  results.AddRange (providers);
								  }
							  });

			return results;
		}

		public override string GetDescription (FilterOptions filterOptions, string pattern, string replacePattern)
		{
			if (replacePattern == null)
				return GettextCatalog.GetString ("Looking for '{0}' in all projects", pattern);
			return GettextCatalog.GetString ("Replacing '{0}' in all projects", pattern);
		}
	}

	public class WholeProjectScope : Scope
	{
		readonly Project project;

		public override int GetTotalWork (FilterOptions filterOptions)
		{
			return project.Files.Count;
		}

		public WholeProjectScope (Project project)
		{
			if (project == null)
				throw new ArgumentNullException ("project");

			this.project = project;
		}

		public override IEnumerable<FileProvider> GetFiles (ProgressMonitor monitor, FilterOptions filterOptions)
		{
			if (IdeApp.Workspace.IsOpen) {
				monitor.Log.WriteLine (GettextCatalog.GetString ("Looking in project '{0}'", project.Name));
				var alreadyVisited = new HashSet<string> ();
				var conf = project.DefaultConfiguration?.Selector;
				foreach (ProjectFile file in project.GetSourceFilesAsync (conf).Result.Where (f => filterOptions.NameMatches (f.Name) && File.Exists (f.Name))) {
					if ((file.Flags & ProjectItemFlags.Hidden) == ProjectItemFlags.Hidden)
						continue;
					if (!IncludeBinaryFiles && !DesktopService.GetFileIsText (file.Name))
						continue;
					if (alreadyVisited.Contains (file.FilePath.FullPath))
						continue;
					alreadyVisited.Add (file.FilePath.FullPath);
					yield return new FileProvider (file.Name, project);
				}
			}
		}

		public override string GetDescription (FilterOptions filterOptions, string pattern, string replacePattern)
		{
			if (replacePattern == null)
				return GettextCatalog.GetString ("Looking for '{0}' in project '{1}'", pattern, project.Name);
			return GettextCatalog.GetString ("Replacing '{0}' in project '{1}'", pattern, project.Name);
		}
	}


	public class AllOpenFilesScope : Scope
	{
		public override int GetTotalWork (FilterOptions filterOptions)
		{
			return IdeApp.Workbench.Documents.Count;
		}

		public override IEnumerable<FileProvider> GetFiles (ProgressMonitor monitor, FilterOptions filterOptions)
		{
			foreach (Document document in IdeApp.Workbench.Documents) {
				monitor.Log.WriteLine (GettextCatalog.GetString ("Looking in '{0}'", document.FileName));
				if (document.Editor != null && filterOptions.NameMatches (document.FileName))
					yield return new OpenFileProvider (document.Editor, document.Project);
			}
		}

		public override string GetDescription (FilterOptions filterOptions, string pattern, string replacePattern)
		{
			if (replacePattern == null)
				return GettextCatalog.GetString ("Looking for '{0}' in all open documents", pattern);
			return GettextCatalog.GetString ("Replacing '{0}' in all open documents", pattern);
		}
	}


	public class DirectoryScope : Scope
	{
		readonly string path;
		readonly bool recurse;

		public override PathMode PathMode {
			get { return PathMode.Absolute; }
		}

		public bool IncludeHiddenFiles {
			get;
			set;
		}

		FileProvider[] fileNames;
		public override int GetTotalWork (FilterOptions filterOptions)
		{
			fileNames = GetFileNames (filterOptions).Select (file => new FileProvider (file)).ToArray ();
			return fileNames.Length;
		}

		public DirectoryScope (string path, bool recurse)
		{
			this.path = path;
			this.recurse = recurse;
		}

		IEnumerable<string> GetFileNames (FilterOptions filterOptions)
		{
			var directoryStack = new Stack<string> ();
			directoryStack.Push (path);

			while (directoryStack.Count > 0) {
				var curPath = directoryStack.Pop ();

				try {
					var readPermission = new FileIOPermission(FileIOPermissionAccess.Read, curPath);
					readPermission.Demand ();
				} catch (Exception e) {
					LoggingService.LogError ("Can't access path " + curPath, e);
					yield break;
				}

				foreach (string fileName in Directory.EnumerateFiles (curPath, "*")) {
					if (!IncludeHiddenFiles) {
						if (Platform.IsWindows) {
							var attr = File.GetAttributes (fileName);
							if (attr.HasFlag (FileAttributes.Hidden))
								continue;
						}
						if (Path.GetFileName (fileName).StartsWith (".", StringComparison.Ordinal))
							continue;
					}
					if (!filterOptions.NameMatches (fileName))
						continue;
					if (!IncludeBinaryFiles && !DesktopService.GetFileIsText (fileName))
						continue;
					yield return fileName;
				}

				if (recurse) {
					foreach (string directoryName in Directory.EnumerateDirectories (curPath)) {
						if (!IncludeHiddenFiles) {
							if (Platform.IsWindows) {
								var attr = File.GetAttributes (directoryName);
								if (attr.HasFlag (FileAttributes.Hidden))
									continue;
							}
							if (Path.GetFileName (directoryName).StartsWith (".", StringComparison.Ordinal))
								continue;
						}
						directoryStack.Push (directoryName);
					}
				}

			}
		}

		public override IEnumerable<FileProvider> GetFiles (ProgressMonitor monitor, FilterOptions filterOptions)
		{
			monitor.Log.WriteLine (GettextCatalog.GetString ("Looking in '{0}'", path));
			return fileNames;
		}

		public override string GetDescription (FilterOptions filterOptions, string pattern, string replacePattern)
		{
			if (replacePattern == null)
				return GettextCatalog.GetString ("Looking for '{0}' in directory '{1}'", pattern, path);
			return GettextCatalog.GetString ("Replacing '{0}' in directory '{1}'", pattern, path);
		}
	}
}