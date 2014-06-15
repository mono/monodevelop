using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using GitHub.Issues.Views;
using MonoDevelop.Ide.Gui;
using System.Collections.Generic;

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

			IssuesManager manager = new IssuesManager ();
			IReadOnlyList<Octokit.Issue> issues = manager.GetAllIssues ();

			window.AttachViewContent (new IssuesView ("Issues View", issues));
		}
	}
}

