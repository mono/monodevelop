//
// ProjectPackageCollection.cs: Collection of pkg-config packages for the prject
//
// Authors:
//   Marcos David Marin Amador <MarcosMarin@gmail.com>
//
// Copyright (C) 2007 Marcos David Marin Amador
//
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
using System.Collections;

using Mono.Addins;

namespace CBinding
{
	[Serializable()]
	public class ProjectPackageCollection : CollectionBase
	{
		private CProject project;
		
		internal CProject Project {
			get { return project; }
			set { project = value; }
		}
		
		public ProjectPackageCollection ()
		{
		}
		
		public ProjectPackage this[int index] {
			get { return (ProjectPackage)List[index]; }
			set { List[index] = value; }
		}
		
		public int Add (ProjectPackage package)
		{
			return List.Add (package);
		}
		
		public void AddRange (ProjectPackage[] packages)
		{
			foreach (ProjectPackage p in packages)
				List.Add (p);
		}
		
		public void AddRange (ProjectPackageCollection packages)
		{
			foreach (ProjectPackage p in packages)
				List.Add (p);
		}
		
		public bool Contains (ProjectPackage package)
		{
			return List.Contains (package);
		}
		
		public void CopyTo (ProjectPackage[] array, int index)
		{
			List.CopyTo (array, index);
		}
		
		public int IndexOf (ProjectPackage package)
		{
			return List.IndexOf (package);
		}
		
		public void Insert (int index, ProjectPackage package)
		{
			List.Insert (index, package);
		}
		
		public void Remove (ProjectPackage package)
		{
			List.Remove (package);
		}
		
		public string[] ToStringArray ()
		{
			string[] array = new string[Count];
			int i = 0;
			
			foreach (ProjectPackage p in List)
				array[i++] = p.Name;
			
			return array;
		}
		
		public new ProjectPackageEnumerator GetEnumerator ()
		{
			return new ProjectPackageEnumerator (this);
		}
		
		protected override void OnClear ()
		{
			if (project != null) {
				foreach (ProjectPackage package in (ArrayList)InnerList) {
					project.NotifyPackageRemovedFromProject (package);
				}
			}
		}
		
//		protected override void OnClearComplete ()
//		{
//			if (project != null) {
//				foreach (ProjectPackage package in (ArrayList)InnerList) {
//					project.NotifyPackageRemovedFromProject (package);
//				}
//			}
//		}
		
		protected override void OnInsertComplete (int index, object value)
		{
			if (project != null)
				project.NotifyPackageAddedToProject ((ProjectPackage)value);
		}
		
		protected override void OnRemoveComplete (int index, object value)
		{
			if (project != null) 
				project.NotifyPackageRemovedFromProject ((ProjectPackage)value);
		}
		
		protected override void OnSet (int index, object oldValue, object newValue)
		{
			if (project != null)
				project.NotifyPackageRemovedFromProject ((ProjectPackage)oldValue);
		}
		
		protected override void OnSetComplete (int index, object oldValue, object newValue)
		{
			if (project != null)
				project.NotifyPackageAddedToProject ((ProjectPackage)newValue);
		}
	}
	
	public class ProjectPackageEnumerator : IEnumerator
	{
		private IEnumerator enumerator;
		private IEnumerable temp;
		
		public ProjectPackageEnumerator (ProjectPackageCollection packages)
		{
			temp = (IEnumerable)packages;
			enumerator = temp.GetEnumerator ();
		}
		
		public ProjectPackage Current {
			get { return (ProjectPackage)enumerator.Current; }
		}
		
		object IEnumerator.Current {
			get { return enumerator.Current; }
		}
		
		public bool MoveNext ()
		{
			return enumerator.MoveNext ();
		}
		
		bool IEnumerator.MoveNext ()
		{
			return enumerator.MoveNext ();
		}
		
		public void Reset ()
		{
			enumerator.Reset ();
		}
		
		void IEnumerator.Reset ()
		{
			enumerator.Reset ();
		}
	}
}
