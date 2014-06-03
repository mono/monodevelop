using System;
using MonoDevelop.Components.Commands;

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
			IssuesManager manager = new IssuesManager ();
			manager.GetAllIssues ();
		}
	}
}

