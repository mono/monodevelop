using System;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.WebBrowser;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Documents;
using System.Threading.Tasks;

namespace MonoDevelop.Ide.GettingStarted
{
	public class GettingStartedViewContent : DocumentController
	{
		Control gettingStartedWidget;


		public GettingStartedViewContent (Project project, GettingStartedProvider provider)
		{
			Owner = project;
			gettingStartedWidget = provider.GetGettingStartedWidget (project);
			DocumentTitle = GettextCatalog.GetString ("Getting Started");
			IsReadOnly = true;
		}

		public override object GetDocumentObject ()
		{
			return ((Project)Owner).GetGettingStartedNode ();
		}

		protected override Control OnGetViewControl (DocumentViewContent view)
		{
			return gettingStartedWidget;
		}
	}
}

