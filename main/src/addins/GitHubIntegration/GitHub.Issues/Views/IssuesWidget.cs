using System;
using System.Collections.Generic;
using Octokit;
using System.ComponentModel;
using System.Collections;

namespace GitHub.Issues
{
	/// <summary>
	/// Issues widget - displays the list of issues for the current working repository
	/// Allows to filter and select the visible columns and to go into other management screens
	/// </summary>
	[System.ComponentModel.ToolboxItem (true)]
	public partial class IssuesWidget : Gtk.Bin
	{
		private List<IssueColumn> columns = new List<IssueColumn> ();
		private List<Gtk.TreeViewColumn> treeColumns = new List<Gtk.TreeViewColumn> ();
		private List<KeyValuePair<String, Int32>> filters = new List<KeyValuePair<string, int>> ();

		private Gtk.ListStore issueListStore;
		private Gtk.ListStore columnListStore;
		private Gtk.TreeModelFilter filter;
		private Gtk.TreeModelSort sort;

		private Gtk.TreeView issueTable;
		private Gtk.TreeView columnListView;

		private Gtk.Button updateIssueListButton;
		private Gtk.Button createNewIssueButton;
		private Gtk.Button manageLabelsButton;

		private List<IssueNode> issues;

		private Octokit.Issue oldSelectedIssue;

		private CommonControlsFactories commonControlsFactory;

		#region Events

		public EventHandler<IssueSelectedEventArgs> IssueSelected;

		public EventHandler CreateNewIssueClicked;
		public EventHandler ManageLabelsButtonClicked;

		#endregion

		/// <summary>
		/// Smart ListStore property
		/// </summary>
		/// <value>The list store.</value>
		public Gtk.ListStore ListStore {
			get {
				if (issueListStore == null) {
					issueListStore = new Gtk.ListStore (typeof(IssueNode));
				}

				return issueListStore;
			}
		}

		/// <summary>
		/// Creates the main window contents that are seen, basically the UI of the screen
		/// </summary>
		/// <param name="issues">Issues.</param>
		public IssuesWidget (IReadOnlyList<Octokit.Issue> issues)
		{
			this.Build ();

			this.commonControlsFactory = new CommonControlsFactories ();

			// Wrap the Octokit Issues into my issues which are easier to read with reflection
			// because I can handle conditional property reads from within my properties masked
			// behind a single property which is not easy with reflection (if possible at all.)
			// Look at Assignee for example. I can do the check internally in my property.
			this.issues = new List<IssueNode> ();

			if (issues != null) {
				foreach (Octokit.Issue issue in issues) {
					this.issues.Add (new IssueNode (issue));
				}
			}

			// Create the tree view to hold the issues
			this.issueTable = this.CreateIssueTable (this.issues);
			// Create the tree view to hold column selections
			this.columnListView = this.CreateColumnListView ();
			// Create the button
			this.updateIssueListButton = this.CreateUpdateIssueListButton ();
			this.createNewIssueButton = this.CreateCreateIssueButton ();
			this.manageLabelsButton = this.CreateManageLabelsButton ();

			// Set sizing
			// this.columnListView.SetSizeRequest (100, 600);
			//this.issueTable.SetSizeRequest (950, 600);
			//this.updateIssueListButton.SetSizeRequest (100, 50);

			// Set up the layout
			// 2 columns on screen with first column having 2 rows
			// |      |      |
			// |______|      |
			// |      |      |
			// Something like this

			bool expand = false;
			bool fill = false;
			uint padding = 0;

			Gtk.VBox mainContainer = new Gtk.VBox (false, 0);
			Gtk.HBox headerContainer = new Gtk.HBox ();
			Gtk.HBox tablesContainer = new Gtk.HBox ();
			Gtk.VBox columnsSelectionContainer = new Gtk.VBox ();

			headerContainer.Add (LayoutUtilities.SetPadding(updateIssueListButton, 3, 3, 3, 0));
			headerContainer.Add (LayoutUtilities.SetPadding(createNewIssueButton, 3, 3, 0, 0));
			headerContainer.Add (LayoutUtilities.SetPadding(manageLabelsButton, 3, 3, 0, 0));

			Gtk.EventBox headerEventBox = new Gtk.EventBox ();
			headerEventBox.Add (LayoutUtilities.LeftAlign (headerContainer));
			headerEventBox.ModifyBg (Gtk.StateType.Normal, ThemeColors.HeaderBarColor);

			columnsSelectionContainer.PackStart (columnListView, true, true, padding);

			tablesContainer.PackStart (columnsSelectionContainer, expand, fill, padding);

			tablesContainer.PackStart (LayoutUtilities.SetPadding(LayoutUtilities.StretchXAlign(issueTable), 0, 0, 10, 0), true, true, 0);

			// Add the layout to the main container
			mainContainer.PackStart (headerEventBox, false, false, 0);
			mainContainer.PackStart (tablesContainer, true, true, padding);

			// Add main container to screen/widget
			this.Add (mainContainer);

			// Display our UI
			this.ShowAll ();
		}

