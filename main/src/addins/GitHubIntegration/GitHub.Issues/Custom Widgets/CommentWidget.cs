using System;
using Pango;

namespace GitHub.Issues
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class CommentWidget : Gtk.Bin
	{
		#region Private Members

		/// <summary>
		/// The control factory.
		/// </summary>
		private CommonControlsFactories controlFactory = new CommonControlsFactories ();

		/// <summary>
		/// The delete button handler.
		/// </summary>
		private EventHandler<DeleteCommentClickEventArgs> deleteButtonHandler;

		/// <summary>
		/// Comment which is represented by this control.
		/// </summary>
		private Octokit.IssueComment comment;

		#endregion

		/// <summary>
		/// Initializes a new instance of the <see cref="GitHub.Issues.CommentWidget"/> class.
		/// </summary>
		/// <param name="comment">Comment.</param>
		public CommentWidget (Octokit.IssueComment comment, EventHandler<DeleteCommentClickEventArgs> deleteButtonHandler)
		{
			this.Build ();

			this.comment = comment;

			Gtk.VBox mainContainer = new Gtk.VBox ();

			Gtk.HBox topPanel = new Gtk.HBox ();

			this.deleteButtonHandler = deleteButtonHandler;

			topPanel.Add (this.padWidget (this.leftAlign (this.createOwnerTextBox (comment)), 10, 0, 0, 10));

			// Contains the date and time of writting and the delete button
			Gtk.HBox dateAndDeleteContainer = new Gtk.HBox ();
			dateAndDeleteContainer.Add (this.createCommentDateTextBox (comment));
			dateAndDeleteContainer.Add (this.padWidget (this.rightAlign (this.createDeleteButton (this.DeleteButtonHandler)), 10, 0, 5, 10));

			topPanel.Add (this.padWidget (this.rightAlign (dateAndDeleteContainer), 10, 0, 0, 0));

			mainContainer.Add (topPanel);

			Gtk.Label commentBox = this.createCommentBox (comment);

			// Bind the width to the parent size
			mainContainer.SizeAllocated += (object o, Gtk.SizeAllocatedArgs args) => {
				commentBox.WidthRequest = args.Allocation.Width;
			};

			mainContainer.Add (this.padWidget (commentBox, 10, 10, 0, 10));
			mainContainer.Add (new Gtk.HSeparator ());

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
		private Gtk.Label createCommentDateTextBox (Octokit.IssueComment comment)
		{
			Gtk.Label date = new Gtk.Label (comment.CreatedAt.DateTime.ToString ());

			return date;
		}

		/// <summary>
		/// Creates the comment box.
		/// </summary>
		/// <returns>The comment box.</returns>
		/// <param name="comment">Comment.</param>
		private Gtk.Label createCommentBox (Octokit.IssueComment comment)
		{
			Gtk.Label commentBox = new Gtk.Label (comment.Body);
			commentBox.LineWrapMode = Pango.WrapMode.WordChar;
			commentBox.Wrap = true;

			return commentBox;
		}

		/// <summary>
		/// Creates the delete button.
		/// </summary>
		/// <returns>The delete button.</returns>
		/// <param name="handler">Handler.</param>
		private Gtk.Button createDeleteButton (EventHandler handler)
		{
			return this.controlFactory.CreateButton ("X", handler);
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
		private Gtk.Alignment padWidget (Gtk.Widget widget, uint topPadding, uint bottomPadding, uint leftPadding, uint rightPadding)
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
		private Gtk.Alignment padWidget (Gtk.Alignment alignment, uint topPadding, uint bottomPadding, uint leftPadding, uint rightPadding)
		{
			alignment.TopPadding = topPadding;
			alignment.BottomPadding = bottomPadding;
			alignment.LeftPadding = leftPadding;
			alignment.RightPadding = rightPadding;

			return alignment;
		}

		#endregion

		#region Event Handlers

		/// <summary>
		/// Event handler which fires another handler off when the delete button is clicked for this comment
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">Arguments.</param>
		private void DeleteButtonHandler (object sender, EventArgs e)
		{
			this.deleteButtonHandler (this, new DeleteCommentClickEventArgs () {
				CommentToDelete = this.comment
			});
		}

		#endregion
	}
}