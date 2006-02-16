//
// AddInService.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core.AddIns.Setup;

namespace MonoDevelop.Core.AddIns
{
	public class AddInService
	{
		ArrayList addInLoadErrors = new ArrayList ();
		
		internal void Initialize ()
		{
			bool ignoreDefaultPath = false;
			string [] addInDirs = GetAddInDirectories (out ignoreDefaultPath);
			AddInTreeSingleton.Initialize ();
			AddInTreeSingleton.SetAddInDirectories (addInDirs, ignoreDefaultPath);
			Runtime.SetupService.Initialize (addInDirs, ignoreDefaultPath);
		}

		public AddinError[] AddInLoadErrors {
			get { return (AddinError[]) addInLoadErrors.ToArray (typeof(AddinError)); }
		}
		
		string[] GetAddInDirectories (out bool ignoreDefaultPath)
		{
			ArrayList addInDirs = System.Configuration.ConfigurationSettings.GetConfig("AddInDirectories") as ArrayList;
			if (addInDirs != null) {
				int i, count = addInDirs.Count;
				if (count <= 1) {
					ignoreDefaultPath = false;
					return null;
				}
				ignoreDefaultPath = (bool) addInDirs[0];
				string [] directories = new string[count-1];
				for (i = 0; i < count-1; i++) {
					directories[i] = addInDirs[i+1] as string;
				}
				return directories;
			}
			ignoreDefaultPath = false;
			return null;
		}
		
		public int StartApplication (string addinId, string[] parameters)
		{
			AddInStatus ads = Runtime.SetupService.GetAddInStatus ();
		
			string addin = null;
			foreach (ApplicationRecord arec in ads.Applications) {
				if (arec.Id == addinId) {
					addin = arec.AddIn;
					break;
				}
			}
			
			if (addin == null)
				throw new ArgumentException ("Application '" + addinId + "' not found.");
			
			PreloadAddin (null, addin);
			
			IApplication app = (IApplication) AddInTreeSingleton.AddInTree.GetTreeNode("/Workspace/Applications").BuildChildItem (addinId, null);

			try {
				return app.Run (parameters);
			} catch (Exception ex) {
				Console.WriteLine (ex);
				return -1;
			}
		}
		
		public bool IsAddinLoaded (string id)
		{
			return AddInTreeSingleton.AddInTree.AddIns [id] != null;
		}
		
		public void PreloadAddins (IProgressMonitor monitor, params string[] requestedExtensionPoints)
		{
			AddInStatus ads = Runtime.SetupService.GetAddInStatus ();
			ArrayList addins = new ArrayList ();
			foreach (string path in requestedExtensionPoints) {
				if (path == "/Workspace/Services" || path == "/Workspace/Applications")
					continue;
				string ppath = path + "/";
				foreach (ExtensionRelation rel in ads.ExtensionRelations) {
					if ((rel.Path + "/").StartsWith (ppath)) {
						foreach (string addin in rel.AddIns)
							if (!addins.Contains (addin) && !IsAddinLoaded (addin))
								addins.Add (addin);
					}
				}
			}
			if (addins.Count > 0) {
				if (monitor == null)
					monitor = new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor ();
				
				try {
					monitor.BeginTask (GettextCatalog.GetString("Loading Add-ins"), addins.Count);
					foreach (string id in addins) {
						PreloadAddin (monitor, id);
						monitor.Step (1);
					}
				} finally {
					monitor.EndTask ();
				}
			}
		}
		
		public void PreloadAddin (IProgressMonitor monitor, string id)
		{
			if (IsAddinLoaded (id))
				return;

			if (monitor == null)
				monitor = new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor ();
					
			ArrayList addins = new ArrayList ();
			Stack depCheck = new Stack ();
			ResolveLoadDependencies (addins, depCheck, id, null);
			addins.Reverse ();
			
			monitor.BeginTask (GettextCatalog.GetString("Loading Addins"), addins.Count);
			try {
				foreach (AddinSetupInfo iad in addins) {
					if (IsAddinLoaded (iad.Addin.Id)) {
						monitor.Step (1);
						continue;
					}

					monitor.BeginTask (string.Format(GettextCatalog.GetString("Loading {0} add-in"), iad.Addin.Id), 1);
					try {
						AddinError err = AddInTreeSingleton.InsertAddIn (iad.ConfigFile);
						if (err != null) {
							ReportLoadError (err);
							Runtime.LoggingService.Error ("Add-in failed to load: " + iad.Addin.Id);
							Runtime.LoggingService.Error (err.Exception);
						} else {
							Runtime.LoggingService.Info ("Loaded add-in: " + iad.Addin.Id);
							AddIn ad = AddInTreeSingleton.AddInTree.AddIns [iad.Addin.Id];
							ServiceManager.InitializeServices ("/Workspace/Services", ad);
						}
					} finally {
						monitor.EndTask ();
					}
					monitor.Step (1);
				}
			} finally {
				monitor.EndTask ();
			}
		}
		
		void ResolveLoadDependencies (ArrayList addins, Stack depCheck, string id, string version)
		{
			if (IsAddinLoaded (id))
				return;
				
			if (depCheck.Contains (id))
				throw new InvalidOperationException ("A cyclic addin dependency has been detected.");

			depCheck.Push (id);
			
			AddinSetupInfo iad = Runtime.SetupService.GetInstalledAddin (id, version);
			if (iad == null)
				throw new ArgumentException ("The addin '" + id + "' v" + version + " is not installed.");
			
			// If this addin has already been requested, bring it to the head
			// of the list, so it is loaded earlier than before.
			addins.Remove (iad);
			addins.Add (iad);
			
			foreach (PackageDependency dep in iad.Addin.Dependencies) {
				AddinDependency adep = dep as AddinDependency;
				if (adep != null)
					ResolveLoadDependencies (addins, depCheck, adep.AddinId, adep.Version);
			}
				
			depCheck.Pop ();
		}
		
		void ReportLoadError (AddinError err)
		{
			foreach (AddinError e in addInLoadErrors)
				if (e.AddinFile == err.AddinFile)
					return;
			addInLoadErrors.Add (err);
		}
		
		public object[] GetTreeItems (string path)
		{
			PreloadAddins (null, path);
			return AddInTreeSingleton.AddInTree.GetTreeNode (path).BuildChildItems(null).ToArray ();
		}
		
		public Array GetTreeItems (string path, Type itemType)
		{
			PreloadAddins (null, path);
			return AddInTreeSingleton.AddInTree.GetTreeNode (path).BuildChildItems(null).ToArray (itemType);
		}
		
		public object[] GetTreeCodons (string path)
		{
			PreloadAddins (null, path);
			IAddInTreeNode rootNode = AddInTreeSingleton.AddInTree.GetTreeNode (path);
			ArrayList list = new ArrayList ();
			foreach (DictionaryEntry de in rootNode.ChildNodes) {
				IAddInTreeNode node = (IAddInTreeNode) de.Value;
				list.Add (node.Codon);
			}
			return list.ToArray ();
		}
		
		public IAddInTreeNode GetTreeNode (string path)
		{
			PreloadAddins (null, path);
			return AddInTreeSingleton.AddInTree.GetTreeNode (path);
		}
	}
}
