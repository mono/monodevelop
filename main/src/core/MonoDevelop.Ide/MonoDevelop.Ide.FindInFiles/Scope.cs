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
		public abstract int GetTotalWork (FilterOptions filterOptions);
		public abstract IEnumerable<FileProvider> GetFiles (FilterOptions filterOptions);
		public abstract string GetDescription (FilterOptions filterOptions, string pattern, string replacePattern);
	}

	public class DocumentScope : Scope
	{
		public override int GetTotalWork (FilterOptions filterOptions)
		{
			return 1;
		}
		
		public DocumentScope()
		{
		}
		
		public override IEnumerable<FileProvider> GetFiles(FilterOptions filterOptions)
		{
			yield return new FileProvider(IdeApp.Workbench.ActiveDocument.FileName);
		}

		public override string GetDescription(FilterOptions filterOptions, string pattern, string replacePattern)
		{
			if (string.IsNullOrEmpty(replacePattern))
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
		
		public SelectionScope()
		{
		}

		public override IEnumerable<FileProvider> GetFiles(FilterOptions filterOptions)
		{
			yield return new FileProvider(IdeApp.Workbench.ActiveDocument.FileName, null,
				IdeApp.Workbench.ActiveDocument.TextEditor.SelectionStartPosition,
				IdeApp.Workbench.ActiveDocument.TextEditor.SelectionEndPosition);
		}

		public override string GetDescription(FilterOptions filterOptions, string pattern, string replacePattern)
		{
			if (string.IsNullOrEmpty(replacePattern))
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
		
		public override IEnumerable<FileProvider> GetFiles (FilterOptions filterOptions)
		{
			if (IdeApp.Workspace.IsOpen) {
				foreach (Project project in IdeApp.Workspace.GetAllProjects ()) {
					foreach (ProjectFile file in project.Files) {
						if (filterOptions.NameMatches (file.Name))
							yield return new FileProvider (file.Name, project);
					}
				}
			}
		}
		
		public override string GetDescription (FilterOptions filterOptions, string pattern, string replacePattern)
		{
			if (string.IsNullOrEmpty (replacePattern))
				return GettextCatalog.GetString ("Looking for '{0}' in all projects", pattern);
			return GettextCatalog.GetString ("Replacing '{0}' in all projects", pattern);
		}
	}
	
	public class WholeProjectScope : Scope
	{
		Project project;
		
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
		
		public override IEnumerable<FileProvider> GetFiles (FilterOptions filterOptions)
		{
			if (IdeApp.Workspace.IsOpen) {
				foreach (ProjectFile file in project.Files) {
					if (filterOptions.NameMatches (file.Name))
						yield return new FileProvider (file.Name, project);
				}
			}
		}
		
		public override string GetDescription (FilterOptions filterOptions, string pattern, string replacePattern)
		{
			if (string.IsNullOrEmpty (replacePattern))
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
		
		public AllOpenFilesScope ()
		{
		}
		
		public override IEnumerable<FileProvider> GetFiles (FilterOptions filterOptions)
		{
			foreach (Document document in IdeApp.Workbench.Documents) {
				if (!string.IsNullOrEmpty (document.FileName))
					yield return new FileProvider (document.FileName);
			}
		}
		
		public override string GetDescription (FilterOptions filterOptions, string pattern, string replacePattern)
		{
			if (string.IsNullOrEmpty (replacePattern))
				return GettextCatalog.GetString ("Looking for '{0}' in all open documents", pattern);
			return GettextCatalog.GetString ("Replacing '{0}' in all open documents", pattern);
		}
	}
	
	
	public class DirectoryScope : Scope
	{
		string path;
		bool recurse;
		
		public override int GetTotalWork (FilterOptions filterOptions)
		{
			int result = 0;
			foreach (string fileMask in filterOptions.FileMask.Split (',', ';')) {
				string[] files = Directory.GetFiles (path, fileMask, recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
				result += files.Length;
			}
			return result;
		}
		
		public DirectoryScope (string path, bool recurse)
		{
			this.path = path;
			this.recurse = recurse;
		}
		
		public override IEnumerable<FileProvider> GetFiles (FilterOptions filterOptions)
		{
			foreach (string fileMask in filterOptions.FileMask.Split (',', ';')) {
				foreach (string file in Directory.GetFiles (path, fileMask, recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)) {
					yield return new FileProvider (file);
				}
			}
		}
		
		public override string GetDescription (FilterOptions filterOptions, string pattern, string replacePattern)
		{
			if (string.IsNullOrEmpty (replacePattern))
				return GettextCatalog.GetString ("Looking for '{0}' in directory '{1}'", pattern, path);
			return GettextCatalog.GetString ("Replacing '{0}' in directory '{1}'", pattern, path);
		}
	}
}
