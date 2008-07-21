// AddinAuthoringService.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Collections.Generic;
using Mono.Addins;
using Mono.Addins.Description;
using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;
using System.Xml;

namespace MonoDevelop.AddinAuthoring
{
	public static class AddinAuthoringService
	{
		static AddinAuthoringServiceConfig config;
		static string configFile;
		
		static AddinAuthoringService ()
		{
			if (IdeApp.IsInitialized) {
				IdeApp.ProjectOperations.EndBuild += OnEndBuild;
			}
			
			configFile = Path.Combine (PropertyService.ConfigPath, "AddinAuthoring.config");
			if (File.Exists (configFile)) {
				try {
					XmlDataSerializer ser = new XmlDataSerializer (new DataContext ());
					StreamReader sr = new StreamReader (configFile);
					using (sr) {
						config = (AddinAuthoringServiceConfig) ser.Deserialize (new XmlTextReader (sr), typeof(AddinAuthoringServiceConfig));
					}
				}
				catch (Exception ex) {
					LoggingService.LogError ("Could not load add-in authoring service configuration", ex);
				}
			}
			if (config == null)
				config = new AddinAuthoringServiceConfig ();
		}
		
		static void SaveConfig ()
		{
			try {
				XmlDataSerializer ser = new XmlDataSerializer (new DataContext ());
				StreamWriter sw = new StreamWriter (configFile);
				using (sw) {
					ser.Serialize (new XmlTextWriter (sw), config, typeof(AddinAuthoringServiceConfig));
				}
			}
			catch (Exception ex) {
				LoggingService.LogError ("Could not save add-in authoring service configuration", ex);
			}
		}
		
		internal static void Init ()
		{
			// Do nothing. Will be initialized in the static constructor.
		}
		
		static void OnEndBuild (object s, BuildEventArgs args)
		{
			if (args.Success && IdeApp.Workspace.IsOpen) {
				Dictionary<string, AddinRegistry> regs = new Dictionary<string, AddinRegistry> ();
				foreach (DotNetProject p in IdeApp.Workspace.GetAllSolutionItems<DotNetProject> ()) {
					AddinData data = AddinData.GetAddinData (p);
					if (data != null) {
						if (!regs.ContainsKey (data.AddinRegistry.RegistryPath))
							regs [data.AddinRegistry.RegistryPath] = data.AddinRegistry;
					}
				}
				if (regs.Count > 0) {
					args.ProgressMonitor.BeginTask (AddinManager.CurrentLocalizer.GetString ("Updating add-in registry"), regs.Count);
					foreach (AddinRegistry reg in regs.Values) {
						reg.Update (new ProgressStatusMonitor (args.ProgressMonitor, 2));
						args.ProgressMonitor.Step (1);
					}
					args.ProgressMonitor.EndTask ();
				}
			}
		}
		
		public static string GetRegistryName (string regPath)
		{
			foreach (RegistryExtensionNode node in GetRegistries ()) {
				if (Path.GetFullPath (node.RegistryPath) == Path.GetFullPath (regPath))
					return node.Name;
			}
			return regPath;
		}
		
		public static IEnumerable<RegistryExtensionNode> GetRegistries ()
		{
			foreach (RegistryExtensionNode node in AddinManager.GetExtensionNodes ("MonoDevelop/AddinAuthoring/AddinRegistries"))
				yield return node;
			foreach (RegistryExtensionNode node in config.Registries)
				yield return node;
		}
		
		public static void AddCustomRegistry (RegistryExtensionNode reg)
		{
			config.Registries.Add (reg);
			SaveConfig ();
		}
		
		public static void RemoveCustomRegistry (RegistryExtensionNode reg)
		{
			config.Registries.Remove (reg);
			SaveConfig ();
		}
		
		internal static string NormalizeUserPath (string path)
		{
			if (path.StartsWith ("~")) {
				string absRegistryPath = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
				return Path.Combine (absRegistryPath, path.Substring (2));
			}
			else
				return path;
		}
		
		internal static void AddReferences (AddinData data, object[] addins)
		{
			AddinDescription desc = data.LoadAddinManifest ();
			AddinDescriptionView view = FindLoadedDescription (data);
			foreach (Addin ad in addins) {
				AddReference (desc, ad);
				if (view != null)
					AddReference (view.AddinDescription, ad);
			}
			if (view != null) {
				view.Update ();
				view.BeginInternalUpdate ();
			}
			
			try {
				desc.Save ();
				data.NotifyChanged ();
			} finally {
				if (view != null)
					view.EndInternalUpdate ();
			}
		}
		
		internal static void RemoveReferences (AddinData data, string[] fullIds)
		{
			AddinDescription desc = data.LoadAddinManifest ();
			AddinDescriptionView view = FindLoadedDescription (data);
			foreach (string ad in fullIds) {
				RemoveReference (desc, ad);
				if (view != null)
					RemoveReference (view.AddinDescription, ad);
			}
			if (view != null) {
				view.Update ();
				view.BeginInternalUpdate ();
			}
			
			try {
				desc.Save ();
				data.NotifyChanged ();
			} finally {
				if (view != null)
					view.EndInternalUpdate ();
			}
		}
		
		static AddinDescriptionView FindLoadedDescription (AddinData data)
		{
			foreach (Document doc in IdeApp.Workbench.Documents) {
				AddinDescriptionView view = doc.GetContent <AddinDescriptionView> ();
				if (view != null && view.Data == data)
					return view;
			}
			return null;
		}
				
		static void AddReference (AddinDescription desc, Addin addin)
		{
			foreach (AddinDependency adep in desc.MainModule.Dependencies) {
				if (adep.FullAddinId == addin.Id)
					return;
			}
			if (addin.Namespace == desc.Namespace)
				desc.MainModule.Dependencies.Add (new AddinDependency (addin.LocalId, addin.Version));
			else
				desc.MainModule.Dependencies.Add (new AddinDependency (addin.Id));
		}
				
		static void RemoveReference (AddinDescription desc, string addinId)
		{
			foreach (AddinDependency adep in desc.MainModule.Dependencies) {
				if (adep.FullAddinId == addinId) {
					desc.MainModule.Dependencies.Remove (adep);
					break;
				}
			}
		}
	}
	
	class AddinAuthoringServiceConfig
	{
		List<RegistryExtensionNode> registries = new List<RegistryExtensionNode> ();
		
		[ItemProperty]
		public List<RegistryExtensionNode> Registries {
			get { return registries; }
		}
	}
}
