using System;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.WebBrowser;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.GettingStarted
{
	public class GettingStartedViewContent : ViewContent
	{
		Control gettingStartedWidget;
		
		public GettingStartedViewContent (Project project, GettingStartedProvider provider)
		{
			Owner = project;
			gettingStartedWidget = provider.GetGettingStartedWidget (project);
			ContentName = GettextCatalog.GetString ("Getting Started");
		}

		public override Control Control {
			get {
				return gettingStartedWidget;
			}
		}

		public override bool IsViewOnly {
			get {
				return true;
			}
		}

		public override bool IsFile {
			get {
				return false;
			}
		}

		public override object GetDocumentObject ()
		{
			return Owner.GetGettingStartedNode ();
		}
	}
}

