using System;
using System.Xml;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.WebBrowser;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.GettingStarted
{
	public class GettingStartedProjectExtension : ProjectExtension
	{
		static bool showGettingStartedOnce = false;

		protected override void OnInitializeFromTemplate (ProjectCreateInformation projectCreateInfo, XmlElement template)
		{
			base.OnInitializeFromTemplate (projectCreateInfo, template);
			if (Project.GetService<IGettingStartedProvider> () != null) {
				if (template.GetAttribute ("HideGettingStarted")?.ToLower () == "true")
					Project.UserProperties.SetValue ("HideGettingStarted", true);
				else
					showGettingStartedOnce = true;
			}
		}

		protected override void OnEndLoad ()
		{
			base.OnEndLoad ();

			var provider = Project.GetGettingStartedProvider ();
			if (provider == null)
				return;

			if (showGettingStartedOnce) {
				showGettingStartedOnce = false;

				Runtime.RunInMainThread (() => GettingStarted.ShowGettingStarted (Project));
			}
		}
	}
}

