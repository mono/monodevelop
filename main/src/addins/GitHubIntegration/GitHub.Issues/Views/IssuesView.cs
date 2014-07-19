using System;
using MonoDevelop.Ide.Gui;
using GitHub.Issues.UserInterface;
using System.Collections.Generic;

namespace GitHub.Issues.Views
{
	public interface IIssuesView : IAttachableViewContent
	{
	}

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

		public IssuesView (String name, IReadOnlyList<Octokit.Issue> issues) : base(name)
		{
			this.name = name;
			this.issues = issues;
		}

		#region implemented abstract members of AbstractBaseViewContent

		public override Gtk.Widget Control {
			get {
				if (widget == null) {
					CreateWidgetFromInfo (this.issues);
					this.widget.IssueSelected = new EventHandler<IssueSelectedEventArgs> (this.ViewIssueDetails);
				}

				return widget; 
			}
		}

		private void ViewIssueDetails(object sender, IssueSelectedEventArgs args)
		{
			ViewIssueHandler viewIssueHandler = new ViewIssueHandler (args.SelectedIssue);
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

