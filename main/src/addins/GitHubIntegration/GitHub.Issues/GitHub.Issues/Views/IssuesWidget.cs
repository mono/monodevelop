using System;
using System.Collections.Generic;

namespace GitHub.Issues.UserInterface
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class IssuesWidget : Gtk.Bin
	{
		Gtk.NodeStore nodeStore;

		public Gtk.NodeStore NodeStore
		{
			get {
				if (nodeStore == null) {
					nodeStore = new Gtk.NodeStore (typeof(IssueNode));
				}

				return nodeStore;
			}
		}

		public IssuesWidget (IReadOnlyList<Octokit.Issue> issues)
		{
			this.Build ();

			foreach (Octokit.Issue issue in issues) {
				this.NodeStore.AddNode (new IssueNode (issue));
			}

			Gtk.NodeView nodeView = new Gtk.NodeView (this.NodeStore);

			this.Add (nodeView);

			nodeView.AppendColumn ("Title", new Gtk.CellRendererText (), "text", 0);
			nodeView.AppendColumn ("Body", new Gtk.CellRendererText (), "text", 1);
			nodeView.AppendColumn ("Assigned To", new Gtk.CellRendererText (), "text", 2);
			nodeView.AppendColumn ("Last Updated", new Gtk.CellRendererText (), "text", 3);
			nodeView.AppendColumn ("State", new Gtk.CellRendererText (), "text", 4);

			this.ShowAll ();
		}
	}
}