		#region Event Handlers

		/// <summary>
		/// Called when the create issue button is clicked
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		private void CreateIssueButtonClicked (object sender, EventArgs e)
		{
			this.CreateNewIssueClicked (this, null);
		}

		/// <summary>
		/// Called when the manage labels button is clicked
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		private void ManageLabelsButtonClickedHandler (object sender, EventArgs e)
		{
			this.ManageLabelsButtonClicked (this, null);
		}

		/// <summary>
		/// Called when the button to update columns is clicked
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">Events from click</param>
		private void UpdateIssueListButtonClickedHandler (object sender, EventArgs e)
		{
			// Clear the existing columns
			this.DeleteColumnsFromTreeView (this.issueTable, this.treeColumns);

			// Find and save the selected columns
			this.columns = this.GetIssueColumnsFromSelectedColumns (this.GetSelectedColumns (this.columnListStore));

			// Reconfigure the issue tree view to show the selected columns
			this.treeColumns = this.CreateAndAppendColumnsToTreeView (this.issueTable, this.columns);

			// Retrieve the filter values which can be used to search the issue list store
			this.filters = this.GetFiltersForColumns (this.columnListStore);

			// Update the table with the new filter values (regardless if updated or not)
			this.filter.Refilter ();
		}

		/// <summary>
		/// When a toggle is clicked on the toggle cell renderer, this method handles setting the new value of the toggle
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="args">Arguments.</param>
		private void ToggleRenderedToggledHandlerColumnList (object sender, Gtk.ToggledArgs args)
		{
			TreeViewUtilities.ToggleCheckBoxRenderer (this.columnListStore, 2, args);
		}

		/// <summary>
		/// Handles the text box updates in the column list store. For filter specification for example
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="args">Arguments.</param>
		private void TextRendererEditedHandlerColumnList (object sender, Gtk.EditedArgs args)
		{
			Gtk.TreeIter iterator;

			// Try and find the edited item in the column list store
			if (this.columnListStore.GetIter (out iterator, new Gtk.TreePath (args.Path))) {
				// Update the new filter text
				this.columnListStore.SetValue (iterator, 3, args.NewText);
			}
		}

		#endregion

		#region Creation of UI Components

		/// <summary>
		/// Creates the main issue view tree view, sets up columns, handlers, sorting, filtering and populates the data
		/// </summary>
		/// <returns>The table to populate</returns>
		/// <param name="issues">Issues to insert into the table</param>
		private Gtk.TreeView CreateIssueTable (List<IssueNode> issues)
		{
			// Setting up the table
			Gtk.TreeView treeView = new Gtk.TreeView ();

			// Fill up the table and create sort and filter models
			this.issueListStore = this.CreateIssuesListStore (this.GetAllPossibleColumns ());

			// Set up the columns in the TreeView for issues
			// Needs to be set up before creating the filter and sort based
			// on this store. Getting a StackOverflowException otherwise
			this.PopulateIssuesTable (issues, this.issueListStore);

			// Set up the filtering and sorting models based on data
			this.filter = new Gtk.TreeModelFilter (this.issueListStore, null);
			this.sort = new Gtk.TreeModelSort (this.filter);

			// Set up sorting and filtering methods
			this.SetColumnSortHandlers (this.treeColumns, this.sort);
			this.SetFilteringHandlers (this.treeColumns, this.filter);

			// Now since we have the data loaded into the store and we know which
			// columns we want to display (all by default) we add those columns mappings
			// to the control itself so that it can display them from the list store
			// 
			// Also need to save the output of this method for later in case we need to
			// remove those column mappings (it needs those exact instances... stupid.)
			this.treeColumns = this.CreateAndAppendColumnsToTreeView (treeView, this.columns);

			// Assign the store so that it can read the issue rows and display them
			treeView.Model = this.sort;

			// When the selection changes we need to locate the correct Octokit.Issue instance and 
			// pass it out with the event to whoever is interested in it
			treeView.CursorChanged += (object sender, EventArgs e) => {
				treeView.Selection.SelectedForeach (new Gtk.TreeSelectionForeachFunc ((Gtk.TreeModel model, Gtk.TreePath path, Gtk.TreeIter iter) => {
					Octokit.Issue selectedIssue = (Octokit.Issue)model.GetValue (iter, 0);

					if (this.IssueSelected != null) {
						this.IssueSelected.Invoke (this, new IssueSelectedEventArgs () {
							OldSelectedIssue = this.oldSelectedIssue,
							SelectedIssue = selectedIssue
						});
					}

					this.oldSelectedIssue = selectedIssue;
				}));
			};

			// Show horizontal separators between the rows
			treeView.EnableGridLines = Gtk.TreeViewGridLines.Horizontal;

			return treeView;
		}

