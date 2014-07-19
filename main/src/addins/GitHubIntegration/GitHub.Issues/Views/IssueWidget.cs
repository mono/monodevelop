using System;
using System.Collections.Generic;
using System.Linq;

namespace GitHub.Issues
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class IssueWidget : Gtk.Bin
	{
		/// <summary>
		/// Issue that is represented by this screen
		/// </summary>
		private Octokit.Issue issue;

		/// <summary>
		/// The labels store.
		/// </summary>
		private Gtk.ListStore labelsStore;

		/// <summary>
		/// The common controls factory.
		/// </summary>
		private CommonControlsFactories commonControlsFactory;

		/// <summary>
		/// Issues manager
		/// </summary>
		IssuesManager manager = new IssuesManager ();

		/// <summary>
		/// Constructor which creates and initializes the widget so that its ready to use
		/// </summary>
		/// <param name="issue">Issue to show on screen</param>
		public IssueWidget (Octokit.Issue issue)
		{
			this.Build ();

			this.issue = issue;

			this.commonControlsFactory = new CommonControlsFactories ();

			this.CreateAndInitializeWidget (this.issue);

			this.ShowAll ();
		}

		/// <summary>
		/// Creates the and initializes the widget with the information from the specified issue
		/// </summary>
		/// <param name="issue">Issue to show on screen</param>
		private void CreateAndInitializeWidget(Octokit.Issue issue)
		{
			Gtk.HBox mainSplit = new Gtk.HBox (false, 5);

			Gtk.VBox labelsPanel = new Gtk.VBox (false, 5);
			Gtk.VBox detailsPanel = new Gtk.VBox (false, 5);
			Gtk.VBox commentsPanel = new Gtk.VBox (false, 5);

			Gtk.Table labelsTable = new Gtk.Table (1, 1, false);
			Gtk.Table detailsTable = new Gtk.Table (3, 2, false);

			Gtk.AttachOptions xOptions = Gtk.AttachOptions.Fill;
			Gtk.AttachOptions yOptions = Gtk.AttachOptions.Expand;

			uint left = 1;
			uint right = 2;
			// First control using this can't use it as ++top like the others since we want to start at 0, -1 is not possible because its a uint
			uint top = 0;
			uint bottom = 0;

			//******************************LABELS PANEL************************************

			// Issue Labels
			labelsTable.Attach (LayoutUtilities.SetPadding(LayoutUtilities.LeftAlign (this.CreateLabelsTreeView (issue)), 0 ,0, 0, 5),
				--left, --right, top, ++bottom, xOptions, yOptions, 5, 5);

			labelsTable.Show ();

			labelsPanel.PackStart (labelsTable, false, false, 5);

			// Reset counters

			left = 1;
			right = 2;
			top = 0;
			bottom = 0;

			//******************************DETAILS PANEL************************************

			// Issue name
			Gtk.TextView issueTitle = this.createIssueNameTextBox (issue.Title);
			LayoutUtilities.SetUpWidthBinding (detailsTable, issueTitle, -80);

			detailsTable.Attach (LayoutUtilities.LeftAlign(this.createIssueNameTitleLabel ()), --left, --right, top, ++bottom, xOptions, Gtk.AttachOptions.Fill, 5, 5);
			detailsTable.Attach (LayoutUtilities.LeftAlign(issueTitle), ++left, ++right, top, bottom, xOptions, yOptions, 5, 5);

			// Issue State
			detailsTable.Attach (LayoutUtilities.LeftAlign (this.CreateIssueStateLabel ()), --left, --right, ++top, ++bottom, xOptions, Gtk.AttachOptions.Fill, 5, 5);
			detailsTable.Attach (LayoutUtilities.LeftAlign (this.CreateIssueStateIndicator (issue)), ++left, ++right, top, bottom, xOptions, yOptions, 5, 5);

			detailsTable.Attach (LayoutUtilities.LeftAlign(this.createIssueBodyLabel ()), --left, --right, ++top, ++bottom, xOptions, Gtk.AttachOptions.Fill, 5, 5);

			Gtk.TextView descriptionTextBox = this.createIssueBodyTextBox (issue.Body);
			LayoutUtilities.SetUpWidthBinding (detailsTable, descriptionTextBox, -80);

			detailsTable.Attach (LayoutUtilities.LeftAlign(descriptionTextBox), ++left, ++right, top, bottom, Gtk.AttachOptions.Fill, yOptions, 5, 5);
			descriptionTextBox.WrapMode = Gtk.WrapMode.Word;
			detailsTable.Show ();

			detailsPanel.PackStart (detailsTable, false, true, 5);

			//******************************COMMENTS PANEL************************************

			// Create the comments widgets
			List<CommentWidget> comments = this.createCommentWidgets (this.manager.GetCommentsForIssue (this.issue));

			Gtk.VBox commentsContainer = new Gtk.VBox ();

			if (comments.Count > 0) {
				// Display all comments on screen - separated by a horizontal line
				foreach (CommentWidget comment in comments) {
					commentsContainer.Add (comment);
					commentsContainer.Add (new Gtk.HSeparator ());
				}
			} else {
				// Display a message if there are no comments - to avoid having a big blank space on screen
				commentsContainer.Add (LayoutUtilities.SetPadding(LayoutUtilities.CenterHorizontalAlign(new Gtk.Label ("No comments...")), 10, 15, 0, 0));
				commentsContainer.Add (new Gtk.HSeparator ());
			}

			commentsPanel.PackStart (commentsContainer, false, false, 5);

			//******************************PACK MAIN PANEL************************************

			mainSplit.PackStart (detailsPanel, false, true, 0);
			mainSplit.PackStart (labelsPanel, false, true, 0);
			mainSplit.PackStart (commentsPanel, true, true, 0);

			// Bind details panel to use 40% of the main panels width
			LayoutUtilities.SetUpWidthBinding (mainSplit, detailsPanel, 0.4);

			this.Add (mainSplit);
		}

		#region Creation of UI Components

		/// <summary>
		/// Creates the comment widgets.
		/// </summary>
		/// <returns>The comment widgets.</returns>
		/// <param name="comments">Comments.</param>
		private List<CommentWidget> createCommentWidgets(IReadOnlyList<Octokit.IssueComment> comments)
		{
			List<CommentWidget> commentWidgets = new List<CommentWidget> ();

			foreach (Octokit.IssueComment comment in comments) {
				commentWidgets.Add (new CommentWidget (comment));
			}

			return commentWidgets;
		}

		/// <summary>
		/// Creates the issue name text box.
		/// </summary>
		/// <returns>The issue name text box</returns>
		/// <param name="text">Name of the issue to put into the textbox</param>
		private Gtk.TextView createIssueNameTextBox(string text)
		{
			return this.commonControlsFactory.CreateTextBox (text, 1, Gtk.WrapMode.WordChar);
		}

		/// <summary>
		/// Creates the issue name title label.
		/// </summary>
		/// <returns>The issue name title label.</returns>
		private Gtk.Label createIssueNameTitleLabel()
		{
			return this.commonControlsFactory.CreateLabel (StringResources.IssueName);
		}

		/// <summary>
		/// Creates the issue body text box.
		/// </summary>
		/// <returns>The issue body text box.</returns>
		/// <param name="text">Text.</param>
		private Gtk.TextView createIssueBodyTextBox(string text)
		{
			return this.commonControlsFactory.CreateTextBox (text, 10, Gtk.WrapMode.WordChar);
		}

		/// <summary>
		/// Creates the issue body label.
		/// </summary>
		/// <returns>The issue body label.</returns>
		private Gtk.Label createIssueBodyLabel()
		{
			return this.commonControlsFactory.CreateLabel (StringResources.IssueDescription);
		}

		/// <summary>
		/// Creates the labels tree view label.
		/// </summary>
		/// <returns>The labels tree view label.</returns>
		private Gtk.Label CreateLabelsTreeViewLabel()
		{
			return this.commonControlsFactory.CreateLabel (StringResources.Labels);
		}

		/// <summary>
		/// Creates the labels tree list store.
		/// </summary>
		/// <returns>The labels tree list store.</returns>
		/// <param name="issue">Issue.</param>
		private Gtk.ListStore createLabelsTreeListStore(Octokit.Issue issue)
		{
			Gtk.ListStore store = new Gtk.ListStore (typeof(Boolean), typeof(String), typeof(String));

			IReadOnlyList<Octokit.Label> labels = this.manager.GetAllLabels ();

			List<Octokit.Label> unselectedLabels = new List<Octokit.Label> ();

			foreach (Octokit.Label label in labels) {
				unselectedLabels.Add (label);
			}

			// Filter out the unselected labels
			foreach (Octokit.Label selectedLabel in issue.Labels) {
				Octokit.Label labelToRemove = null;

				foreach (Octokit.Label label in labels) {
					if (label.Name == selectedLabel.Name && label.Color == selectedLabel.Color && label.Url == selectedLabel.Url) {
						labelToRemove = label;
						break;
					}
				}

				unselectedLabels.Remove (labelToRemove);
			}

			// Append all selected labels
			foreach (Octokit.Label label in issue.Labels) {
				store.AppendValues (true, label.Name, label.Color);
			}

			// Append the rest of unselected labels onto the end
			foreach (Octokit.Label label in unselectedLabels) {
				store.AppendValues (false, label.Name, label.Color);
			}

			return store;
		}

		/// <summary>
		/// Creates the labels tree view.
		/// </summary>
		/// <returns>The labels tree view.</returns>
		/// <param name="issue">Issue.</param>
		private Gtk.TreeView CreateLabelsTreeView(Octokit.Issue issue)
		{
			Gtk.TreeView treeView = new Gtk.TreeView ();

			Gtk.CellRendererToggle selectionToggle = new Gtk.CellRendererToggle ();
			selectionToggle.Mode = Gtk.CellRendererMode.Activatable;
			selectionToggle.Toggled += this.toggleRenderedToggledHandlerIssueLabels;

			Gtk.CellRendererText labelRenderer = new Gtk.CellRendererText ();

			Gtk.TreeViewColumn selectionColumn = new Gtk.TreeViewColumn ("", selectionToggle, "active", 0);
			Gtk.TreeViewColumn labelColumn = new Gtk.TreeViewColumn ("Label", labelRenderer, "text", 1);

			treeView.AppendColumn (selectionColumn);
			treeView.AppendColumn (labelColumn);

			selectionColumn.SetCellDataFunc (selectionToggle, new Gtk.TreeCellDataFunc(this.ColorLabelRow));
			labelColumn.SetCellDataFunc (labelRenderer, new Gtk.TreeCellDataFunc(this.ColorLabelRow));

			treeView.ModifyBase (Gtk.StateType.Normal, new Gdk.Color (245, 245, 245));
			treeView.HeadersVisible = false;

			treeView.Model = this.labelsStore = this.createLabelsTreeListStore (issue);

			return treeView;
		}

		/// <summary>
		/// Creates the issue state label.
		/// </summary>
		/// <returns>The issue state label.</returns>
		private Gtk.Label CreateIssueStateLabel()
		{
			return this.commonControlsFactory.CreateLabel (StringResources.State);
		}

		/// <summary>
		/// Creates the issue state indicator.
		/// </summary>
		/// <returns>The issue state indicator.</returns>
		/// <param name="issue">Issue.</param>
		private Gtk.Label CreateIssueStateIndicator(Octokit.Issue issue)
		{
			return this.commonControlsFactory.CreateLabel (issue.State.ToString ());
		}

		#endregion

		#region Event Handlers

		/// <summary>
		/// When a toggle is clicked on the toggle cell renderer, this method handles setting the new value of the toggle
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="args">Arguments.</param>
		private void toggleRenderedToggledHandlerIssueLabels(object sender, Gtk.ToggledArgs args)
		{
			TreeViewUtilities.ToggleCheckBoxRenderer (this.labelsStore, 0, args);
		}

		/// <summary>
		/// Colors the label row.
		/// </summary>
		/// <param name="column">Column.</param>
		/// <param name="cell">Cell.</param>
		/// <param name="model">Model.</param>
		/// <param name="iterator">Iterator.</param>
		private void ColorLabelRow(Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iterator)
		{
			String color = (String)model.GetValue (iterator, 2);

			if (!string.IsNullOrEmpty (color)) {
				try {
					System.Drawing.Color colorRGB = System.Drawing.ColorTranslator.FromHtml ('#' + color);
					cell.CellBackgroundGdk = new Gdk.Color (colorRGB.R, colorRGB.G, colorRGB.B);
				}
				catch (Exception) {
				}
			}
		}

		#endregion
	}
}