using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace GitHub.Issues
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class IssueWidget : Gtk.Bin
	{
		/// <summary>
		/// Issue that is represented by this screen
		/// </summary>
		private Octokit.Issue issue;

		#region Issue UI Fields

		/// <summary>
		/// The labels store.
		/// </summary>
		private Gtk.ListStore labelsStore;

		/// <summary>
		/// The milestone store.
		/// </summary>
		private Gtk.ListStore milestoneStore;

		/// <summary>
		/// The states store.
		/// </summary>
		private Gtk.ListStore statesStore;

		/// <summary>
		/// The assignee store.
		/// </summary>
		private Gtk.ListStore assigneeStore;

		/// <summary>
		/// The comments container.
		/// </summary>
		private Gtk.VBox commentsContainer;

		/// <summary>
		/// Issue title text box
		/// </summary>
		private Gtk.TextView issueTitle;

		/// <summary>
		/// Issue body text box
		/// </summary>
		private Gtk.TextView issueBody;

		/// <summary>
		/// The comment box for adding new comments
		/// </summary>
		private CommentBoxWidget commentBox;

		/// <summary>
		/// Tree view containing the labels (both assigned and unassigned)
		/// </summary>
		private Gtk.TreeView issueLabelsTreeView;

		/// <summary>
		/// The issue state combo box.
		/// </summary>
		private Gtk.ComboBox issueStateComboBox;

		/// <summary>
		/// The issue milestone combo box.
		/// </summary>
		private Gtk.ComboBox issueMilestoneComboBox;

		/// <summary>
		/// The issue assignee combo box.
		/// </summary>
		private Gtk.ComboBox issueAssigneeComboBox;

		#endregion

		/// <summary>
		/// Edit mode specifying whether the save button should create or update an issue
		/// Edit by default
		/// </summary>
		private EditMode editMode = EditMode.Edit;

		/// <summary>
		/// The common controls factory.
		/// </summary>
		private CommonControlsFactories commonControlsFactory;

		/// <summary>
		/// Specifies whether or not there are comments in this issue or not
		/// Used to decide whther to clear the comments container or not when adding a new comment
		/// </summary>
		private bool noComments;

		/// <summary>
		/// Issues manager
		/// </summary>
		IssuesManager manager = new IssuesManager ();

		#region Events

		/// <summary>
		/// Called when the issue is saved
		/// </summary>
		public EventHandler<IssueSavedEventArgs> IssueSaved;

		#endregion

		/// <summary>
		/// Constructor which creates and initializes the widget so that its ready to use
		/// </summary>
		/// <param name="issue">Issue to show on screen, if [null] then we assume creation of new issue</param>
		public IssueWidget (Octokit.Issue issue)
		{
			this.Build ();

			this.issue = issue;

			if (this.issue != null) {
				// Since we have an issue passed in we are editing an issue
				this.editMode = EditMode.Edit;
			}
			else {
				this.editMode = EditMode.Creation;
			}

			this.commonControlsFactory = new CommonControlsFactories ();

			this.CreateAndInitializeWidget (this.issue);

			// Disable or enable the appropriate features based on which mode we are currently in
			if (this.editMode == EditMode.Creation) {
				this.DisableEditFeatures ();
			}
			else if (this.editMode == EditMode.Edit) {
				this.EnableEditFeatures ();
			}

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
			Gtk.Table detailsTable = new Gtk.Table (5, 2, false);

			Gtk.AttachOptions xOptions = Gtk.AttachOptions.Fill;
			Gtk.AttachOptions yOptions = Gtk.AttachOptions.Expand;

			uint left = 1;
			uint right = 2;
			// First control using this can't use it as ++top like the others since we want to start at 0, -1 is not possible because its a uint
			uint top = 0;
			uint bottom = 0;

			//******************************LABELS PANEL************************************

			// Issue Labels
			labelsTable.Attach (LayoutUtilities.SetPadding(LayoutUtilities.LeftAlign (this.issueLabelsTreeView = this.CreateLabelsTreeView (issue)), 0 ,0, 0, 5),
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
			this.issueTitle = this.createIssueNameTextBox (this.issue != null ? this.issue.Title : StringResources.NewIssueTitle);

			// Used to easily account for the text boxes going out of bounds and running over onto the next panel
			// Manually tweaked value - couldn't find a way to get a column's width of the table programmatically so ended up using this *** HACKY SOLUTION ***
			int hackyLeftColumnWidth = 142;

			LayoutUtilities.SetUpWidthBinding (detailsTable, issueTitle, -hackyLeftColumnWidth);

			// Issue Title
			detailsTable.Attach (LayoutUtilities.LeftAlign(this.createIssueNameTitleLabel ()), --left, --right, top, ++bottom, xOptions, Gtk.AttachOptions.Fill, 5, 5);
			detailsTable.Attach (LayoutUtilities.LeftAlign(this.issueTitle), ++left, ++right, top, bottom, xOptions, yOptions, 5, 5);

			// Issue State
			detailsTable.Attach (LayoutUtilities.LeftAlign (this.CreateIssueStateLabel ()), --left, --right, ++top, ++bottom, xOptions, Gtk.AttachOptions.Fill, 5, 5);
			detailsTable.Attach (LayoutUtilities.LeftAlign (this.issueStateComboBox = this.CreateIssueStateComboBox (issue)), ++left, ++right, top, bottom, xOptions, yOptions, 5, 5);

			// Issue Milestone
			detailsTable.Attach (LayoutUtilities.LeftAlign (this.CreateIssueMilestoneLabel ()), --left, --right, ++top, ++bottom, xOptions, Gtk.AttachOptions.Fill, 5, 5);
			detailsTable.Attach (LayoutUtilities.LeftAlign (this.issueMilestoneComboBox = this.CreateIssueMilestonesComboBox(issue)), ++left, ++right, top, bottom, xOptions, yOptions, 5, 5);

			// Issue Assignee
			detailsTable.Attach (LayoutUtilities.LeftAlign (this.CreateIssueAssigneeLabel ()), --left, --right, ++top, ++bottom, xOptions, Gtk.AttachOptions.Fill, 5, 5);
			detailsTable.Attach (LayoutUtilities.LeftAlign (this.issueAssigneeComboBox = this.CreateIssueAssigneeComboBox(issue)), ++left, ++right, top, bottom, xOptions, yOptions, 5, 5);

			// Issue Body
			detailsTable.Attach (LayoutUtilities.LeftAlign(this.createIssueBodyLabel ()), --left, --right, ++top, ++bottom, xOptions, Gtk.AttachOptions.Fill, 5, 5);
			this.issueBody = this.createIssueBodyTextBox (this.issue != null ? this.issue.Body : string.Empty);
			LayoutUtilities.SetUpWidthBinding (detailsTable, this.issueBody, -hackyLeftColumnWidth);
			detailsTable.Attach (LayoutUtilities.LeftAlign(this.issueBody), ++left, ++right, top, bottom, Gtk.AttachOptions.Fill, yOptions, 5, 5);
			this.issueBody.WrapMode = Gtk.WrapMode.Word;

			detailsTable.Show ();

			detailsPanel.PackStart (detailsTable, false, true, 5);

			//******************************COMMENTS PANEL************************************

			// Create the comments widgets
			List<CommentWidget> comments = new List<CommentWidget> ();

			if (this.issue != null) {
				comments = this.createCommentWidgets (this.manager.GetCommentsForIssue (this.issue));
			}

			this.commentsContainer = new Gtk.VBox ();

			if (comments.Count > 0) {
				// Display all comments on screen - separated by a horizontal line
				foreach (CommentWidget comment in comments) {
					this.AddCommentToList (this.commentsContainer, comment);
				}

				this.noComments = false;
			} else {
				// Display a message if there are no comments - to avoid having a big blank space on screen
				this.AddNoCommentsLabelToList (this.commentsContainer);

				this.noComments = true;
			}

			this.commentBox = new CommentBoxWidget (StringResources.AddComment, this.AddCommentHandler);
			
			commentsPanel.PackStart (this.commentsContainer, false, true, 5);
			commentsPanel.PackStart (this.commentBox, false, true, 5);

			//******************************PACK MAIN PANEL************************************

			Gtk.VBox verticalSplit = new Gtk.VBox ();

			mainSplit.PackStart (detailsPanel, false, true, 0);
			mainSplit.PackStart (labelsPanel, false, true, 0);
			mainSplit.PackStart (commentsPanel, true, true, 0);

			// Bind details panel to use 40% of the main panels width
			LayoutUtilities.SetUpWidthBinding (mainSplit, detailsPanel, 0.4);

			// Header and Body type of layout
			verticalSplit.PackStart (LayoutUtilities.LeftAlign(this.commonControlsFactory.CreateButton (StringResources.Save, this.SaveIssueDetailsHandler)), false, true, 5);
			verticalSplit.PackStart (mainSplit, true, true, 5);

			this.Add (verticalSplit);
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
				commentWidgets.Add (new CommentWidget (comment, this.DeleteCommentHandler));
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
			if (issue != null) {
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
		/// Creates the issue milestone label.
		/// </summary>
		/// <returns>The issue milestone label.</returns>
		private Gtk.Label CreateIssueMilestoneLabel()
		{
			return this.commonControlsFactory.CreateLabel (StringResources.AssignedToMilestone);
		}

		/// <summary>
		/// Creates the issue assignee label.
		/// </summary>
		/// <returns>The issue assignee label.</returns>
		private Gtk.Label CreateIssueAssigneeLabel()
		{
			return this.commonControlsFactory.CreateLabel (StringResources.Assignee);
		}

		/// <summary>
		/// Creates the issue state indicator.
		/// </summary>
		/// <returns>The issue state indicator.</returns>
		/// <param name="issue">Issue.</param>
		private Gtk.ComboBox CreateIssueStateComboBox(Octokit.Issue issue)
		{
			// 0: State name
			this.statesStore = new Gtk.ListStore (typeof(Octokit.ItemState), typeof(String));

			foreach (Octokit.ItemState state in Enum.GetValues(typeof(Octokit.ItemState))) {
				this.statesStore.AppendValues (state, state.ToString());
			}

			Gtk.ComboBox comboBox = this.commonControlsFactory.CreateComboBox (this.statesStore, delegate(object sender, EventArgs e) {
				// Handler code goes here - don't need anything here
			}, 1, "text");

			// Set the combobox to select the correct item depending on the current issue state
			int activeIndex = 0;

			if (issue != null) {
				// Current issue state
				Octokit.ItemState issueState = issue.State;

				foreach (Octokit.ItemState item in Enum.GetValues(typeof(Octokit.ItemState))) {
					if (item == issueState) {
						break;
					}

					activeIndex++;
				}
			}

			comboBox.Active = activeIndex;

			return comboBox;
		}

		/// <summary>
		/// Creates the issue milestones combo box.
		/// </summary>
		/// <returns>The issue milestones combo box.</returns>
		/// <param name="issue">Issue.</param>
		private Gtk.ComboBox CreateIssueMilestonesComboBox(Octokit.Issue issue)
		{
			IReadOnlyList<Octokit.Milestone> milestones = this.manager.GetAllMilestones ();

			// 0: Object Reference, 1: Title of the Milestone
			this.milestoneStore = new Gtk.ListStore (typeof(Octokit.Milestone), typeof(String));

			foreach (Octokit.Milestone milestone in milestones) {
				this.milestoneStore.AppendValues (milestone, milestone.Title);
			}

			Gtk.ComboBox comboBox = this.commonControlsFactory.CreateComboBox (this.milestoneStore, delegate(object sender, EventArgs e) {
				// Handler code goes here - don't need anything here at the minute
			}, 1, "text");

			if (issue != null) {
				if (issue.Milestone == null) {
					// Select nothing if no milestone is selected
					comboBox.Active = -1;
				} else {
					int activeIndex = 0;

					foreach (Octokit.Milestone milestone in milestones) {
						if (milestone.Number == issue.Milestone.Number) {
							break;
						}

						activeIndex++;
					}

					comboBox.Active = activeIndex;
				}
			}

			return comboBox;
		}

		/// <summary>
		/// Creates the issue assignee combo box.
		/// </summary>
		/// <returns>The issue assignee combo box.</returns>
		/// <param name="issue">Issue.</param>
		public Gtk.ComboBox CreateIssueAssigneeComboBox(Octokit.Issue issue)
		{
			IReadOnlyList<Octokit.User> possibleAssignees = this.manager.GetAllAssignees ();

			// 0: Object Reference, 1: Login of the User, 2: Name of the User
			this.assigneeStore = new Gtk.ListStore (typeof(Octokit.User), typeof(String), typeof(String));

			foreach (Octokit.User possibleAssignee in possibleAssignees) {
				this.assigneeStore.AppendValues (possibleAssignee, possibleAssignee.Login, possibleAssignee.Name);
			}

			// Show both the login and the name in the combo box
			Gtk.ComboBox comboBox = this.commonControlsFactory.CreateComboBox (this.assigneeStore, delegate(object sender, EventArgs e) {
				// Handler code goes here - don't need anything here at the minute
			}, 1, "text", 2, "text");

			if (issue != null) {
				if (issue.Assignee == null) {
					// Select nothing if no assignee is selected
					comboBox.Active = -1;
				} else {
					int activeIndex = 0;

					foreach (Octokit.User possibleAssignee in possibleAssignees) {
						if (possibleAssignee.Login == issue.Assignee.Login) {
							break;
						}

						activeIndex++;
					}

					comboBox.Active = activeIndex;
				}
			}

			return comboBox;
		}

		#endregion

		#region UI Helpers

		/// <summary>
		/// Adds the comment to the list in the UI.
		/// </summary>
		/// <param name="container">Container to add the comment to.</param>
		/// <param name="comment">Comment to add.</param>
		private void AddCommentToList(Gtk.VBox container, CommentWidget comment)
		{
			container.Add (comment);
		}

		/// <summary>
		/// Adds the "no comments label" to comments list.
		/// </summary>
		/// <param name="container">Comments container.</param>
		private void AddNoCommentsLabelToList(Gtk.VBox container)
		{
			container.Add (LayoutUtilities.SetPadding(LayoutUtilities.CenterHorizontalAlign(new Gtk.Label ("No comments...")), 10, 15, 0, 0));
			container.Add (new Gtk.HSeparator ());
		}

		/// <summary>
		/// Gets the selected labels.
		/// </summary>
		/// <returns>The selected labels.</returns>
		private List<String> GetSelectedLabels()
		{
			if (this.labelsStore != null) {
				IEnumerator rowEnumerator = this.labelsStore.GetEnumerator ();

				// Here we will store string representations of the selected labels
				List<String> selectedLabels = new List<String> ();

				while (rowEnumerator.MoveNext ()) {
					// 0 - Selected?, 1 - Label Name, 2 - Label Color
					Array currentRow = (Array)rowEnumerator.Current;

					// Get column property (not title - not the user friendly name) and the row index
					if ((bool)currentRow.GetValue(0) == true)
					{
						selectedLabels.Add ((String)currentRow.GetValue(1));
					}
				}

				return selectedLabels;
			}

			return new List<String>();
		}

		/// <summary>
		/// Gets the selected milestone.
		/// </summary>
		/// <returns>The selected milestone.</returns>
		private Octokit.Milestone GetSelectedMilestone()
		{
			Octokit.Milestone milestone = null;

			if (this.milestoneStore != null) {
				Gtk.TreeIter iterator = new Gtk.TreeIter();

				// If there is a selection then retrieve the item as a milestone and return it
				if (this.issueMilestoneComboBox.GetActiveIter (out iterator)) {
					milestone = (Octokit.Milestone)this.milestoneStore.GetValue (iterator, 0);
				}
			}

			return milestone;
		}

		/// <summary>
		/// Gets the state of the selected issue.
		/// </summary>
		/// <returns>The selected issue state.</returns>
		private Octokit.ItemState GetSelectedIssueState()
		{
			// Default to the first one in the list (Opened) - Each issue has to be in some state at any given point in time
			Octokit.ItemState state = Octokit.ItemState.Open;

			if (this.statesStore != null) {
				Gtk.TreeIter iterator = new Gtk.TreeIter();

				// If there is a selection then retrieve the item as a milestone and return it
				if (this.issueStateComboBox.GetActiveIter (out iterator)) {
					state = (Octokit.ItemState)this.statesStore.GetValue (iterator, 0);
				}
			}

			return state;
		}

		/// <summary>
		/// Gets the selected assignee.
		/// </summary>
		/// <returns>The selected assignee.</returns>
		private Octokit.User GetSelectedAssignee()
		{
			Octokit.User user = null;

			if (this.assigneeStore != null) {
				Gtk.TreeIter iterator = new Gtk.TreeIter();

				// If there is a selection then retrieve the item as a milestone and return it
				if (this.issueAssigneeComboBox.GetActiveIter (out iterator)) {
					user = (Octokit.User)this.assigneeStore.GetValue (iterator, 0);
				}
			}

			return user;
		}

		/// <summary>
		/// Enables features which are only available when editing an existing issue not during the creation
		/// </summary>
		private void EnableEditFeatures()
		{
			this.issueStateComboBox.Sensitive = true;
			this.commentBox.Sensitive = true;
		}

		/// <summary>
		/// Disables the edit features for the time of the creation of the issue, once the issue is saved these features should be enabled
		/// </summary>
		private void DisableEditFeatures()
		{
			this.issueStateComboBox.Sensitive = false;
			this.commentBox.Sensitive = false;
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

		/// <summary>
		/// Adds the comment handler.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="args">Arguments.</param>
		private void AddCommentHandler(object sender, EventArgs args)
		{
			Octokit.IssueComment comment = this.manager.AddComment (this.issue, this.commentBox.CommentText);

			// Clear the container from the "No Comments" label
			if (this.noComments) {
				Gtk.Widget[] children = this.commentsContainer.Children;

				foreach (Gtk.Widget child in children) {
					this.commentsContainer.Remove (child);
				}
			}

			this.AddCommentToList (this.commentsContainer, new CommentWidget(comment, this.DeleteCommentHandler));

			this.noComments = false;

			this.commentsContainer.ShowAll ();

			// Clear the comment box
			this.commentBox.Reset ();
		}

		/// <summary>
		/// Deletes the comment for the current issue.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="args">Arguments.</param>
		private void DeleteCommentHandler(object sender, DeleteCommentClickEventArgs args)
		{
			this.manager.DeleteComment (args.CommentToDelete);

			this.commentsContainer.Remove ((Gtk.Widget)sender);
		}

		/// <summary>
		/// Saves the issue details click handler
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">Event arguments.</param>
		private void SaveIssueDetailsHandler(object sender, EventArgs e)
		{
			if (this.editMode == EditMode.Edit) {
				this.Update ();
			}
			else if (this.editMode == EditMode.Creation) {
				this.Create ();
				this.editMode = EditMode.Edit;
				this.EnableEditFeatures ();
			}

			// Call the issue saved event
			this.IssueSaved (this, new IssueSavedEventArgs () {
				issue = this.issue
			});
		}

		#endregion

		#region Creating and Updating Issues

		/// <summary>
		/// Updates an already created issue
		/// </summary>
		private void Update()
		{
			Octokit.User assignee = this.GetSelectedAssignee ();

			this.manager.UpdateIssue (this.issue, 
				this.issueTitle.Buffer.Text, 
				this.issueBody.Buffer.Text, 
				assignee == null ? string.Empty : assignee.Login, 
				this.GetSelectedLabels ().ToArray (),
				this.GetSelectedIssueState (),
				this.GetSelectedMilestone ());
		}

		/// <summary>
		/// Creates a new issue on GitHub Issues system
		/// </summary>
		private void Create()
		{
			Octokit.User assignee = this.GetSelectedAssignee ();

			this.issue = this.manager.CreateIssue (this.issueTitle.Buffer.Text, 
				this.issueBody.Buffer.Text, 
				assignee == null ? string.Empty : assignee.Login, 
				this.GetSelectedLabels ().ToArray (), 
				this.GetSelectedMilestone ());
		}

		#endregion
	}
}