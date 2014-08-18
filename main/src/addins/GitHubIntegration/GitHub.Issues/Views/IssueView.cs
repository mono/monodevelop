using System;
using MonoDevelop.Ide.Gui;
using GitHub.Issues.Views;

namespace GitHub.Issues
{
	/// <summary>
	/// Interface for issue view
	/// </summary>
	public interface IIssueView : IAttachableViewContent
	{
	}

	/// <summary>
	/// Issue view which handles creation and management of a single issue from the current repository
	/// </summary>
	public class IssueView : BaseView, IIssueView
	{
		private IssueWidget widget;
		private String name;
		private Octokit.Issue issue;

		/// <summary>
		/// Gets a value indicating whether this instance issue widget.
		/// </summary>
		/// <value><c>true</c> if this instance issue widget; otherwise, <c>false</c>.</value>
		public IssueWidget IssueWidget {
			get {
				return widget;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GitHub.Issues.IssueView"/> class.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="issue">Issue.</param>
		public IssueView (String name, Octokit.Issue issue) : base (name)
		{
			this.name = name;
			this.issue = issue;
		}

		#region implemented abstract members of AbstractBaseViewContent

		/// <summary>
		/// Gets the control.
		/// </summary>
		/// <value>The control.</value>
		public override Gtk.Widget Control {
			get {
				if (widget == null) {
					CreateWidgetFromInfo (this.issue);
				}

				return widget; 
			}
		}

		#endregion

		/// <summary>
		/// Creates the widget from info.
		/// </summary>
		/// <param name="issue">Issue.</param>
		private void CreateWidgetFromInfo (Octokit.Issue issue)
		{
			this.widget = new IssueWidget (issue);
			this.widget.IssueSaved += this.IssueSaved;
		}

		/// <summary>
		/// Selected this instance.
		/// </summary>
		void IAttachableViewContent.Selected ()
		{
		}

		/// <summary>
		/// Deselected this instance.
		/// </summary>
		void IAttachableViewContent.Deselected ()
		{
		}

		/// <summary>
		/// Befores the save.
		/// </summary>
		void IAttachableViewContent.BeforeSave ()
		{
		}

		/// <summary>
		/// Bases the content changed.
		/// </summary>
		void IAttachableViewContent.BaseContentChanged ()
		{
		}

		/// <summary>
		/// Called whenever the issue is saved from within the widget and updates the name etc.
		/// </summary>
		/// <returns><c>true</c> if this instance issue saved the specified sender e; otherwise, <c>false</c>.</returns>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		private void IssueSaved (object sender, IssueSavedEventArgs e)
		{
			this.name = e.issue.Title;
			this.issue = e.issue;
		}
	}
}

