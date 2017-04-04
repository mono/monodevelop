using System;
using System.Collections.Generic;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.GettingStarted
{
	public static class GettingStarted
	{
		static readonly string GettingStartedProvidersExtensionPoint = "/MonoDevelop/Ide/GettingStartedProviders";

		static List<GettingStartedProvider> providers = new List<GettingStartedProvider> ();

		static GettingStarted ()
		{
			AddinManager.AddExtensionNodeHandler (GettingStartedProvidersExtensionPoint, new ExtensionNodeEventHandler (OnExtensionChanged));
		}

		static void OnExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add)
				providers.Add ((GettingStartedProvider)args.ExtensionObject);
			else if (args.Change == ExtensionChange.Remove)
				providers.Remove ((GettingStartedProvider)args.ExtensionObject);
		}

		public static GettingStartedProvider GetGettingStartedProvider (this Project project)
		{
			if (project != null)
				foreach (var provider in providers) {
					if (provider.SupportsProject (project))
						return provider;
				}

			return null;
		}

		public static GettingStartedNode GetGettingStartedNode (this Project project)
		{
			return project.GetService<GettingStartedProjectExtension> ()?.ProjectPadNode;
		}

		/// <summary>
		/// Shows the getting started page for the given project.
		/// </summary>
		/// <param name="project">The project for which the getting started page should be shown</param>
		/// <param name="pageHint">A hint to the getting started page for cases when the provide may need assistance in determining the correct content to show</param>
		public static void ShowGettingStarted (Project project, string pageHint = null)
		{
			var provider = project.GetGettingStartedProvider ();
			if (provider != null) {
				provider.ShowGettingStarted (project, pageHint);
			}
		}
	}
}

