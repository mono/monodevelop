using System;
using Octokit;
using Gtk;

namespace GitHub.Issues
{
	[Gtk.TreeNode(ListOnly=true)]
	public class IssueNode : Gtk.TreeNode
	{
		private Octokit.Issue issue;

		public IssueNode (Octokit.Issue issue)
		{
			this.issue = issue;
		}

		[Gtk.TreeNodeValue (Column=0)]
		public string IssueName { get { return this.issue.Title; } }

		[Gtk.TreeNodeValue (Column=1)]
		public string IssueDiscription { get { return this.issue.Body; } }

		[Gtk.TreeNodeValue (Column=2)]
		public string AssignedTo { get { return this.issue.Assignee == null ? "Unassigned" : this.issue.Assignee.Login; } }

		[Gtk.TreeNodeValue (Column=3)]
		public string LastUpdated { get { return this.issue.UpdatedAt.ToString(); } }

		[Gtk.TreeNodeValue (Column=4)]
		public string State { get { return this.issue.State.ToString (); } }
	}
}

