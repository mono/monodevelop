using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using GitHub.Issues.Views;
using MonoDevelop.Ide.Gui;

namespace GitHub.Issues
{
	public enum Commands
	{
		GetAllIssues
	}

	class GetAllIssuesHandler : CommandHandler
	{
		protected override void Run ()
		{
			IWorkbenchWindow window = IdeApp.Workbench.ActiveDocument.Window;
			// window.SwitchView (window.FindView<IIssuesView> ());
			window.AttachViewContent (new IssuesView ("Issues View"));
		}
	}
}

