// ProjectReferenceCollection.cs
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

namespace MonoDevelop.Projects
{
	[Serializable()]
	public class ProjectReferenceCollection : Collection<ProjectReference>
	{
		DotNetProject project;
		
		public ProjectReferenceCollection()
		{
		}
		
		internal void SetProject (DotNetProject project)
		{
			this.project = project;
		}
		
		public void AddRange (IEnumerable<ProjectReference> references)
		{
			foreach (ProjectReference pr in references)
				Add (pr);
		}
		
		protected override void ClearItems()
		{
			if (project != null) {
				List<ProjectReference> list = new List<ProjectReference> (Items);
				base.ClearItems ();
				foreach (ProjectReference pref in list) {
					pref.SetOwnerProject (null);
					project.NotifyReferenceRemovedFromProject (pref);
				}
			}
		}
		
		protected override void InsertItem (int index, ProjectReference value)
		{
			base.InsertItem (index, value);
			if (project != null) {
				value.SetOwnerProject (project);
				project.NotifyReferenceAddedToProject (value);
			}
		}
		
		protected override void RemoveItem (int index)
		{
			ProjectReference pr = Items [index];
			base.RemoveItem (index);
			if (project != null) {
				pr.SetOwnerProject (null);
				project.NotifyReferenceRemovedFromProject (pr);
			}
		}
		
		protected override void SetItem (int index, ProjectReference item)
		{
			ProjectReference oldValue = Items [index];
			base.SetItem (index, item);
			if (project != null) {
				oldValue.SetOwnerProject (null);
				project.NotifyReferenceRemovedFromProject (oldValue);
				item.SetOwnerProject (project);
				project.NotifyReferenceAddedToProject (item);
			}
		}
	}
}