		/// <summary>
		/// Creates and returns an instance of the list view which allows the users to select columns they'd like to see
		/// </summary>
		/// <returns>The column list view.</returns>
		private Gtk.TreeView CreateColumnListView ()
		{
			// Set up the control panel for column selection
			this.columnListStore = new Gtk.ListStore (typeof(String), typeof(String), typeof(Boolean), typeof(String));

			// Add all properties into the list store that appear in IssueNode class
			// The property names won't show, the Description attribute value will be displayed instead
			this.PopulatePropertiesIntoListStore (this.columnListStore, typeof(IssueNode));

			// Create the list instance 
			Gtk.TreeView columnListView = new Gtk.TreeView ();
			this.InitializeColumnListControl (columnListView);

			columnListView.Model = this.columnListStore;

			// Allow multiselect with CTRL or SHIFT held down
			// columnListView.Selection.Mode = Gtk.SelectionMode.Extended;

			return columnListView;
		}

		/// <summary>
		/// Creates a button which triggers update of visible columns in the issue tree view
		/// </summary>
		/// <returns>The "update issue list" button.</returns>
		private Gtk.Button CreateUpdateIssueListButton ()
		{
			return this.commonControlsFactory.CreateButton (StringResources.Update, this.UpdateIssueListButtonClickedHandler);
		}

		/// <summary>
		/// Creates a button which allows the creation of new issues
		/// </summary>
		/// <returns>The create issue button.</returns>
		private Gtk.Button CreateCreateIssueButton ()
		{
			return this.commonControlsFactory.CreateButton (StringResources.CreateNewIssue, this.CreateIssueButtonClicked);
		}

		/// <summary>
		/// Creates the manage labels button.
		/// </summary>
		/// <returns>The manage labels button.</returns>
		private Gtk.Button CreateManageLabelsButton ()
		{
			return this.commonControlsFactory.CreateButton (StringResources.ManageLabels, this.ManageLabelsButtonClickedHandler);
		}

		#endregion

		#region Sorting and Filtering functions

		/// <summary>
		/// Allows us to sort the columns based on the string provided for each column in the column selection tree view
		/// </summary>
		/// <param name="columns">Columns to set filters on</param>
		/// <param name="sort">Sort model which we are applying the filtering for</param>
		private void SetFilteringHandlers (List<Gtk.TreeViewColumn> columns, Gtk.TreeModelFilter sort)
		{
			// Set the visibility function for filtering
			sort.VisibleFunc = this.TreeFilterVisibilityFunctionForIssues;
		}

		/// <summary>
		/// Filtering function which checks if the row should be show or not by comparing against the filter
		/// </summary>
		/// <returns><c>true</c>, if filter visibility function for issues was treed, <c>false</c> otherwise.</returns>
		/// <param name="treeModel">Tree model.</param>
		/// <param name="iterator">Iterator.</param>
		private bool TreeFilterVisibilityFunctionForIssues (Gtk.TreeModel treeModel, Gtk.TreeIter iterator)
		{
			// Need to compare all values of all columns against the filters (we also accept filters against columns which are not shown)
			foreach (KeyValuePair<String, Int32> filter in this.filters) {
				// First we need to know what is show in that given column of that given row
				String columnValue = (String)this.issueListStore.GetValue (iterator, filter.Value);

				// Check if the value should be shown after comparing to the filter
				// If the check fails don't show the row at all and stop comparing against other columns
				if (!columnValue.StartsWith (filter.Key.Trim ())) {
					return false;
				}
			}
				
			// It hasn't failed by now so the row is okay to show
			return true;
		}

