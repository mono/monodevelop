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


namespace MonoDevelop.Ide.FindInFiles
{
	public abstract class Scope
	{
		public bool IncludeBinaryFiles {
			get;
			set;
		}
		
		public abstract int GetTotalWork (FilterOptions filterOptions);
		public abstract IEnumerable<FileProvider> GetFiles (IProgressMonitor monitor, FilterOptions filterOptions);
		public abstract string GetDescription (FilterOptions filterOptions, string pattern, string replacePattern);
	}

	public class DocumentScope : Scope
	{
		public override int GetTotalWork (FilterOptions filterOptions)
		{
			return 1;
		}

		public override IEnumerable<FileProvider> GetFiles (IProgressMonitor monitor, FilterOptions filterOptions)
		{
			monitor.Log.WriteLine (GettextCatalog.GetString ("Looking in '{0}'", IdeApp.Workbench.ActiveDocument.FileName));
			yield return new FileProvider(IdeApp.Workbench.ActiveDocument.FileName);
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
		public override int GetTotalWork (FilterOptions filterOptions)
		{
			return 1;
		}

		public override IEnumerable<FileProvider> GetFiles (IProgressMonitor monitor, FilterOptions filterOptions)
		{
			yield return new FileProvider(IdeApp.Workbench.ActiveDocument.FileName, null,
				IdeApp.Workbench.ActiveDocument.Editor.SelectionRange.Offset,
				IdeApp.Workbench.ActiveDocument.Editor.SelectionRange.EndOffset);
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
		
		public override IEnumerable<FileProvider> GetFiles (IProgressMonitor monitor, FilterOptions filterOptions)
		{
			if (IdeApp.Workspace.IsOpen) {
				foreach (Project project in IdeApp.Workspace.GetAllProjects ()) {
					monitor.Log.WriteLine (GettextCatalog.GetString ("Looking in project '{0}'", project.Name));
					foreach (ProjectFile file in project.Files.Where (f => filterOptions.NameMatches (f.Name) && File.Exists (f.Name))) {
						if (!IncludeBinaryFiles && !DesktopService.GetMimeTypeIsText (DesktopService.GetMimeTypeForUri (file.Name)))
							continue;
						yield return new FileProvider (file.Name, project);
					}
				}
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
		
		public override IEnumerable<FileProvider> GetFiles (IProgressMonitor monitor, FilterOptions filterOptions)
		{
			if (IdeApp.Workspace.IsOpen) {
				monitor.Log.WriteLine (GettextCatalog.GetString ("Looking in project '{0}'", project.Name));
				foreach (ProjectFile file in project.Files.Where (f => filterOptions.NameMatches (f.Name) && File.Exists (f.Name))) {
					if (!IncludeBinaryFiles && !DesktopService.GetMimeTypeIsText (DesktopService.GetMimeTypeForUri (file.Name)))
						continue;
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

		public override IEnumerable<FileProvider> GetFiles (IProgressMonitor monitor, FilterOptions filterOptions)
		{
			foreach (Document document in IdeApp.Workbench.Documents) {
				monitor.Log.WriteLine (GettextCatalog.GetString ("Looking in '{0}'", document.FileName));
				if (!string.IsNullOrEmpty (document.FileName))
					yield return new FileProvider (document.FileName);
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
		
		public bool IncludeHiddenFiles {
			get;
			set;
		}
		
		public override int GetTotalWork (FilterOptions filterOptions)
		{
			return GetFileNames (null, filterOptions).Count ();
		}
		
		public DirectoryScope (string path, bool recurse)
		{
			this.path = path;
			this.recurse = recurse;
		}
		
		IEnumerable<string> GetFileNames (IProgressMonitor monitor, FilterOptions filterOptions)
		{
			if (monitor != null)
				monitor.Log.WriteLine (GettextCatalog.GetString ("Looking in '{0}'", path));
			foreach (string fileMask in filterOptions.FileMask.Split (',', ';')) {
				string[] files;
				try {
					files = Directory.GetFiles (path, fileMask, recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
				} catch (Exception e) {
					LoggingService.LogError ("Can't access path " + path, e);
					yield break;
				}
				
				foreach (string fileName in files) {
					if (fileName.StartsWith (".") && !IncludeHiddenFiles)
						continue;
					if (!IncludeBinaryFiles && !DesktopService.GetMimeTypeIsText (DesktopService.GetMimeTypeForUri (fileName))) 
						continue;
					yield return fileName;
				}
			}
		}
		
		public override IEnumerable<FileProvider> GetFiles (IProgressMonitor monitor, FilterOptions filterOptions)
		{
			return GetFileNames (monitor, filterOptions).Select (file => new FileProvider (file));
		}

		public override string GetDescription (FilterOptions filterOptions, string pattern, string replacePattern)
		{
			if (replacePattern == null)
				return GettextCatalog.GetString ("Looking for '{0}' in directory '{1}'", pattern, path);
			return GettextCatalog.GetString ("Replacing '{0}' in directory '{1}'", pattern, path);
		}
	}
}
