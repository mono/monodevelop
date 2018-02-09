// ProjectFileCollection.cs
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
using System.IO;
using System.Linq;
using System.Collections.Generic;

using MonoDevelop.Core;
using System.Collections.Immutable;

namespace MonoDevelop.Projects
{
	class ProjectFileNode
	{
		public SortedList<string, ProjectFileNode> Children { get; private set; }
		public ProjectFileNode Parent { get; private set; }

		public ProjectFile ProjectFile { get; set; }
		public string FileName { get; set; }

		public ProjectFileNode () : this (null, string.Empty) { }

		public ProjectFileNode (ProjectFileNode parent, ProjectFile file)
		{
			Children = new SortedList<string, ProjectFileNode> ();
			FileName = file.ProjectVirtualPath.FileName;
			ProjectFile = file;
			Parent = parent;
		}

		public ProjectFileNode (ProjectFileNode parent, string fileName)
		{
			Children = new SortedList<string, ProjectFileNode> ();
			FileName = fileName;
			ProjectFile = null;
			Parent = parent;
		}

		ProjectFileNode Find (string[] path, int pathIndex, bool create)
		{
			ProjectFileNode child;

			if (Children.TryGetValue (path[pathIndex], out child)) {
				if (pathIndex + 1 == path.Length)
					return child;

				return child.Find (path, pathIndex + 1, create);
			}

			if (create) {
				child = new ProjectFileNode (this, path[pathIndex]);
				Children.Add (child.FileName, child);

				if (pathIndex + 1 == path.Length)
					return child;

				return child.Find (path, pathIndex + 1, create);
			}

			return null;
		}

		public ProjectFileNode Find (string vpath, bool create)
		{
			if (string.IsNullOrEmpty (vpath))
				return this;

			var path = vpath.Split (new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.None);

			return Find (path, 0, create);
		}

		public IEnumerable<ProjectFile> EnumerateProjectFiles (bool recursive)
		{
			foreach (var child in Children.Select (x => x.Value)) {
				if (child.ProjectFile != null)
					yield return child.ProjectFile;

				if (recursive) {
					foreach (var pf in child.EnumerateProjectFiles (recursive))
						yield return pf;
				}
			}

			yield break;
		}
	}

	[Serializable()]
	public class ProjectFileCollection : ProjectItemCollection<ProjectFile>
	{
		ImmutableDictionary<FilePath, ProjectFile> files;
		ProjectFileNode root;

		public ProjectFileCollection ()
		{
			files = ImmutableDictionary<FilePath, ProjectFile>.Empty;
			root = new ProjectFileNode ();
		}

		void ProjectVirtualPathChanged (object sender, ProjectFileVirtualPathChangedEventArgs e)
		{
			ProjectFileNode node;

			// Note: if the OldVirtualPath is null, then it means that a Project was just set on the ProjectFile
			// which means that it hasn't yet been added to our VirtualProjectPath tree.
			if (e.OldVirtualPath.IsNotNull) {
				node = root.Find (e.OldVirtualPath, false);
				if (node != null) {
					node.Parent.Children.Remove (node.FileName);
					PruneEmptyParents (node.Parent);
				}
			}

			node = root.Find (e.NewVirtualPath, true);
			node.ProjectFile = e.ProjectFile;
		}

		void FilePathChanged (object sender, ProjectFilePathChangedEventArgs e)
		{
			AssertCanWrite ();
			ProjectVirtualPathChanged (sender, e);
			files = files.Remove (e.OldPath);
			files = files.SetItem (e.NewPath, e.ProjectFile);
		}

		IEnumerable<KeyValuePair<FilePath, ProjectFile>> AddProjectFiles (IEnumerable<ProjectFile> items)
		{
			foreach (var item in items) {
				item.VirtualPathChanged += ProjectVirtualPathChanged;
				item.PathChanged += FilePathChanged;

				if (item.Project != null) {
					// Note: the ProjectVirtualPath is useless unless a Project is specified.
					var node = root.Find (item.ProjectVirtualPath, true);
					node.ProjectFile = item;
				}
				yield return new KeyValuePair<FilePath, ProjectFile> (item.FilePath, item);
			}
		}

		void PruneEmptyParents (ProjectFileNode node)
		{
			if (node.Children.Count > 0 || node.ProjectFile != null || node.Parent == null)
				return;

			node.Parent.Children.Remove (node.FileName);
			PruneEmptyParents (node.Parent);
		}

		void RemoveProjectFile (ProjectFile item)
		{
			var node = root.Find (item.ProjectVirtualPath, false);
			if (node != null) {
				node.Parent.Children.Remove (node.FileName);
				PruneEmptyParents (node.Parent);
			}

			files = files.Remove (item.FilePath);

			item.VirtualPathChanged -= ProjectVirtualPathChanged;
			item.PathChanged -= FilePathChanged;
		}

		#region ItemCollection<T>
		protected override void OnItemsAdded (IEnumerable<ProjectFile> items)
		{
			var pairs = AddProjectFiles (items);
			files = files.SetItems (pairs);
			base.OnItemsAdded (items);
		}

		protected override void OnItemsRemoved (IEnumerable<ProjectFile> items)
		{
			foreach (var item in items)
				RemoveProjectFile (item);
			base.OnItemsRemoved (items);
		}
		#endregion

		public ProjectFile GetFile (FilePath path)
		{
			if (path.IsNull)
				return null;

			ProjectFile pf;
			if (files.TryGetValue (path.FullPath, out pf))
				return pf;

			return null;
		}
		
		public ProjectFile GetFileWithVirtualPath (string virtualPath)
		{
			if (string.IsNullOrEmpty (virtualPath))
				return null;

			var node = root.Find (virtualPath, false);
			if (node != null && node.ProjectFile != null)
				return node.ProjectFile;

			return null;
		}
		
		public IEnumerable<ProjectFile> GetFilesInVirtualPath (string virtualPath)
		{
			if (string.IsNullOrEmpty (virtualPath))
				yield break;

			var node = root.Find (virtualPath, false);
			if (node == null)
				yield break;

			foreach (var pf in node.EnumerateProjectFiles (true))
				yield return pf;

			yield break;
		}
		
		public ProjectFile[] GetFilesInPath (FilePath path)
		{
			List<ProjectFile> list = new List<ProjectFile> ();
			foreach (ProjectFile file in this) {
				if (file.FilePath.IsChildPathOf (path))
					list.Add (file);
			}
			return list.ToArray ();
		}
		
		public void Remove (string fileName)
		{
			fileName = FileService.GetFullPath (fileName);
			for (int n = 0; n < Count; n++) {
				if (this[n].Name == fileName) {
					RemoveAt (n);
					break;
				}
			}
		}
	}
}
