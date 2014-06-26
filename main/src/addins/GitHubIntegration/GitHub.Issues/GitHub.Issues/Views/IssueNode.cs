using System;
using Octokit;
using Gtk;
using System.ComponentModel;

namespace GitHub.Issues
{
	public class IssueNode
	{
		private Octokit.Issue issue;

		[Description("Title")]
		public String Title { get { return this.issue.Title; } }
		[Description("Description")]
		public String Body { get { return this.issue.Body; } }
		[Description("Assigned To")]
		public String Assigee { get { return this.issue.Assignee != null ? this.issue.Assignee.Login : "Unassigned"; } }
		[Description("Last Updated At")]
		public String UpdatedAt { get { return this.issue.UpdatedAt.ToString (); } }
		[Description("State")]
		public String State { get { return this.issue.State.ToString (); } }

		public IssueNode (Octokit.Issue issue)
		{
			this.issue = issue;
		}
	}
}