		/// <summary>
		/// Allows us to sort the columns by click on the column headers. It simply toggles the sorting from
		/// ascending to descending and it only allows for sorting to be applied on a single columns at a
		/// time
		/// </summary>
		/// <param name="columns">Columns to allow sorting on</param>
		/// <param name="sort">Sort model to which we apply sorting handlers to</param>
		private void SetColumnSortHandlers (List<Gtk.TreeViewColumn> columns, Gtk.TreeModelSort sort)
		{
			// We want to enable sorting for all column mappings in the tree view control supplied
			foreach (Gtk.TreeViewColumn column in columns) {
				// When its clicked we either set up the sorting (if currently on a different column)
				// or toggle if already on this column
				column.Clicked += (object sender, EventArgs e) => {
					int colId = -1;
					Gtk.SortType currentSort = Gtk.SortType.Descending;

					// Get the column id of the current column which is sorted and the sort type of that sort
					bool sorted = sort.GetSortColumnId (out colId, out currentSort);

					// If no sorting is applied at the minute
					if (sorted == false) {
						// Set up myself as sort column
						sort.SetSortColumnId (column.SortColumnId, currentSort);
					} else {
						// If sorting is currently on myself
						if (colId == column.SortColumnId) {
							// Toggle the sorting type
							sort.ChangeSortColumn ();
						} else {
							// If someone else is sorted, set up the sorting on myself with Descending (look above - currentSort)
							sort.SetSortColumnId (column.SortColumnId, currentSort);
						}
					}
				};
			}
		}

		#endregion

		#region ListStore Management Methods

		/// <summary>
		/// Takes an IssueNode class and creates a row that is then put into the list store
		/// IssueNode is used as a wrapper for Octokit.Issue to avoid problems where conditional
		/// information fetches are required. This way we can mask it with a single property.
		/// </summary>
		/// <param name="listStore">List store</param>
		/// <param name="issue">Issue to add into the row</param>
		private void AddIssueRowIntoListStore (Gtk.ListStore listStore, IssueNode issue)
		{
			List<Object> values = new List<Object> ();

			// Store a reference to the issue itself
			values.Add (issue.Issue);

			foreach (IssueColumn column in this.columns) {
				object value = typeof(IssueNode).GetProperty (column.PropertyName).GetValue (issue);
				values.Add (value != null ? value.ToString () : string.Empty);
			}

			listStore.AppendValues (values.ToArray ());
		}

		/// <summary>
		/// Creates a list store for the issues
		/// </summary>
		/// <returns>The issues list store.</returns>
		/// <param name="allColumns">All columns.</param>
		private Gtk.ListStore CreateIssuesListStore (List<KeyValuePair<String, Int32>> allColumns)
		{
			this.columns.Clear ();

			// Need to get the property names of the columns based on enum from the string list
			// Each Key is the column name, each Value is the index of the column - used later to specify
			// what we want to see from the ListStore
			foreach (KeyValuePair<String, Int32> column in allColumns) {
				// Get the property name where to read the data from
				DescriptionAttribute descriptionAttribute = (DescriptionAttribute)Attribute.GetCustomAttribute (typeof(IssueNode).GetProperty (column.Key), typeof(DescriptionAttribute));
				// Create a IssueColumn based on the information we know
				this.columns.Add (new IssueColumn (typeof(String), descriptionAttribute.Description, column.Key, column.Value));
			}

			// Need create the list store now
			List<Type> columnTypes = new List<Type> ();

			// Add a column to store a reference to the Octokit.Issue for that row (in index 0)
			columnTypes.Add (typeof(Octokit.Issue));

			foreach (IssueColumn column in this.columns) {
				columnTypes.Add (column.Type);
			}

			return new Gtk.ListStore (columnTypes.ToArray ());
		}

		#endregion

		#region TreeView Column Management Methods

		/// <summary>
		/// Remove all the columns in the list from the given tree view
		/// Used for things like clearing all columns when user updates the 
		/// columns he/she wants to see
		/// </summary>
		/// <param name="treeView">Tree view to remove columns from.</param>
		/// <param name="columnsToRemove">Columns to remove.</param>
		private void DeleteColumnsFromTreeView (Gtk.TreeView treeView, List<Gtk.TreeViewColumn> columnsToRemove)
		{
			foreach (Gtk.TreeViewColumn column in columnsToRemove) {
				treeView.RemoveColumn (column);
			}
		}

