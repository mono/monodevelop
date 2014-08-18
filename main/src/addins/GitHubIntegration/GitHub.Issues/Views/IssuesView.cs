using System;
using MonoDevelop.Ide.Gui;
using GitHub.Issues;
using System.Collections.Generic;

namespace GitHub.Issues.Views
{
	/// <summary>
	/// Interface for the issues view
	/// </summary>
	public interface IIssuesView : IAttachableViewContent
	{
	}

	/// <summary>
	/// Issues View which displays all issues from the current repository, allows selection of columns/properties to display and filtering
	/// Also provides funtionality to move to other more specific management windows
	/// </summary>
	public partial class IssuesView : BaseView, IIssuesView
	{
		private IssuesWidget widget;
		private String name;
		private IReadOnlyList<Octokit.Issue> issues;

		public IssuesWidget IssuesWidget {
			get {
				return widget;
			}
		}

		public IssuesView (String name, IReadOnlyList<Octokit.Issue> issues) : base (name)
		{
			this.name = name;
			this.issues = issues;
		}

		#region implemented abstract members of AbstractBaseViewContent

		public override Gtk.Widget Control {
			get {
				if (widget == null) {
					CreateWidgetFromInfo (this.issues);
					this.widget.IssueSelected += new EventHandler<IssueSelectedEventArgs> (this.ViewIssueDetails);
					this.widget.CreateNewIssueClicked += new EventHandler (this.CreateNewIssueClicked);
					this.widget.ManageLabelsButtonClicked += new EventHandler (this.ManageLabelsButtonClicked);
				}

				return widget; 
			}
		}

		#endregion

		#region Event Handlers

		/// <summary>
		/// Called when an issue is selected from the list and the user wants to view it
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="args">Arguments.</param>
		private void ViewIssueDetails (object sender, IssueSelectedEventArgs args)
		{
			ViewIssueHandler viewIssueHandler = new ViewIssueHandler (args.SelectedIssue);
		}

		/// <summary>
		/// Called when the "Create New Issue" button is cliked
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		private void CreateNewIssueClicked (object sender, EventArgs e)
		{
			// [null] makes it go into creation mode
			ViewIssueHandler viewIssueHandler = new ViewIssueHandler (null);
		}

		/// <summary>
		/// Called when the "Manage Labels" button is cliked
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		private void ManageLabelsButtonClicked (object sender, EventArgs e)
		{
			LabelsHandler labelsHandler = new LabelsHandler ();
		}

		#endregion

		private void CreateWidgetFromInfo (IReadOnlyList<Octokit.Issue> issues)
		{
			this.widget = new IssuesWidget (issues);
		}

		void IAttachableViewContent.Selected ()
		{
		}

		void IAttachableViewContent.Deselected ()
		{
		}

		void IAttachableViewContent.BeforeSave ()
		{
		}

		void IAttachableViewContent.BaseContentChanged ()
		{
		}
	}
}

