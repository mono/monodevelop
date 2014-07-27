using System;

namespace GitHub.Issues
{
	public class IssueSavedEventArgs : EventArgs
	{
		public Octokit.Issue issue;

		public IssueSavedEventArgs ()
		{

		}
	}
}