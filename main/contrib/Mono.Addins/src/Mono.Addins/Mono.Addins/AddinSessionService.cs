//
// AddinService.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Xml;
using System.Collections;
using System.Reflection;

using Mono.Addins.Description;
using Mono.Addins.Database;
using Mono.Addins.Localization;

namespace Mono.Addins
{
	internal class AddinSessionService
	{
		bool checkAssemblyLoadConflicts;
		Hashtable loadedAddins = new Hashtable ();
		ExtensionContext defaultContext;
		Hashtable nodeSets = new Hashtable ();
		Hashtable autoExtensionTypes = new Hashtable ();
		Hashtable loadedAssemblies = new Hashtable ();
		AddinLocalizer defaultLocalizer;
		
		internal void Initialize ()
		{
			defaultContext = new ExtensionContext ();
			ActivateRoots ();
			AppDomain.CurrentDomain.AssemblyLoad += new AssemblyLoadEventHandler (OnAssemblyLoaded);
		}
		
		internal void Shutdown ()
		{
			AppDomain.CurrentDomain.AssemblyLoad -= new AssemblyLoadEventHandler (OnAssemblyLoaded);
			defaultContext = null;
			loadedAddins.Clear ();
			loadedAssemblies.Clear ();
			defaultContext = null;
		}
		
		public void InitializeDefaultLocalizer (IAddinLocalizer localizer)
		{
			if (localizer != null)
				defaultLocalizer = new AddinLocalizer (localizer);
			else
				defaultLocalizer = null;
		}
		
		public AddinLocalizer DefaultLocalizer {
			get {
				if (defaultLocalizer != null)
					return defaultLocalizer; 
				else
					return NullLocalizer.Instance;
			}
		}
		
		internal ExtensionContext DefaultContext {
			get { return defaultContext; }
		}
		
		public AddinLocalizer CurrentLocalizer {
			get {
				Assembly asm = Assembly.GetCallingAssembly ();
				RuntimeAddin addin = GetAddinForAssembly (asm);
				if (addin != null)
					return addin.Localizer;
				else
					return DefaultLocalizer;
			}
		}
		
		public RuntimeAddin CurrentAddin {
			get {
				Assembly asm = Assembly.GetCallingAssembly ();
				return GetAddinForAssembly (asm);
			}
		}
		
		internal RuntimeAddin GetAddinForAssembly (Assembly asm)
		{
			return (RuntimeAddin) loadedAssemblies [asm];
		}
		
		// Enables or disables conflict checking while loading assemblies.
		// Disabling makes loading faster, but less safe.
		public bool CheckAssemblyLoadConflicts {
			get { return checkAssemblyLoadConflicts; }
			set { checkAssemblyLoadConflicts = value; }
		}

		public bool IsAddinLoaded (string id)
		{
			return loadedAddins.Contains (Addin.GetIdName (id));
		}
		
		internal RuntimeAddin GetAddin (string id)
		{
			return (RuntimeAddin) loadedAddins [Addin.GetIdName (id)];
		}
		
		internal void ActivateAddin (string id)
		{
			defaultContext.ActivateAddinExtensions (id);
		}
		
		internal void UnloadAddin (string id)
		{
			defaultContext.RemoveAddinExtensions (id);
			
			RuntimeAddin addin = GetAddin (id);
			if (addin != null) {
				addin.UnloadExtensions ();
				loadedAddins.Remove (Addin.GetIdName (id));
				foreach (Assembly asm in addin.Assemblies)
					loadedAssemblies.Remove (asm);
				AddinManager.ReportAddinUnload (id);
			}
		}
		
