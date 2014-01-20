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
		
		public abstract Task<int> GetTotalWork (FilterOptions filterOptions);
		public abstract Task GetFiles (ProgressMonitor monitor, FilterOptions filterOptions, ResultQueue<FileProvider> results);
		public abstract string GetDescription (FilterOptions filterOptions, string pattern, string replacePattern);
	}

	public class DocumentScope : Scope
	{
		public override Task<int> GetTotalWork (FilterOptions filterOptions)
		{
			return Task.FromResult (1);
		}

		public override Task GetFiles (ProgressMonitor monitor, FilterOptions filterOptions, ResultQueue<FileProvider> results)
		{
			monitor.Log.WriteLine (GettextCatalog.GetString ("Looking in '{0}'", IdeApp.Workbench.ActiveDocument.FileName));
			results.Enqueue (new FileProvider(IdeApp.Workbench.ActiveDocument.FileName));
			results.SetComplete ();
			return Task.FromResult (0);
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
		public override Task<int> GetTotalWork (FilterOptions filterOptions)
		{
			return Task.FromResult (1);
		}

		public override Task GetFiles (ProgressMonitor monitor, FilterOptions filterOptions, ResultQueue<FileProvider> results)
		{
			var selection = IdeApp.Workbench.ActiveDocument.Editor.SelectionRange;
			results.Enqueue (new FileProvider (IdeApp.Workbench.ActiveDocument.FileName, null, selection.Offset, selection.EndOffset));
			results.SetComplete ();
			return Task.FromResult (0);
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
		public override Task<int> GetTotalWork (FilterOptions filterOptions)
		{
			int result = 0;
			if (IdeApp.Workspace.IsOpen) 
				result = IdeApp.Workspace.GetAllProjects ().Sum (p => p.Files.Count);
			return Task.FromResult (result);
		}
		
		public async override Task GetFiles (ProgressMonitor monitor, FilterOptions filterOptions, ResultQueue<FileProvider> results)
		{
			if (IdeApp.Workspace.IsOpen) {
				var alreadyVisited = new HashSet<string> ();
				var allFiles = IdeApp.Workspace.GetAllSolutionItems ().OfType<SolutionFolder> ().SelectMany (sf => sf.Files).Where (f => filterOptions.NameMatches (f.FileName)).Select (f => f.FullPath).ToArray ();
				await Task.Factory.StartNew (delegate {
					foreach (var file in allFiles.Where (f => File.Exists (f.FullPath))) {
						if (!IncludeBinaryFiles && !DesktopService.GetFileIsText (file.FullPath))
							continue;
						if (alreadyVisited.Contains (file.FullPath))
							continue;
						alreadyVisited.Add (file.FileName);
						results.Enqueue (new FileProvider (file.FullPath));
					}
					results.SetComplete ();
				});

				var allProjectFiles = IdeApp.Workspace.GetAllProjects ().SelectMany (project => project.Files).Where (f => filterOptions.NameMatches (f.Name)).Select (f => new Tuple<Project,string>(f.Project,f.Name)).ToArray ();
				await Task.Factory.StartNew (delegate {
					foreach (var ft in allProjectFiles.Where (f => File.Exists (f.Item2))) {
						var file = ft.Item2;
						if (!IncludeBinaryFiles && !DesktopService.GetFileIsText (file))
							continue;
						if (alreadyVisited.Contains (file))
							continue;
						alreadyVisited.Add (file);
						results.Enqueue (new FileProvider (file, ft.Item1));
					}
					results.SetComplete ();
				});
			}
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
		
		public override Task<int> GetTotalWork (FilterOptions filterOptions)
		{
			return Task.FromResult (project.Files.Count);
		}
		
		public WholeProjectScope (Project project)
		{
			if (project == null)
				throw new ArgumentNullException ("project");
			
			this.project = project;
		}
		
		public override Task GetFiles (ProgressMonitor monitor, FilterOptions filterOptions, ResultQueue<FileProvider> results)
		{
			if (IdeApp.Workspace.IsOpen) {
				monitor.Log.WriteLine (GettextCatalog.GetString ("Looking in project '{0}'", project.Name));
				var alreadyVisited = new HashSet<string> ();
				var allFiles = project.Files.Where (f => filterOptions.NameMatches (f.Name) && File.Exists (f.Name)).Select (f => f.Name).ToArray ();
				return Task.Factory.StartNew (delegate {
					foreach (string file in allFiles) {
						if (!IncludeBinaryFiles && !DesktopService.GetFileIsText (file))
							continue;

						if (alreadyVisited.Contains (file))
							continue;
						alreadyVisited.Add (file);
						results.Enqueue (new FileProvider (file, project));
					}
					results.SetComplete ();
				});
			}
			return Task.FromResult (0);
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
		public override Task<int> GetTotalWork (FilterOptions filterOptions)
		{
			return Task.FromResult (IdeApp.Workbench.Documents.Count);
		}

		public override Task GetFiles (ProgressMonitor monitor, FilterOptions filterOptions, ResultQueue<FileProvider> results)
		{
			foreach (Document document in IdeApp.Workbench.Documents) {
				monitor.Log.WriteLine (GettextCatalog.GetString ("Looking in '{0}'", document.FileName));
				if (!string.IsNullOrEmpty (document.FileName) && filterOptions.NameMatches (document.FileName))
					results.Enqueue (new FileProvider (document.FileName));
			}
			results.SetComplete ();
			return Task.FromResult (0);
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
		
		public bool IncludeHiddenFiles {
			get;
			set;
		}
		
		public override Task<int> GetTotalWork (FilterOptions filterOptions)
		{
			return Task<int>.Factory.StartNew (delegate {
				return GetFileNames (null, filterOptions).Count ();
			});
		}
		
		public DirectoryScope (string path, bool recurse)
		{
			this.path = path;
			this.recurse = recurse;
		}
		
		IEnumerable<string> GetFileNames (ProgressMonitor monitor, FilterOptions filterOptions)
		{
			if (monitor != null)
				monitor.Log.WriteLine (GettextCatalog.GetString ("Looking in '{0}'", path));
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
		
		public override Task GetFiles (ProgressMonitor monitor, FilterOptions filterOptions, ResultQueue<FileProvider> results)
		{
			return Task.Factory.StartNew (delegate {
				foreach (var r in GetFileNames (monitor, filterOptions).Select (file => new FileProvider (file)))
					results.Enqueue (r);
				results.SetComplete ();
			});
		}

		public override string GetDescription (FilterOptions filterOptions, string pattern, string replacePattern)
		{
			if (replacePattern == null)
				return GettextCatalog.GetString ("Looking for '{0}' in directory '{1}'", pattern, path);
			return GettextCatalog.GetString ("Replacing '{0}' in directory '{1}'", pattern, path);
		}
	}
}
