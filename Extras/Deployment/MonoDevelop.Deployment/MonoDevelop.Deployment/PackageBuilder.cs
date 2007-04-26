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
using System.Collections.Generic;
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Deployment
{
	[DataItem (FallbackType=typeof(UnknownPackageBuilder))]
	public class PackageBuilder: IDirectoryResolver
	{
		[ItemProperty ("ChildEntries")]
		[ProjectPathItemProperty ("Entry", Scope=1)]
		List<string> childEntries = new List<string> ();
		
		[ItemProperty ("ExcludedFiles")]
		[ItemProperty ("File", Scope=1)]
		List<string> excludedFiles;
		
		[ProjectPathItemProperty]
		string rootEntry;
		
		List<CombineEntry> childCombineEntries;
		CombineEntry rootCombineEntry;
		
		public PackageBuilder ()
		{
		}
		
		public virtual string Description {
			get { return GettextCatalog.GetString ("Package"); } 
		}
		
		public virtual string Icon {
			get { return "md-package"; }
		}
		
		public virtual string DefaultName {
			get {
				return Description;
			}
		}
		
		public virtual string Validate ()
		{
			return null;
		}
		
		internal void Build (IProgressMonitor monitor)
		{
			monitor.BeginTask ("Package: " + Description, 1);
			DeployContext ctx = null;
			try {
				ctx = CreateDeployContext ();
				if (ctx != null)
					ctx.FileFilter = this;
				OnBuild (monitor, ctx);
			} catch (Exception ex) {
				monitor.ReportError ("Package creation failed", ex);
				monitor.AsyncOperation.Cancel ();
			} finally {
				monitor.EndTask ();
				if (ctx != null)
					ctx.Dispose ();
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
			childEntries = new List<string> (other.childEntries);
			rootEntry = other.rootEntry;
			if (other.childCombineEntries != null)
				childCombineEntries = new List<CombineEntry> (other.childCombineEntries);
			else
				childCombineEntries = null;
			if (other.excludedFiles != null)
				excludedFiles = new List<string> (other.excludedFiles);
			else
				excludedFiles = null;
			rootCombineEntry = other.rootCombineEntry;
		}
		
		public virtual PackageBuilder[] CreateDefaultBuilders ()
		{
			return new PackageBuilder [0];
		}
		
		protected virtual void OnBuild (IProgressMonitor monitor, DeployContext ctx)
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
		
		public virtual DeployContext CreateDeployContext ()
		{
			return new DeployContext (this, "Linux", null);
		}
		
		public void SetCombineEntry (CombineEntry entry)
		{
			SetCombineEntry (entry, null);
		}
		
		public void SetCombineEntry (CombineEntry rootCombineEntry, CombineEntry[] childEntries)
		{
			this.rootCombineEntry = rootCombineEntry;
			childCombineEntries = new List<CombineEntry> ();
			
			if (childEntries != null)
			    childCombineEntries.AddRange (childEntries);
			
			UpdateEntryNames ();
			InitializeSettings (rootCombineEntry);
		}
		
		void UpdateEntryNames ()
		{
			this.rootEntry = rootCombineEntry.FileName;
			this.childEntries.Clear ();
			foreach (CombineEntry e in childCombineEntries)
				childEntries.Add (e.FileName);
		}
		
		public CombineEntry RootCombineEntry {
			get {
				if (rootCombineEntry == null && rootEntry != null) {
					rootCombineEntry = GetEntry (rootEntry);
				}
				return rootCombineEntry; 
			}
		}
		
		public void AddEntry (CombineEntry entry)
		{
			string fp = Runtime.FileService.GetFullPath (entry.FileName);
			foreach (string s in childEntries)
				if (Runtime.FileService.GetFullPath (s) == fp)
					return;
			
			if (Runtime.FileService.GetFullPath (rootEntry) == fp)
				return;
			
			List<CombineEntry> list = new List<CombineEntry> ();
			list.Add (RootCombineEntry);
			list.AddRange (GetChildEntries());
			list.Add (entry);
			
			rootCombineEntry = GetCommonCombineEntry (list);
			list.Remove (rootCombineEntry);
			
			foreach (CombineEntry e in list.ToArray ()) {
				CombineEntry ce = e.ParentCombine;
				while (ce != rootCombineEntry) {
					if (!list.Contains (ce))
						list.Add (ce);
					ce = ce.ParentCombine;
				}
			}
			childCombineEntries = list;
			UpdateEntryNames ();
		}
		
		public CombineEntry[] GetChildEntries ()
		{
			if (childCombineEntries != null)
				return childCombineEntries.ToArray ();
			
			childCombineEntries = new List<CombineEntry> ();
			foreach (string en in childEntries) {
				CombineEntry re = GetEntry (en);
				if (re != null && !(re is UnknownCombineEntry))
					childCombineEntries.Add (re);
			}
			return childCombineEntries.ToArray ();
		}
		
		public CombineEntry[] GetAllEntries ()
		{
			List<CombineEntry> list = new List<CombineEntry> ();
			if (RootCombineEntry != null)
				list.Add (RootCombineEntry);
			list.AddRange (GetChildEntries ());
			return list.ToArray ();
		}
		
		CombineEntry GetEntry (string fileName)
		{
			if (IdeApp.ProjectOperations.CurrentOpenCombine != null) {
				CombineEntry fe = FindEntry (IdeApp.ProjectOperations.CurrentOpenCombine, Runtime.FileService.GetFullPath (fileName));
				return fe;
			}
			return Services.ProjectService.ReadCombineEntry (fileName, new NullProgressMonitor ());
		}
		
		CombineEntry FindEntry (CombineEntry e, string fileName)
		{
			if (Runtime.FileService.GetFullPath (e.FileName) == fileName)
				return e;
			if (e is Combine) {
				foreach (CombineEntry ce in ((Combine)e).Entries) {
					CombineEntry fe = FindEntry (ce, fileName);
					if (fe != null)
						return fe;
				}
			}
			return null;
		}
		
		public virtual DeployFileCollection GetDeployFiles (DeployContext ctx)
		{
			return DeployService.GetDeployFiles (ctx, GetAllEntries ());
		}
		
		public bool IsFileIncluded (DeployFile file)
		{
			if (excludedFiles == null)
				return true;
			return !excludedFiles.Contains (GetKey (file));
		}
		
		public void SetFileIncluded (DeployFile file, bool included)
		{
			if (excludedFiles == null)
				excludedFiles = new List<string> ();
			excludedFiles.Remove (GetKey (file));
			if (!included)
				excludedFiles.Add (GetKey (file));
		}
		
		string GetKey (DeployFile file)
		{
			return file.SourceCombineEntry.Name + "," + file.TargetDirectoryID + "," + file.RelativeTargetPath;
		}
		
		
		internal static CombineEntry GetCommonCombineEntry (ICollection entries)
		{
			ArrayList combineList = new ArrayList ();
			bool firstEntry = true;
			foreach (CombineEntry e in entries) {
				CombineEntry c = e;
				do {
					int i = combineList.IndexOf (c);
					if (i != -1) {
						// Found a common entry.
						// Remove all previous entries in the list, since they are not common.
						combineList.RemoveRange (0, i);
						break;
					} else if (firstEntry) {
						combineList.Add (c);
					}
					c = c.ParentCombine;
				}
				while (c != null);
				
				firstEntry = false;
			}
			if (combineList.Count == 0)
				return null;
			else
				return (CombineEntry) combineList [0];
		}
	}
}