		/// <summary>
		/// Usually called when the columns on the treeview have been cleared already
		/// Simply recreates the columns, adds them to the treeview with accurate references to
		/// the columns from the list store (column.ListStoreColumnIndex - this is what allows
		/// us to hide certain columns and only show selected ones)
		/// </summary>
		/// <returns>The and append columns to tree view.</returns>
		/// <param name="treeView">Tree view to add to</param>
		/// <param name="columnsToAdd">Columns to add</param>
		private List<Gtk.TreeViewColumn> CreateAndAppendColumnsToTreeView (Gtk.TreeView treeView, List<IssueColumn> columnsToAdd)
		{
			List<Gtk.TreeViewColumn> columns = new List<Gtk.TreeViewColumn> ();

			foreach (IssueColumn column in columnsToAdd) {
				columns.Add (treeView.AppendColumn (column.Title, new Gtk.CellRendererText (), "text", column.ListStoreColumnIndex));
				columns [columns.Count - 1].SortColumnId = column.ListStoreColumnIndex;
			}

			foreach (Gtk.TreeViewColumn column in columns) {
				column.Resizable = true;
				column.Reorderable = true;
				column.Sizing = Gtk.TreeViewColumnSizing.Autosize;
			}

			return columns;
		}

		/// <summary>
		/// Initializes the columns for the list control which contains the columns available for selection
		/// </summary>
		/// <param name="columnListView">Column list view.</param>
		private void InitializeColumnListControl (Gtk.TreeView columnListView)
		{
			// Only show column 0 from the list store since it contains the user friendly description, leave out the second one (property name - used for back end)
			Gtk.CellRendererToggle displayToggle = new Gtk.CellRendererToggle ();
			displayToggle.Mode = Gtk.CellRendererMode.Activatable;
			displayToggle.Toggled += this.ToggleRenderedToggledHandlerColumnList;

			Gtk.CellRendererText filterTextBox = new Gtk.CellRendererText ();
			filterTextBox.Mode = Gtk.CellRendererMode.Activatable;
			filterTextBox.Editable = true;
			filterTextBox.Edited += this.TextRendererEditedHandlerColumnList;
			filterTextBox.SingleParagraphMode = true;

			Gtk.TreeViewColumn displayColumn = new Gtk.TreeViewColumn ("Display", displayToggle, "active", 2);
			Gtk.TreeViewColumn titleColumn = new Gtk.TreeViewColumn ("Column Title", new Gtk.CellRendererText (), "text", 0);
			Gtk.TreeViewColumn filterColumn = new Gtk.TreeViewColumn ("Filter", filterTextBox, "text", 3);

			// Give it some extra room at the start when the filters are all empty
			filterColumn.MinWidth = 150;

			// To prevent it from overwhelming the issues table
			filterColumn.Sizing = Gtk.TreeViewColumnSizing.Fixed;

			columnListView.AppendColumn (displayColumn);
			columnListView.AppendColumn (titleColumn);
			columnListView.AppendColumn (filterColumn);
		}

		/// <summary>
		/// Once we know the property names and the column indexes in the store, we can create the IssueColumn classes which we
		/// use to populate the list store with issues. It is purely here for handiness so that we have the information in a
		/// single instance instead of having to call the same thing all over the place.
		/// </summary>
		/// <returns>The issue columns from selected columns.</returns>
		/// <param name="selectedColumns">Selected columns.</param>
		private List<IssueColumn> GetIssueColumnsFromSelectedColumns (List<KeyValuePair<String, Int32>> selectedColumns)
		{
			List<IssueColumn> issueColumns = new List<IssueColumn> ();

			foreach (KeyValuePair<String, Int32> column in selectedColumns) {
				String columnTitle = ((DescriptionAttribute)Attribute.GetCustomAttribute (typeof(IssueNode).GetProperty (column.Key), typeof(DescriptionAttribute))).Description;
				issueColumns.Add (new IssueColumn (typeof(String), columnTitle, column.Key, column.Value));
			}

			return issueColumns;
		}

		#endregion

		#region Tree View Data Management Methods - Reading for Trees, Adding Rows etc.

