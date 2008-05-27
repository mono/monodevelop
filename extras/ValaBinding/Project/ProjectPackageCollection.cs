//
// ProjectPackageCollection.cs: Collection of pkg-config packages for the project
//
// Authors:
//  Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com> 
//
// Copyright (C) 2008 Levi Bard
// Based on CBinding by Marcos David Marin Amador <MarcosMarin@gmail.com>
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU Lesser General Public License as published by
//    the Free Software Foundation, either version 2 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public License
//    along with this program.  If not, see <http://www.gnu.org/licenses/>.
//


using System;
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
			foreach (ProjectPackage p in packages)
				Add (p);
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
