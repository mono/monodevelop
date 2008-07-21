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
using MonoDevelop.Core.Serialization;
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
		List<SolutionItemReference> childEntries = new List<SolutionItemReference> ();
		
		[ItemProperty ("ExcludedFiles")]
		[ItemProperty ("File", Scope="*")]
		List<string> excludedFiles;
		
		[ItemProperty ("RootEntry")]
		SolutionItemReference rootEntry;
		
		List<SolutionItem> childCombineEntries;
		SolutionItem rootSolutionItem;

#region MD 1.0 compatibility fields
		string[] md1ChildEntries;
		string md1RootEntry;
#endregion
		
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
		
		internal bool Build (IProgressMonitor monitor)
		{
			monitor.BeginTask ("Package: " + Description, 1);
			DeployContext ctx = null;
			try {
				ctx = CreateDeployContext ();
				if (ctx != null)
					ctx.FileFilter = this;
				if (!OnBuild (monitor, ctx)) {
					monitor.AsyncOperation.Cancel ();
					return false;
				}
			} catch (Exception ex) {
				monitor.ReportError ("Package creation failed", ex);
				LoggingService.LogError ("Package creation failed", ex);
				monitor.AsyncOperation.Cancel ();
				return false;
			} finally {
				monitor.EndTask ();
				if (ctx != null)
					ctx.Dispose ();
			}
			return true;
		}
		
		public virtual bool CanBuild (SolutionItem entry)
		{
			return true;
		}
		
		public virtual void InitializeSettings (SolutionItem entry)
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
			childEntries = new List<SolutionItemReference> (other.childEntries);
			rootEntry = other.rootEntry;
			md1ChildEntries = other.md1ChildEntries;
			md1RootEntry = other.md1RootEntry;
			
			if (other.childCombineEntries != null)
				childCombineEntries = new List<SolutionItem> (other.childCombineEntries);
			else
				childCombineEntries = null;
			if (other.excludedFiles != null)
				excludedFiles = new List<string> (other.excludedFiles);
			else
				excludedFiles = null;
			rootSolutionItem = other.rootSolutionItem;
		}
		
		public virtual PackageBuilder[] CreateDefaultBuilders ()
		{
			return new PackageBuilder [0];
		}
		
		protected virtual bool OnBuild (IProgressMonitor monitor, DeployContext ctx)
		{
			return true;
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
		
		public void SetSolutionItem (SolutionItem entry)
		{
			SetSolutionItem (entry, null);
		}
		
		public void SetSolutionItem (SolutionItem rootSolutionItem, IEnumerable<SolutionItem> childEntries)
		{
			this.rootSolutionItem = rootSolutionItem;
			childCombineEntries = new List<SolutionItem> ();
			
			if (childEntries != null)
			    childCombineEntries.AddRange (childEntries);
			
			UpdateEntryNames ();
			InitializeSettings (rootSolutionItem);
		}
		
		internal void SetSolutionItem (SolutionItemReference siRoot, SolutionItemReference[] children)
		{
			rootEntry = siRoot;
			childEntries.Clear ();
			foreach (SolutionItemReference e in children)
				childEntries.Add (e);
		}
		
		internal void SetSolutionItemMd1 (string siRoot, string[] children)
		{
			md1RootEntry = siRoot;
			md1ChildEntries = children;
		}
		
		void UpdateEntryNames ()
		{
			this.rootEntry = new SolutionItemReference (rootSolutionItem);
			this.childEntries.Clear ();
			foreach (SolutionItem e in childCombineEntries)
				childEntries.Add (new SolutionItemReference (e));
		}
		
		public SolutionItem RootSolutionItem {
			get {
				if (rootSolutionItem == null && (rootEntry != null || md1RootEntry != null)) {
					if (md1RootEntry != null) {
						rootSolutionItem = GetEntryMD1 (md1RootEntry);
						md1RootEntry = null;
					} else
						rootSolutionItem = GetEntry (rootEntry);
				}
				return rootSolutionItem; 
			}
		}
		
		public Solution Solution {
			get {
				return RootSolutionItem != null ? RootSolutionItem.ParentSolution : null;
			}
		}
		
		public void AddEntry (SolutionItem entry)
		{
			SolutionItemReference fp = new SolutionItemReference (entry);
			foreach (SolutionItemReference s in childEntries)
				if (s.Equals (fp))
					return;
			
			if (rootEntry == fp)
				return;
			
			List<SolutionItem> list = new List<SolutionItem> ();
			if (RootSolutionItem != null)
				list.Add (RootSolutionItem);
			list.AddRange (GetChildEntries());
			list.Add (entry);
			
			rootSolutionItem = GetCommonSolutionItem (list);
			list.Remove (rootSolutionItem);
			
			foreach (SolutionItem e in list.ToArray ()) {
				SolutionItem ce = e.ParentFolder;
				while (ce != rootSolutionItem) {
					if (!list.Contains (ce))
						list.Add (ce);
					ce = ce.ParentFolder;
				}
			}
			childCombineEntries = list;
			UpdateEntryNames ();
		}
		
		public SolutionItem[] GetChildEntries ()
		{
			if (childCombineEntries != null)
				return childCombineEntries.ToArray ();
			
			childCombineEntries = new List<SolutionItem> ();
			
			if (md1ChildEntries != null) {
				foreach (string it in md1ChildEntries) {
					SolutionItem re = GetEntryMD1 (it);
					if (re != null && !(re is UnknownSolutionItem))
						childCombineEntries.Add (re);
				}
				md1ChildEntries = null;
				return childCombineEntries.ToArray ();
			}
			
			foreach (SolutionItemReference en in childEntries) {
				SolutionItem re = GetEntry (en);
				if (re != null && !(re is UnknownSolutionItem))
					childCombineEntries.Add (re);
			}
			return childCombineEntries.ToArray ();
		}
		
		public SolutionItem[] GetAllEntries ()
		{
			List<SolutionItem> list = new List<SolutionItem> ();
			if (RootSolutionItem != null)
				list.Add (RootSolutionItem);
			list.AddRange (GetChildEntries ());
			return list.ToArray ();
		}
		
		SolutionItem GetEntry (SolutionItemReference reference)
		{
			return Services.ProjectService.ReadSolutionItem (new NullProgressMonitor (), reference, IdeApp.Workspace.Items.ToArray ());
		}
		
		public virtual DeployFileCollection GetDeployFiles (DeployContext ctx, string configuration)
		{
			return DeployService.GetDeployFiles (ctx, GetAllEntries (), configuration);
		}
		
		public virtual string[] GetSupportedConfigurations ()
		{
			if (Solution != null) {
				ICollection<string> col = Solution.GetConfigurations ();
				string[] arr = new string [col.Count];
				col.CopyTo (arr, 0);
				return arr;
			}
			else
				return new string [0];
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
			return file.SourceSolutionItem.Name + "," + file.TargetDirectoryID + "," + file.RelativeTargetPath;
		}
		
		
		internal static SolutionItem GetCommonSolutionItem (IEnumerable<SolutionItem> entries)
		{
			SolutionItem common = null;
			foreach (SolutionItem it in entries) {
				if (common == null)
					common = it;
				else
					return it.ParentSolution.RootFolder;
			}
			return common;
		}
		
		SolutionItem GetEntryMD1 (string fileName)
		{
			foreach (WorkspaceItem it in IdeApp.Workspace.Items) {
				SolutionItem fi = FindEntryMD1 (it, FileService.GetFullPath (fileName));
				if (fi != null)
					return fi;
			}
			return Services.ProjectService.ReadSolutionItem (new NullProgressMonitor (), fileName);
		}
		
		SolutionItem FindEntryMD1 (object item, string fileName)
		{
			if (item is SolutionItem) {
				string file = MonoDevelop.Projects.Formats.MD1.MD1ProjectService.GetItemFileName ((SolutionItem)item);
				if (file != null && FileService.GetFullPath (file) == fileName)
					return (SolutionItem) item;
			}
			
			if (item is Solution) {
				return FindEntryMD1 (((Solution)item).RootFolder, fileName);
			}
			else if (item is SolutionFolder) {
				foreach (SolutionItem ce in ((SolutionFolder)item).Items) {
					SolutionItem fi = FindEntryMD1 (ce, fileName);
					if (fi != null) return fi;
				}
			}
			else if (item is Workspace) {
				foreach (WorkspaceItem wi in ((Workspace)item).Items) {
					SolutionItem fi = FindEntryMD1 (wi, fileName);
					if (fi != null) return fi;
				}
			}
			return null;
		}
	}
}
