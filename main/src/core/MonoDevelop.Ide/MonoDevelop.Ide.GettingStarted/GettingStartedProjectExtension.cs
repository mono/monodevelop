using System;
using System.Xml;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Ide.WebBrowser;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.GettingStarted
{
	public class GettingStartedProjectExtension : ProjectExtension
	{
		static bool showGettingStartedOnce = false;
		GettingStartedNode treeDataObject;

		public GettingStartedNode ProjectPadNode {
			get {
				if (treeDataObject == null) {
					treeDataObject = new GettingStartedNode (Project);
				}
				return treeDataObject;
			}
		}

		protected override void OnInitializeFromTemplate (ProjectCreateInformation projectCreateInfo, XmlElement template)
		{
			base.OnInitializeFromTemplate (projectCreateInfo, template);
			if (template.GetAttribute ("HideGettingStarted")?.ToLower () == "true")
				Project.UserProperties.SetValue ("HideGettingStarted", true);
			else
				showGettingStartedOnce = true;
		}

		protected override void OnEndLoad ()
		{
			base.OnEndLoad ();

			var provider = ProjectPadNode.Provider;
			if (provider == null)
				return;

			if (showGettingStartedOnce) {
				showGettingStartedOnce = false;

				Runtime.RunInMainThread (() => GettingStarted.ShowGettingStarted (Project));
			}
		}
	}
}