		internal bool LoadAddin (IProgressStatus statusMonitor, string id, bool throwExceptions)
		{
			try {
				if (IsAddinLoaded (id))
					return true;

				if (!AddinManager.Registry.IsAddinEnabled (id)) {
					string msg = GettextCatalog.GetString ("Disabled add-ins can't be loaded.");
					AddinManager.ReportError (msg, id, null, false);
					if (throwExceptions)
						throw new InvalidOperationException (msg);
					return false;
				}

				ArrayList addins = new ArrayList ();
				Stack depCheck = new Stack ();
				ResolveLoadDependencies (addins, depCheck, id, false);
				addins.Reverse ();
				
				if (statusMonitor != null)
					statusMonitor.SetMessage ("Loading Addins");
				
				for (int n=0; n<addins.Count; n++) {
					
					if (statusMonitor != null)
						statusMonitor.SetProgress ((double) n / (double)addins.Count);
					
					Addin iad = (Addin) addins [n];
					if (IsAddinLoaded (iad.Id))
						continue;

					if (statusMonitor != null)
						statusMonitor.SetMessage (string.Format(GettextCatalog.GetString("Loading {0} add-in"), iad.Id));
					
					if (!InsertAddin (statusMonitor, iad))
						return false;
				}
				return true;
			}
			catch (Exception ex) {
				AddinManager.ReportError ("Add-in could not be loaded: " + ex.Message, id, ex, false);
				if (statusMonitor != null)
					statusMonitor.ReportError ("Add-in '" + id + "' could not be loaded.", ex);
				if (throwExceptions)
					throw;
				return false;
			}
		}
			
		bool InsertAddin (IProgressStatus statusMonitor, Addin iad)
		{
			try {
				RuntimeAddin p = new RuntimeAddin ();
				
				// Read the config file and load the add-in assemblies
				AddinDescription description = p.Load (iad);
				
				// Register the add-in
				loadedAddins [Addin.GetIdName (p.Id)] = p;
				
				if (!AddinDatabase.RunningSetupProcess) {
					// Load the extension points and other addin data
					
					foreach (ExtensionNodeSet rel in description.ExtensionNodeSets) {
						RegisterNodeSet (rel);
					}
					
					foreach (ConditionTypeDescription cond in description.ConditionTypes) {
						Type ctype = p.GetType (cond.TypeName, true);
						defaultContext.RegisterCondition (cond.Id, ctype);
					}
				}
					
				foreach (ExtensionPoint ep in description.ExtensionPoints)
					InsertExtensionPoint (p, ep);
				
				foreach (Assembly asm in p.Assemblies)
					loadedAssemblies [asm] = p;
				
				// Fire loaded event
				defaultContext.NotifyAddinLoaded (p);
				AddinManager.ReportAddinLoad (p.Id);
				return true;
			}
			catch (Exception ex) {
				AddinManager.ReportError ("Add-in could not be loaded", iad.Id, ex, false);
				if (statusMonitor != null)
					statusMonitor.ReportError ("Add-in '" + iad.Id + "' could not be loaded.", ex);
				return false;
			}
		}
		
		internal void InsertExtensionPoint (RuntimeAddin addin, ExtensionPoint ep)
		{
			defaultContext.CreateExtensionPoint (ep);
			foreach (ExtensionNodeType nt in ep.NodeSet.NodeTypes) {
				if (nt.ObjectTypeName.Length > 0) {
					Type ntype = addin.GetType (nt.ObjectTypeName, true);
					RegisterAutoTypeExtensionPoint (ntype, ep.Path);
				}
			}
		}
		
