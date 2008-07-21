
using System;
using System.Collections;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.Deployment
{
	public class PackageCollection: CollectionBase
	{
		PackagingProject project;
		
		public PackageCollection ()
		{
		}
		
		internal PackageCollection (PackagingProject project)
		{
			this.project = project;
		}
		
		protected override void OnInsertComplete (int index, object value)
		{
			if (project != null) {
				((Package)value).project = project;
				project.NotifyPackagesChanged ();
			}
		}
		
		protected override void OnSetComplete (int index, object oldValue, object newValue)
		{
			if (project != null) {
				((Package)newValue).project = project;
				project.NotifyPackagesChanged ();
			}
		}
		
		protected override void OnRemoveComplete (int index, object value)
		{
			if (project != null)
				project.NotifyPackagesChanged ();
		}

		protected override void OnClearComplete ()
		{
			if (project != null)
				project.NotifyPackagesChanged ();
		}


		public void Add (Package package)
		{
			List.Add (package);
		}
		
		public void Remove (Package package)
		{
			List.Remove (package);
		}
		
		public Package this [int n] {
			get { return (Package) List [n]; }
		}
	}
}
