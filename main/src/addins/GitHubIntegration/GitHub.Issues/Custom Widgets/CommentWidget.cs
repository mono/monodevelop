using System;
using Pango;

namespace GitHub.Issues
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class CommentWidget : Gtk.Bin
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="GitHub.Issues.CommentWidget"/> class.
		/// </summary>
		/// <param name="comment">Comment.</param>
		public CommentWidget (Octokit.IssueComment comment)
		{
			this.Build ();

			Gtk.VBox mainContainer = new Gtk.VBox ();

			Gtk.HBox topPanel = new Gtk.HBox ();

			topPanel.Add (this.padWidget(this.leftAlign(this.createOwnerTextBox (comment)), 10, 0, 0, 10));
			topPanel.Add (this.padWidget(this.rightAlign(this.createCommentDateTextBox(comment)), 10, 0, 0, 10));

			mainContainer.Add(topPanel);

			Gtk.Label commentBox = this.createCommentBox (comment);

			// Bind the width to the parent size
			mainContainer.SizeAllocated += (object o, Gtk.SizeAllocatedArgs args) => 
			{
				commentBox.WidthRequest = args.Allocation.Width;
			};

			mainContainer.Add (this.padWidget(commentBox, 10, 10, 0, 10));

			this.Add (mainContainer);
		}

		#region Create UI Components

		/// <summary>
		/// Creates the owner text box.
		/// </summary>
		/// <returns>The owner text box.</returns>
		/// <param name="comment">Comment.</param>
		private Gtk.Label createOwnerTextBox (Octokit.IssueComment comment)
		{
			Gtk.Label owner = new Gtk.Label (comment.User.Login);

			return owner;
		}

		/// <summary>
		/// Creates the comment date text box.
		/// </summary>
		/// <returns>The comment date text box.</returns>
		/// <param name="comment">Comment.</param>
		private Gtk.Label createCommentDateTextBox(Octokit.IssueComment comment)
		{
			Gtk.Label date = new Gtk.Label (comment.CreatedAt.DateTime.ToString());

			return date;
		}

		/// <summary>
		/// Creates the comment box.
		/// </summary>
		/// <returns>The comment box.</returns>
		/// <param name="comment">Comment.</param>
		private Gtk.Label createCommentBox(Octokit.IssueComment comment)
		{
			Gtk.Label commentBox = new Gtk.Label (comment.Body);
			commentBox.LineWrapMode = Pango.WrapMode.WordChar;
			commentBox.Wrap = true;

			return commentBox;
		}

		#endregion

		#region Alignment

		/// <summary>
		/// Left aligns the widget
		/// </summary>
		/// <returns>The align.</returns>
		/// <param name="widget">Widget.</param>
		private Gtk.Alignment leftAlign (Gtk.Widget widget)
		{
			Gtk.Alignment alignment = new Gtk.Alignment (0, 0, 0, 0);
			alignment.Add (widget);

			return alignment;
		}

		/// <summary>
		/// Right aligns the widget
		/// </summary>
		/// <returns>The align.</returns>
		/// <param name="widget">Widget.</param>
		private Gtk.Alignment rightAlign (Gtk.Widget widget)
		{
			Gtk.Alignment alignment = new Gtk.Alignment (1, 0, 0, 0);
			alignment.Add (widget);

			return alignment;
		}

		/// <summary>
		/// Pads the sides of the widget
		/// </summary>
		/// <returns>The top and bottom.</returns>
		/// <param name="widget">Widget.</param>
		private Gtk.Alignment padWidget(Gtk.Widget widget, uint topPadding, uint bottomPadding, uint leftPadding, uint rightPadding)
		{
			Gtk.Alignment alignment = this.leftAlign (widget);

			alignment.TopPadding = topPadding;
			alignment.BottomPadding = bottomPadding;
			alignment.LeftPadding = leftPadding;
			alignment.RightPadding = rightPadding;

			return alignment;
		}

		/// <summary>
		/// Pads the sides of the widget
		/// </summary>
		/// <returns>The top and bottom.</returns>
		/// <param name="widget">Widget.</param>
		private Gtk.Alignment padWidget(Gtk.Alignment alignment, uint topPadding, uint bottomPadding, uint leftPadding, uint rightPadding)
		{
			alignment.TopPadding = topPadding;
			alignment.BottomPadding = bottomPadding;
			alignment.LeftPadding = leftPadding;
			alignment.RightPadding = rightPadding;

			return alignment;
		}

		#endregion
	}
}