		bool ResolveLoadDependencies (ArrayList addins, Stack depCheck, string id, bool optional)
		{
			if (IsAddinLoaded (id))
				return true;
				
			if (depCheck.Contains (id))
				throw new InvalidOperationException ("A cyclic addin dependency has been detected.");

			depCheck.Push (id);

			Addin iad = AddinManager.Registry.GetAddin (id);
			if (iad == null || !iad.Enabled) {
				if (optional)
					return false;
				else if (iad != null && !iad.Enabled)
					throw new MissingDependencyException (GettextCatalog.GetString ("The required addin '{0}' is disabled.", id));
				else
					throw new MissingDependencyException (GettextCatalog.GetString ("The required addin '{0}' is not installed.", id));
			}

			// If this addin has already been requested, bring it to the head
			// of the list, so it is loaded earlier than before.
			addins.Remove (iad);
			addins.Add (iad);
			
			foreach (Dependency dep in iad.AddinInfo.Dependencies) {
				AddinDependency adep = dep as AddinDependency;
				if (adep != null) {
					try {
						string adepid = Addin.GetFullId (iad.AddinInfo.Namespace, adep.AddinId, adep.Version);
						ResolveLoadDependencies (addins, depCheck, adepid, false);
					} catch (MissingDependencyException) {
						if (optional)
							return false;
						else
							throw;
					}
				}
			}
			
			if (iad.AddinInfo.OptionalDependencies != null) {
				foreach (Dependency dep in iad.AddinInfo.OptionalDependencies) {
					AddinDependency adep = dep as AddinDependency;
					if (adep != null) {
						string adepid = Addin.GetFullId (iad.Namespace, adep.AddinId, adep.Version);
						if (!ResolveLoadDependencies (addins, depCheck, adepid, true))
						return false;
					}
				}
			}
				
			depCheck.Pop ();
			return true;
		}
		
		public void RegisterNodeSet (ExtensionNodeSet nset)
		{
			nodeSets [nset.Id] = nset;
		}
		
		public void UnregisterNodeSet (ExtensionNodeSet nset)
		{
			nodeSets.Remove (nset.Id);
		}
		
		public string GetNodeTypeAddin (ExtensionNodeSet nset, string type, string callingAddinId)
		{
			ExtensionNodeType nt = FindType (nset, type, callingAddinId);
			if (nt != null)
				return nt.AddinId;
			else
				return null;
		}
		
		internal ExtensionNodeType FindType (ExtensionNodeSet nset, string name, string callingAddinId)
		{
			if (nset == null)
				return null;

			foreach (ExtensionNodeType nt in nset.NodeTypes) {
				if (nt.Id == name)
					return nt;
			}
			
			foreach (string ns in nset.NodeSets) {
				ExtensionNodeSet regSet = (ExtensionNodeSet) nodeSets [ns];
				if (regSet == null) {
					AddinManager.ReportError ("Unknown node set: " + ns, callingAddinId, null, false);
					return null;
				}
				ExtensionNodeType nt = FindType (regSet, name, callingAddinId);
				if (nt != null)
					return nt;
			}
			return null;
		}
		
		public void RegisterAutoTypeExtensionPoint (Type type, string path)
		{
			autoExtensionTypes [type] = path;
		}

		public void UnregisterAutoTypeExtensionPoint (Type type, string path)
		{
			autoExtensionTypes.Remove (type);
		}
		
		public string GetAutoTypeExtensionPoint (Type type)
		{
			return autoExtensionTypes [type] as string;
		}

		void OnAssemblyLoaded (object s, AssemblyLoadEventArgs a)
		{
			CheckHostAssembly (a.LoadedAssembly);
		}
		
		internal void ActivateRoots ()
		{
			foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies ())
				CheckHostAssembly (asm);
		}
		
		void CheckHostAssembly (Assembly asm)
		{
			if (AddinDatabase.RunningSetupProcess || asm is System.Reflection.Emit.AssemblyBuilder)
				return;
			string asmFile = new Uri (asm.CodeBase).LocalPath;
			Addin ainfo = AddinManager.Registry.GetAddinForHostAssembly (asmFile);
			if (ainfo != null && !IsAddinLoaded (ainfo.Id)) {
				if (ainfo.Description.FilesChanged ()) {
					// If the add-in has changed, update the add-in database.
					// We do it here because once loaded, add-in roots can't be
					// reloaded like regular add-ins.
					AddinManager.Registry.Update (null);
					ainfo = AddinManager.Registry.GetAddinForHostAssembly (asmFile);
					if (ainfo == null)
						return;
				}
				LoadAddin (null, ainfo.Id, false);
			}
		}
	}
		
}