		/// <summary>
		/// Expects a tree view which contains the multiple selections of the columns that we want
		/// to see in the issue table for example. It checks what is selected and extracts the names
		/// along with the indices of the selections and composes them into a list for later use.
		/// </summary>
		/// <returns>The selected columns</returns>
		/// <param name="columnsListStore">Columns list store which contains the selected columns</param>
		private List<KeyValuePair<String, Int32>> GetSelectedColumns (Gtk.ListStore columnsListStore)
		{
			if (columnsListStore != null) {
				IEnumerator rowEnumerator = columnsListStore.GetEnumerator ();

				// Here we will store string representations of the selected column names
				List<KeyValuePair<String, Int32>> selectedColumns = new List<KeyValuePair<String, Int32>> ();

				// 0 contains the Octokit.Issue reference, 1 and above are all details
				int currentRowCount = 1;

				while (rowEnumerator.MoveNext ()) {
					// 0 - Title, 1 - Property, 2 - Display?, 3 - Filter Value
					Array currentRow = (Array)rowEnumerator.Current;

					// Get column property (not title - not the user friendly name) and the row index
					if ((bool)currentRow.GetValue (2) == true) {
						selectedColumns.Add (new KeyValuePair<string, int> ((String)currentRow.GetValue (1), currentRowCount));
					}

					// Keep track of column indexes - used for correct mapping to columns in store
					currentRowCount++;
				}

				return selectedColumns;
			}

			return new List<KeyValuePair<String, Int32>> ();
		}

		/// <summary>
		/// Finds the column filter values from the table which can then be used to filter the issues from the tree view
		/// </summary>
		/// <returns>The filters for columns</returns>
		/// <param name="columnsListStore">Columns list store</param>
		private List<KeyValuePair<String, Int32>> GetFiltersForColumns (Gtk.ListStore columnsListStore)
		{
			if (columnsListStore != null) {
				IEnumerator rowEnumerator = columnsListStore.GetEnumerator ();

				List<KeyValuePair<String, Int32>> columnsIndexesAndFilterValues = new List<KeyValuePair<String, Int32>> ();

				int currentRowCount = 1;

				while (rowEnumerator.MoveNext ()) {
					Array currentRow = (Array)rowEnumerator.Current;

					// Get column filter value and the row index
					// 0 - Title, 1 - Property, 2 - Display?, 3 - Filter Value
					columnsIndexesAndFilterValues.Add (new KeyValuePair<string, int> ((String)currentRow.GetValue (3), currentRowCount));

					// Keep track of column indexes - used for correct mapping to columns in store
					currentRowCount++;
				}

				return columnsIndexesAndFilterValues;
			}

			return new List<KeyValuePair<string, int>> ();
		}

		/// <summary>
		/// Populates the list store with each value of the given enum type. Mainly used to
		/// populate all column types from the properties into the list store which is then used
		/// to display them in a table so the user can then select the columns they'd like
		/// to see
		/// </summary>
		/// <param name="list">List</param>
		/// <param name="classType">Class type</param>
		private void PopulatePropertiesIntoListStore (Gtk.ListStore list, Type classType)
		{
			foreach (System.Reflection.PropertyInfo property in classType.GetProperties()) {
				// Use the reflection magic to extract the information that we need ie. Property to read for value and Description to use as column name (user friendliness)
				String propertyName = property.Name;
				String description = ((DescriptionAttribute)Attribute.GetCustomAttribute (classType.GetProperty (propertyName), typeof(DescriptionAttribute))).Description;

				// Add to list store
				list.AppendValues (description, propertyName, true, string.Empty);
				// True because we assume that the user wants to see that column by default
				// string.Empty because we assume that the user doesn't want any filter on by default
			}
		}

		/// <summary>
		/// Gets all possible column types and returns them in a list along with their respective indexes in the list store
		/// </summary>
		/// <returns>The all possible columns</returns>
		private List<KeyValuePair<String, Int32>> GetAllPossibleColumns ()
		{
			List<KeyValuePair<String, Int32>> columns = new List<KeyValuePair<string, int>> ();

			// 0 stores the reference of the Octokit.Issue, details start at 1
			int columnCount = 1;

			// Add all properties as columns
			foreach (System.Reflection.PropertyInfo property in typeof(IssueNode).GetProperties()) {
				columns.Add (new KeyValuePair<string, int> (property.Name, columnCount++));
			}

			return columns;
		}

		/// <summary>
		/// Adds all issues as rows into the given table
		/// </summary>
		/// <param name="issues">Issues to add to the table/param>
		/// <param name="issueListStore">Issue list store of the table</param>
		private void PopulateIssuesTable (List<IssueNode> issues, Gtk.ListStore issueListStore)
		{
			if (issues != null) {
				// Add all issues as rows into the list store
				foreach (IssueNode issue in issues) {
					this.AddIssueRowIntoListStore (issueListStore, issue);
				}
			}
		}

		#endregion
	}
}