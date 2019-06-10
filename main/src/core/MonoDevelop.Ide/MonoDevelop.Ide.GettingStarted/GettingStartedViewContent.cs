using System;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.WebBrowser;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Documents;
using System.Threading.Tasks;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;

namespace MonoDevelop.Ide.GettingStarted
{
	public class GettingStartedViewContent : DocumentController, IProjectPadNodeSelector
	{
		Control gettingStartedWidget;


		public GettingStartedViewContent (Project project, GettingStartedProvider provider)
		{
			Owner = project;
			gettingStartedWidget = provider.GetGettingStartedWidget (project);
			DocumentTitle = GettextCatalog.GetString ("Getting Started");
			IsReadOnly = true;
		}

		protected override Control OnGetViewControl (DocumentViewContent view)
		{
			return gettingStartedWidget;
		}

		object IProjectPadNodeSelector.GetNodeObjext ()
		{
			return ((Project)Owner).GetGettingStartedNode ();
		}
	}
}

