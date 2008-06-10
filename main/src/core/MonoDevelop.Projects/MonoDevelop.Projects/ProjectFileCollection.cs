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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using MonoDevelop.Core;

namespace MonoDevelop.Projects
{
	[Serializable()]
	public class ProjectFileCollection : Collection<ProjectFile> {
	
		Project project;
		List<ProjectFile> unresolvedDeps;
		
		public ProjectFileCollection ()
		{
		}
		
		public ProjectFileCollection (Project project)
		{
			this.project = project;
			DependencyResolutionEnabled = true;
		}
		
		public ProjectFile GetFile (string fileName)
		{
			if (fileName == null) return null;
			fileName = FileService.GetFullPath (fileName);
			
			foreach (ProjectFile file in this) {
				if (file.Name == fileName)
					return file;
			}
			return null;
		}
		
		public ProjectFile[] GetFilesInPath (string path)
		{
			path = path + Path.DirectorySeparatorChar;
			
			List<ProjectFile> list = new List<ProjectFile> ();
			foreach (ProjectFile file in Items) {
				if ((file.Name + Path.DirectorySeparatorChar).StartsWith (path))
					list.Add (file);
			}
			return list.ToArray ();
		}
		
		protected override void InsertItem (int index, ProjectFile value)
		{
			base.InsertItem (index, value);
			if (project != null) {
				if (value.Project != null)
					throw new InvalidOperationException ("ProjectFile already belongs to a project");
				value.SetProject (project);
				ResolveDependencies (value);
				project.NotifyFileAddedToProject (value);
			}
		}
		
		internal void ResolveDependencies (ProjectFile file)
		{
			if (!DependencyResolutionEnabled)
				return;
			
			if (!file.ResolveParent ())
				unresolvedDeps.Add (file);
			
			List<ProjectFile> resolved = null;
			foreach (ProjectFile unres in unresolvedDeps) {
				if (string.IsNullOrEmpty (unres.DependsOn )) {
					resolved.Add (unres);
				}
				if (unres.ResolveParent ()) {
					if (resolved == null)
						resolved = new List<ProjectFile> ();
						resolved.Add (unres);
				}
			}
			if (resolved != null)
				foreach (ProjectFile pf in resolved)
					unresolvedDeps.Remove (pf);
		}
		
		bool DependencyResolutionEnabled {
			set {
				if (value) {
					if (unresolvedDeps != null)
						return;
					
					unresolvedDeps = new List<ProjectFile> ();
					foreach (ProjectFile file in this)
						ResolveDependencies (file);
				} else {
					unresolvedDeps = null;
				}
			}
			get { return unresolvedDeps != null; }
		}
			
		public void AddRange (IEnumerable<ProjectFile> files)
		{
			foreach (ProjectFile pf in files)
				Add (pf);
		}
		
		protected override void RemoveItem (int index)
		{
			ProjectFile file = this [index];
			base.RemoveItem (index);
			if (project != null) {
				file.SetProject (null);
				project.NotifyFileRemovedFromProject (file);
			}
			
			if (DependencyResolutionEnabled) {
				if (unresolvedDeps.Contains (file))
					unresolvedDeps.Remove (file);
				foreach (ProjectFile f in file.DependentChildren) {
					f.DependsOnFile = null;
					if (!string.IsNullOrEmpty (f.DependsOn))
						unresolvedDeps.Add (f);
				}
				file.DependsOnFile = null;
			}
		}
		
		public void Remove (string fileName)
		{
			fileName = FileService.GetFullPath (fileName);
			for (int n=0; n<Count; n++) {
				if (Items [n].Name == fileName)
					RemoveAt (n);
			}
		}
	}
}
