using System;
using GitHub.Issues.Views;
using System.Collections.Generic;

namespace GitHub.Issues
{
	/// <summary>
	/// Interface for the issue view handler
	/// </summary>
	public interface IIssueViewHandler : IGitHubIssueViewHandler<IIssueView>
	{
	}

	/// <summary>
	/// Handles the creation of the Issue View which displays and manages a single issue
	/// </summary>
	public class IssueViewHandler
	{
		/// <summary>
		/// Can it run?
		/// </summary>
		/// <returns><c>true</c>, if handle was caned, <c>false</c> otherwise.</returns>
		public bool canHandle ()
		{
			return true;
		}

		/// <summary>
		/// Creates the view.
		/// </summary>
		/// <returns>The view.</returns>
		/// <param name="name">Name.</param>
		/// <param name="issue">Issue.</param>
		public IIssueView CreateView (String name, Octokit.Issue issue)
		{
			return new IssueView (name, issue);
		}
	}
}

