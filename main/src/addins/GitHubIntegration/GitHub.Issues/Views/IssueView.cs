using System;
using MonoDevelop.Ide.Gui;
using GitHub.Issues.Views;

namespace GitHub.Issues
{
	public interface IIssueView : IAttachableViewContent
	{
	}

	public class IssueView : BaseView, IIssueView
	{
		private IssueWidget widget;
		private String name;
		private Octokit.Issue issue;

		public IssueWidget IssueWidget {
			get {
				return widget;
			}
		}

		public IssueView (String name, Octokit.Issue issue) : base(name)
		{
			this.name = name;
			this.issue = issue;
		}

		#region implemented abstract members of AbstractBaseViewContent

		public override Gtk.Widget Control {
			get {
				if (widget == null) {
					CreateWidgetFromInfo (this.issue);
				}

				return widget; 
			}
		}

		#endregion

		private void CreateWidgetFromInfo (Octokit.Issue issue)
		{
			this.widget = new IssueWidget (issue);
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

