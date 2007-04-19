//
// PackageBuilder.cs
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
using MonoDevelop.Projects;

namespace MonoDevelop.Deployment
{
	[DataItem (FallbackType=typeof(UnknownPackageBuilder))]
	public class PackageBuilder: IDirectoryResolver
	{
		public PackageBuilder ()
		{
		}
		
		public virtual string Description {
			get { return GettextCatalog.GetString ("Package"); } 
		}
		
		public virtual string Icon {
			get { return "md-package"; }
		}
		
		public virtual string Validate ()
		{
			return null;
		}
		
		internal void Build (IProgressMonitor monitor, CombineEntry entry)
		{
			monitor.BeginTask ("Package: " + Description, 1);
			try {
				OnBuild (monitor, entry);
			} catch (Exception ex) {
				monitor.ReportError ("Package creation failed", ex);
				monitor.AsyncOperation.Cancel ();
			} finally {
				monitor.EndTask ();
			}
		}
		
		public virtual bool CanBuild (CombineEntry entry)
		{
			return true;
		}
		
		public virtual void InitializeSettings (CombineEntry entry)
		{
		}
		
		public PackageBuilder Clone ()
		{
			PackageBuilder d = (PackageBuilder) Activator.CreateInstance (GetType());
			d.CopyFrom (this);
			return d;
		}
		
		public virtual void CopyFrom (PackageBuilder other)
		{
		}
		
		protected virtual void OnBuild (IProgressMonitor monitor, CombineEntry entry)
		{
			if (entry is Combine)
				OnBuildCombine (monitor, (Combine) entry);
			else if (entry is Project)
				OnBuildProject (monitor, (Project) entry);
		}
		
		protected virtual void OnBuildCombine (IProgressMonitor monitor, Combine combine)
		{
			foreach (CombineEntry e in combine.Entries)
				DeployService.BuildPackage (monitor, e, this);
		}
		
		protected virtual void OnBuildProject (IProgressMonitor monitor, Project project)
		{
		}
		
		string IDirectoryResolver.GetDirectory (DeployContext ctx, string folderId)
		{
			return OnResolveDirectory (ctx, folderId);
		}
		
		protected virtual string OnResolveDirectory (DeployContext ctx, string folderId)
		{
			return null;
		}
	}
}
