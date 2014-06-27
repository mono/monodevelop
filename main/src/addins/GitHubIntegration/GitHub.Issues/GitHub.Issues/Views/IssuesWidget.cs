using System;
using System.Collections.Generic;
using Octokit;
using System.ComponentModel;
using System.Collections;

namespace GitHub.Issues.UserInterface
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class IssuesWidget : Gtk.Bin
	{
		private List<IssueColumn> columns = new List<IssueColumn> ();
		private List<Gtk.TreeViewColumn> treeColumns = new List<Gtk.TreeViewColumn> ();

		private Gtk.ListStore issueListStore;
		private Gtk.ListStore columnListStore;
		private Gtk.TreeModelFilter filter;
		private Gtk.TreeModelSort sort;

		private Gtk.TreeView issueTable;
		private Gtk.TreeView columnListView;

		private Gtk.Button updateIssueListButton;

		private List<IssueNode> issues;

		// Smart ListStore property
		public Gtk.ListStore ListStore
		{
			get {
				if (issueListStore == null) {
					issueListStore = new Gtk.ListStore (typeof(IssueNode));
				}

				return issueListStore;
			}
		}

		// Creates the main window contents that are seen, basically the UI of the screen
		public IssuesWidget (IReadOnlyList<Octokit.Issue> issues)
		{
			this.Build ();

			// Wrap the Octokit Issues into my issues which are easier to read with reflection
			// because I can handle conditional property reads from within my properties masked
			// behind a single property which is not easy with reflection (if possible at all.)
			// Look at Assignee for example. I can do the check internally in my property.
			this.issues = new List<IssueNode>();

			foreach (Octokit.Issue issue in issues) {
				this.issues.Add (new IssueNode (issue));
			}

			// Create the tree view to hold the issues
			this.issueTable = this.createIssueTable (this.issues);
			// Create the tree view to hold column selections
			this.columnListView = this.createColumnListView ();
			// Create the button
			this.updateIssueListButton = this.createUpdateIssueListButton ();

			// Set sizing
			columnListView.SetSizeRequest (100, 500);
			issueTable.SetSizeRequest (950, 500);

			// Set up the layout
			// 2 columns on screen with first column having 2 rows
			// |      |      |
			// |______|      |
			// |      |      |
			// Something like this
			Gtk.VBox mainContainer = new Gtk.VBox ();
			Gtk.HBox tablesContainer = new Gtk.HBox ();
			Gtk.VBox columnsSelectionContainer = new Gtk.VBox ();

			columnsSelectionContainer.Add (columnListView);
			columnsSelectionContainer.Add (updateIssueListButton);

			tablesContainer.Add (columnsSelectionContainer);
			tablesContainer.Add (issueTable);

			// Add the layout to the main container
			mainContainer.Add (tablesContainer);

			// Add main container to screen/widget
			this.Add (mainContainer);

			// Display our UI
			this.ShowAll ();
		}

		#region Event Handlers

		// Called when the button to update columns is clicked
		private void updateIssueListButtonClicked(object sender, EventArgs e)
		{
			// Clear the existing columns
			this.deleteColumnsFromTreeView (this.issueTable, this.treeColumns);

			// Find and save the selected columns
			this.columns = this.getIssueColumnsFromSelectedColumns (this.getSelectedColumns (this.columnListStore));

			// Reconfigure the issue tree view to show the selected columns
			this.treeColumns = this.createAndAppendColumnsToTreeView (this.issueTable, this.columns);

			// TODO: Set up filtering for the given columns
		}

		// When a toggle is clicked on the toggle cell renderer, this method handles setting the new value of the toggle
		private void toggleRenderedToggledHandlerColumnList(object sender, Gtk.ToggledArgs args)
		{
			Gtk.TreeIter iterator;

			// Try and find the clicked item in the column list store
			if (this.columnListStore.GetIter(out iterator, new Gtk.TreePath(args.Path)))
			{
				// Get the current value and set the value to the opposite
				bool oldToggleValue = (bool)this.columnListStore.GetValue (iterator, 2);
				this.columnListStore.SetValue (iterator, 2, !oldToggleValue);
			}
		}

		// Handles the text box updates in the column list store. For filter specification for example
		private void textRendererEditedHandlerColumnList(object sender, Gtk.EditedArgs args)
		{
			Gtk.TreeIter iterator;

			// Try and find the edited item in the column list store
			if (this.columnListStore.GetIter(out iterator, new Gtk.TreePath(args.Path)))
			{
				// Update the new filter text
				this.columnListStore.SetValue (iterator, 3, args.NewText);
			}
		}

		#endregion

		#region Creation of UI Components

		// Creates the main issue view tree view, sets up columns, handlers, sorting, filtering and populates the data
		private Gtk.TreeView createIssueTable(List<IssueNode> issues)
		{
			// Setting up the table
			Gtk.TreeView treeView = new Gtk.TreeView ();

			// Fill up the table and create sort and filter models
			this.issueListStore = this.createIssuesListStore (this.getAllPossibleColumns ());

			// Set up the columns in the TreeView for issues
			// Needs to be set up before creating the filter and sort based
			// on this store. Getting a StackOverflowException otherwise
			this.populateIssuesTable (issues, this.issueListStore);

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
			this.treeColumns = this.createAndAppendColumnsToTreeView (treeView, this.columns);

			// Assign the store so that it can read the issue rows and display them
			treeView.Model = this.sort;

			return treeView;
		}

		// Creates and returns an instance of the list view which allows the users to select columns they'd like to see
		private Gtk.TreeView createColumnListView()
		{
			// Set up the control panel for column selection
			this.columnListStore = new Gtk.ListStore (typeof(String), typeof(String), typeof(Boolean), typeof(String));

			// Add all properties into the list store that appear in IssueNode class
			// The property names won't show, the Description attribute value will be displayed instead
			this.populatePropertiesIntoListStore (this.columnListStore, typeof(IssueNode));

			// Create the list instance 
			Gtk.TreeView columnListView = new Gtk.TreeView ();
			this.initializeColumnListControl (columnListView);

			columnListView.Model = this.columnListStore;

			// Allow multiselect with CTRL or SHIFT held down
			// columnListView.Selection.Mode = Gtk.SelectionMode.Extended;

			return columnListView;
		}

		// Creates a button which triggers update of visible columns in the issue
		// tree view
		private Gtk.Button createUpdateIssueListButton ()
		{
			Gtk.Button button = new Gtk.Button ();
			button.Label = "Update Issues";

			// Method which handles the udpate of visible columns
			button.Clicked += this.updateIssueListButtonClicked;

			return button;
		}

		#endregion

		#region Sorting and Filtering functions

		// Allows us to sort the columns based on the string provided for each column in the column selection tree view
		private void SetFilteringHandlers (List<Gtk.TreeViewColumn> columns, Gtk.TreeModelFilter sort)
		{
			// Set the visibility function for filtering
			sort.VisibleFunc = this.treeFilterVisibilityFunctionForIssues;
		}

		// Filtering function which checks if the row should be show or not by comparing against the filter
		private bool treeFilterVisibilityFunctionForIssues(Gtk.TreeModel treeModel, Gtk.TreeIter iterator)
		{
			// TODO: Write comparison code against the current filtering values specified
		}

		// Allows us to sort the columns by click on the column headers. It simply toggles the sorting from
		// ascending to descending and it only allows for sorting to be applied on a single columns at a
		// time
		private void SetColumnSortHandlers (List<Gtk.TreeViewColumn> columns, Gtk.TreeModelSort sort)
		{
			// We want to enable sorting for all column mappings in the tree view control supplied
			foreach (Gtk.TreeViewColumn column in columns) {
				// When its clicked we either set up the sorting (if currently on a different column)
				// or toggle if already on this column
				column.Clicked += (object sender, EventArgs e) => 
				{
					int colId = -1;
					Gtk.SortType currentSort = Gtk.SortType.Descending;

					// Get the column id of the current column which is sorted and the sort type of that sort
					bool sorted = sort.GetSortColumnId(out colId, out currentSort);

					// If no sorting is applied at the minute
					if (sorted == false)
					{
						// Set up myself as sort column
						sort.SetSortColumnId(column.SortColumnId, currentSort);
					}
					else
					{
						// If sorting is currently on myself
						if (colId == column.SortColumnId)
						{
							// Toggle the sorting type
							sort.ChangeSortColumn();
						}
						else
						{
							// If someone else is sorted, set up the sorting on myself with Descending (look above - currentSort)
							sort.SetSortColumnId(column.SortColumnId, currentSort);
						}
					}
				};
			}
		}

		#endregion

		#region ListStore Management Methods

		// Takes an IssueNode class and creates a row that is then put into the list store
		// IssueNode is used as a wrapper for Octokit.Issue to avoid problems where conditional
		// information fetches are required. This way we can mask it with a single property.
		private void addIssueRowIntoListStore(Gtk.ListStore listStore, IssueNode issue)
		{
			List<Object> values = new List<Object> ();

			foreach (IssueColumn column in this.columns) {
				values.Add(typeof(IssueNode).GetProperty (column.PropertyName).GetValue (issue).ToString());
			}

			listStore.AppendValues (values.ToArray());
		}

		// Creates a list store for the issues
		private Gtk.ListStore createIssuesListStore(List<KeyValuePair<String, Int32>> allColumns)
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

			foreach (IssueColumn column in this.columns) {
				columnTypes.Add (column.Type);
			}

			return new Gtk.ListStore (columnTypes.ToArray());
		}

		#endregion

		#region TreeView Column Management Methods

		// Remove all the columns in the list from the given tree view
		// Used for things like clearing all columns when user updates the 
		// columns he/she wants to see
		private void deleteColumnsFromTreeView(Gtk.TreeView treeView, List<Gtk.TreeViewColumn> columnsToRemove)
		{
			foreach (Gtk.TreeViewColumn column in columnsToRemove) {
				treeView.RemoveColumn (column);
			}
		}

		// Usually called when the columns on the treeview have been cleared already
		// Simply recreates the columns, adds them to the treeview with accurate references to
		// the columns from the list store (column.ListStoreColumnIndex - this is what allows
		// us to hide certain columns and only show selected ones)
		private List<Gtk.TreeViewColumn> createAndAppendColumnsToTreeView(Gtk.TreeView treeView, List<IssueColumn> columnsToAdd)
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

		// Initializes the columns for the list control which contains the columns available for selection
		private void initializeColumnListControl(Gtk.TreeView columnListView)
		{
			// Only show column 0 from the list store since it contains the user friendly description, leave out the second one (property name - used for back end)
			Gtk.CellRendererToggle displayToggle = new Gtk.CellRendererToggle ();
			displayToggle.Mode = Gtk.CellRendererMode.Activatable;
			displayToggle.Toggled += this.toggleRenderedToggledHandlerColumnList;

			Gtk.CellRendererText filterTextBox = new Gtk.CellRendererText ();
			filterTextBox.Mode = Gtk.CellRendererMode.Activatable;
			filterTextBox.Editable = true;
			filterTextBox.Edited += this.textRendererEditedHandlerColumnList;

			columnListView.AppendColumn (new Gtk.TreeViewColumn ("Display", displayToggle, "active", 2));
			columnListView.AppendColumn (new Gtk.TreeViewColumn ("Column Title", new Gtk.CellRendererText (), "text", 0));
			columnListView.AppendColumn (new Gtk.TreeViewColumn ("Filter", filterTextBox, "text", 3));
		}

		// Once we know the property names and the column indexes in the store, we can create the IssueColumn classes which we
		// use to populate the list store with issues. It is purely here for handiness so that we have the information in a
		// single instance instead of having to call the same thing all over the place.
		private List<IssueColumn> getIssueColumnsFromSelectedColumns(List<KeyValuePair<String, Int32>> selectedColumns)
		{
			List<IssueColumn> issueColumns = new List<IssueColumn> ();

			foreach (KeyValuePair<String, Int32> column in selectedColumns) {
				String columnTitle = ((DescriptionAttribute)Attribute.GetCustomAttribute (typeof(IssueNode).GetProperty (column.Key), typeof(DescriptionAttribute))).Description;
				issueColumns.Add(new IssueColumn(typeof(String), columnTitle, column.Key, column.Value));
			}

			return issueColumns;
		}

		#endregion

		#region Tree View Data Management Methods - Reading for Trees, Adding Rows etc.

		// Expects a tree view which contains the multiple selections of the columns that we want
		// to see in the issue table for example. It checks what is selected and extracts the names
		// along with the indices of the selections and composes them into a list for later use.
		private List<KeyValuePair<String, Int32>> getSelectedColumns(Gtk.ListStore columnsListStore)
		{
			if (columnsListStore != null) {
				IEnumerator rowEnumerator = columnListStore.GetEnumerator ();

				// Here we will store string representations of the selected column names
				List<KeyValuePair<String, Int32>> selectedColumns = new List<KeyValuePair<String, Int32>> ();

				int currentRowCount = 0;

				while (rowEnumerator.MoveNext ()) {
					// 0 - Title, 1 - Property, 2 - Display?
					Array currentRow = (Array)rowEnumerator.Current;

					// Get column property (not show - not the user friendly name) and the row index
					if ((bool)currentRow.GetValue(2) == true)
					{
						selectedColumns.Add (new KeyValuePair<string, int> ((String)currentRow.GetValue(1), currentRowCount));
					}

					// Keep track of column indexes - used for correct mapping to columns in store
					currentRowCount++;
				}

				return selectedColumns;
			}

			return new List<KeyValuePair<String, Int32>>();
		}
			
		// Populates the list store with each value of the given enum type. Mainly used to
		// populate all column types from the properties into the list store which is then used
		// to display them in a table so the user can then select the columns they'd like
		// to see
		private void populatePropertiesIntoListStore(Gtk.ListStore list, Type classType)
		{
			foreach (System.Reflection.PropertyInfo property in classType.GetProperties())
			{
				// Use the reflection magic to extract the information that we need ie. Property to read for value and Description to use as column name (user friendliness)
				String propertyName = property.Name;
				String description = ((DescriptionAttribute)Attribute.GetCustomAttribute (classType.GetProperty (propertyName), typeof(DescriptionAttribute))).Description;

				// Add to list store
				list.AppendValues (description, propertyName, true, string.Empty);
				// True because we assume that the user wants to see that column by default
				// string.Empty because we assume that the user doesn't want any filter on by default
			}
		}

		// Gets all possible column types and returns them in a list along with their respective indexes in the list store
		private List<KeyValuePair<String, Int32>> getAllPossibleColumns()
		{
			List<KeyValuePair<String, Int32>> columns = new List<KeyValuePair<string, int>> ();

			int columnCount = 0;

			// Add all properties as columns
			foreach (System.Reflection.PropertyInfo property in typeof(IssueNode).GetProperties()) {
				columns.Add (new KeyValuePair<string, int> (property.Name, columnCount++));
			}

			return columns;
		}

		// Adds all issues as rows into the given table
		private void populateIssuesTable(List<IssueNode> issues, Gtk.ListStore issueListStore)
		{
			// Add all issues as rows into the list store
			foreach (IssueNode issue in issues) {
				this.addIssueRowIntoListStore (issueListStore, issue);
			}
		}

		#endregion
	}
}