//
// DeployTarget.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Deployment
{
	[DataItem (FallbackType=typeof(UnknownDeployTarget))]
	public class DeployTarget
	{
		[ItemProperty ("Name")]
		string name;
		
		CombineEntry entry;
		
		public DeployTarget ()
		{
		}
		
		public virtual string Description {
			get { return GettextCatalog.GetString ("Deploy Target"); } 
		}
		
		public virtual string Icon {
			get { return "md-package"; }
		}
		
		public void Deploy (IProgressMonitor monitor)
		{
			monitor.BeginTask ("Deploy operation: " + Name, 1);
			try {
				OnDeploy (monitor);
			} catch (Exception ex) {
				monitor.ReportError ("Deploy operation failed", ex);
				monitor.AsyncOperation.Cancel ();
			} finally {
				monitor.EndTask ();
			}
		}
		
		public virtual bool CanDeploy (CombineEntry entry)
		{
			return true;
		}
		
		public string Name {
			get { return name; }
			set { name = value; }
		}
		
		public CombineEntry CombineEntry {
			get {
				return entry; 
			}
			internal set {
				if (entry != value) {
					entry = value;
					if (entry != null)
						OnInitialize (entry);
				}
			}
		}
		
		public DeployTarget Clone ()
		{
			DeployTarget d = (DeployTarget) Activator.CreateInstance (GetType());
			d.CopyFrom (this);
			return d;
		}
		
		public virtual void CopyFrom (DeployTarget other)
		{
			name = other.name;
			entry = other.entry;
		}
		
		public virtual string GetDefaultName (CombineEntry entry)
		{
			string bname = Description;
			string name = bname;
			
			int n = 2;
			while (true) {
				bool found = false;
				foreach (DeployTarget dt in entry.DeployTargets) {
					if (dt.Name == name) {
						found = true;
						break;
					}
				}
				if (found) {
					name = bname + " " + n;
					n++;
				} else
					break;
			}

			return name;
		}
		
		protected virtual void OnInitialize (CombineEntry entry)
		{
		}
		
		protected virtual void OnDeploy (IProgressMonitor monitor)
		{
			OnDeployCombineEntry (monitor, entry);
		}
		
		protected virtual void OnDeployCombineEntry (IProgressMonitor monitor, CombineEntry entry)
		{
			if (entry is Combine)
				OnDeployCombine (monitor, (Combine) entry);
			else if (entry is Project)
				OnDeployProject (monitor, (Project) entry);
		}
		
		protected virtual void OnDeployCombine (IProgressMonitor monitor, Combine combine)
		{
			foreach (CombineEntry e in combine.Entries)
				OnDeployCombineEntry (monitor, e);
		}
		
		protected virtual void OnDeployProject (IProgressMonitor monitor, Project project)
		{
		}
	}
	
	public class DeployTargetCollection: CollectionBase
	{
		CombineEntry owner;
		
		internal DeployTargetCollection (CombineEntry e)
		{
			owner = e;
		}
		
		public DeployTargetCollection ()
		{
		}
		
		public void Add (DeployTarget target)
		{
			List.Add (target);
		}
		
		public void AddRange (ICollection col)
		{
			foreach (DeployTarget tar in col)
				Add (tar);
		}
		
		public DeployTarget this [int n] {
			get { return (DeployTarget) List [n]; }
		}
		
		protected override void OnInsert (int index, object value)
		{
			((DeployTarget)value).CombineEntry = owner;
		}

		protected override void OnSet (int index, object oldValue, object newValue)
		{
			((DeployTarget)newValue).CombineEntry = owner;
		}
	}
}
