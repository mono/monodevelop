//
// ProjectPackageCollection.cs: Collection of pkg-config packages for the project
//
// Authors:
//  Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com> 
//
// Copyright (C) 2008 Levi Bard
// Based on CBinding by Marcos David Marin Amador <MarcosMarin@gmail.com>
//
// This source code is licenced under The MIT License:
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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Mono.Addins;

namespace MonoDevelop.ValaBinding
{
	[Serializable()]
	public class ProjectPackageCollection : Collection<ProjectPackage>
	{
		private ValaProject project;
		
		internal ValaProject Project {
			get { return project; }
			set { project = value; }
		}
		
		public ProjectPackageCollection ()
		{
		}
		
		public void AddRange (IEnumerable<ProjectPackage> packages)
		{
			foreach (ProjectPackage p in packages) {
				bool found = false;
				foreach (ProjectPackage item in Items) {
					if (item.File == p.File) {
						found = true;
					}
				}
				if (!found) { Add (p); }
			}
		}
		
		protected override void ClearItems()
		{
			if (project != null) {
				List<ProjectPackage> list = new List<ProjectPackage> (Items);
				base.ClearItems ();
				foreach (ProjectPackage p in list) {
					project.NotifyPackageRemovedFromProject (p);
				}
			}
		}
		
		protected override void InsertItem (int index, ProjectPackage value)
		{
			base.InsertItem (index, value);
			if (project != null) {
				project.NotifyPackageAddedToProject (value);
			}
		}
		
		protected override void RemoveItem (int index)
		{
			ProjectPackage p = Items [index];
			base.RemoveItem (index);
			if (project != null) {
				project.NotifyPackageRemovedFromProject (p);
			}
		}
		
		protected override void SetItem (int index, ProjectPackage item)
		{
			ProjectPackage oldValue = Items [index];
			base.SetItem (index, item);
			if (project != null) {
				project.NotifyPackageRemovedFromProject (oldValue);
				project.NotifyPackageAddedToProject (item);
			}
		}

		public string[] ToStringArray ()
		{
			string[] array = new string[Count];
			int i = 0;
			
			foreach (ProjectPackage p in Items)
				array[i++] = p.Name;
			
			return array;
		}
	}
}
