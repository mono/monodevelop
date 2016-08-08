using System;
using System.Collections.Generic;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.GettingStarted
{
	public static class GettingStarted
	{
		static readonly string GettingStartedProvidersExtensionPoint = "/MonoDevelop/Ide/GettingStartedProviders";

		static List<IGettingStartedProvider> providers = new List<IGettingStartedProvider> ();

		static GettingStarted ()
		{
			AddinManager.AddExtensionNodeHandler (GettingStartedProvidersExtensionPoint, new ExtensionNodeEventHandler (OnExtensionChanged));
		}

		static void OnExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add)
				providers.Add ((IGettingStartedProvider)args.ExtensionObject);
			else if (args.Change == ExtensionChange.Remove)
				providers.Remove ((IGettingStartedProvider)args.ExtensionObject);
		}

		public static IGettingStartedProvider GetGettingStartedProvider (this Project project)
		{
			foreach (var provider in providers) {
				if (provider.SupportsProject (project))
					return provider;
			}

			return null;
		}

		public static void ShowGettingStarted (Project project)
		{
			GettingStartedViewContent view;
			foreach (var doc in IdeApp.Workbench.Documents) {
				view = doc.PrimaryView.GetContent<GettingStartedViewContent> ();
				if (view != null && view.Project == project) {
					view.WorkbenchWindow.SelectWindow ();
					return;
				}
			}

			var provider = project.GetGettingStartedProvider ();
			if (provider != null) {
				var vc = new GettingStartedViewContent (project, provider);
				IdeApp.Workbench.OpenDocument (vc, true);
			}
		}
	}
}

