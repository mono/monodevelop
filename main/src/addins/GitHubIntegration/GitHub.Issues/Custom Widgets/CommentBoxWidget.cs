using System;

namespace GitHub.Issues
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class CommentBoxWidget : Gtk.Bin
	{
		#region Private Members

		/// <summary>
		/// The control factory.
		/// </summary>
		private CommonControlsFactories controlFactory;

		/// <summary>
		/// The comment text box.
		/// </summary>
		private Gtk.TextView commentTextBox;

		#endregion

		#region Public Properties

		/// <summary>
		/// Gets the text from the comment text box
		/// </summary>
		/// <value>The comment text.</value>
		public String CommentText
		{
			get {
				return this.commentTextBox.Buffer.Text;
			}
		}

		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="GitHub.Issues.CommentBoxWidget"/> class.
		/// </summary>
		/// <param name="buttonText">Button text.</param>
		/// <param name="addCommentHandler">Add comment handler.</param>
		public CommentBoxWidget (String buttonText, EventHandler addCommentHandler)
		{
			this.Build ();

			this.controlFactory = new CommonControlsFactories ();

			Gtk.VBox mainContainer = new Gtk.VBox ();

			mainContainer.Add (this.commentTextBox = this.createCommentTextBox ());
			mainContainer.Add (LayoutUtilities.RightAlign (this.createAddCommentButton (buttonText, addCommentHandler)));
			this.commentTextBox.TooltipText = "Type comment here...";
			this.Add (mainContainer);
		}

		#endregion

		#region Create UI Compoments

		/// <summary>
		/// Creates the comment text box.
		/// </summary>
		/// <returns>The comment text box.</returns>
		private Gtk.TextView createCommentTextBox()
		{
			return this.controlFactory.CreateTextBox (string.Empty, 5);
		}
			
		/// <summary>
		/// Creates the add comment button.
		/// </summary>
		/// <returns>The add comment button.</returns>
		/// <param name="buttonText">Button text.</param>
		/// <param name="addCommentHandler">Add comment handler.</param>
		private Gtk.Button createAddCommentButton(String buttonText, EventHandler addCommentHandler)
		{
			return this.controlFactory.CreateButton(buttonText, addCommentHandler);
		}

		#endregion

		#region Maintenance

		/// <summary>
		/// Reset this instance.
		/// </summary>
		public void Reset()
		{
			this.commentTextBox.Buffer.Text = string.Empty;
		}

		#endregion
	}
}