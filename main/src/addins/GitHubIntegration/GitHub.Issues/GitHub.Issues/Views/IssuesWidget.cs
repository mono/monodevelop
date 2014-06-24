using System;
using System.Collections.Generic;
using Octokit;
using System.ComponentModel;

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

		private IReadOnlyList<Octokit.Issue> issues;

		public Gtk.ListStore ListStore
		{
			get {
				if (issueListStore == null) {
					issueListStore = new Gtk.ListStore (typeof(IssueNode));
				}

				return issueListStore;
			}
		}

		public IssuesWidget (IReadOnlyList<Octokit.Issue> issues)
		{
			this.Build ();

			this.issues = issues;

			this.issueTable = this.createIssueTable (this.issues, false);
			this.columnListView = this.createColumnListView ();
			this.updateIssueListButton = this.createUpdateIssueListButton ();

			// Set sizing
			columnListView.SetSizeRequest (100, 500);
			issueTable.SetSizeRequest (950, 500);

			// Add controls to the widget
			Gtk.VBox mainContainer = new Gtk.VBox ();
			Gtk.HBox tablesContainer = new Gtk.HBox ();
			Gtk.VBox columnsSelectionContainer = new Gtk.VBox ();

			columnsSelectionContainer.Add (columnListView);
			columnsSelectionContainer.Add (updateIssueListButton);

			tablesContainer.Add (columnsSelectionContainer);
			tablesContainer.Add (issueTable);

			mainContainer.Add (tablesContainer);

			this.Add (mainContainer);

			this.ShowAll ();
		}

		private Gtk.Button createUpdateIssueListButton ()
		{
			Gtk.Button button = new Gtk.Button ();
			button.Label = "Update Issues";
			button.Clicked += this.updateIssueListButtonClicked;

			return button;
		}

		private void updateIssueListButtonClicked(object sender, EventArgs e)
		{
			this.populateIssuesTableAndSetUpSortAndFilter ();

			this.issueTable.Model = this.sort;
		}

		private Gtk.TreeView createIssueTable(IReadOnlyList<Octokit.Issue> issues, bool populate)
		{
			// Setting up the table
			Gtk.TreeView treeView = new Gtk.TreeView ();

			this.treeColumns = this.createAndConfigureColumns (treeView);

			// Populate the table
			if (populate) {
				this.populateIssuesTableAndSetUpSortAndFilter ();

				treeView.Model = this.sort;
			}

			return treeView;
		}

		private void populateIssuesTableAndSetUpSortAndFilter()
		{
			this.issueListStore = this.createListStore ();

			foreach (Octokit.Issue issue in issues) {
				this.addRowBasedOnIssue (this.issueListStore, issue);
			}

			this.filter = new Gtk.TreeModelFilter (this.issueListStore, null);
			this.sort = new Gtk.TreeModelSort (this.filter);

			// Add sorting and filtering functionality
			this.SetColumnSortHandlers (this.treeColumns, this.sort);

			this.SetFilteringHandlers (this.treeColumns, this.filter);
		}

		private Gtk.TreeView createColumnListView()
		{
			// Set up the control panel for column selection
			this.columnListStore = new Gtk.ListStore (typeof(String));

			this.CreateAndAddColumnSelectionsToTable (this.columnListStore, typeof(IssueProperties));

			Gtk.TreeView columnListView = new Gtk.TreeView ();
			this.createColumnListWidget (columnListView);
			columnListView.Model = this.columnListStore;

			columnListView.Selection.Mode = Gtk.SelectionMode.Extended;

			return columnListView;
		}

		private void createColumnListWidget(Gtk.TreeView columnListView)
		{
			columnListView.AppendColumn (new Gtk.TreeViewColumn ("Column Title", new Gtk.CellRendererText (), "text", 0));
		}

		private void SetFilteringHandlers (List<Gtk.TreeViewColumn> columns, Gtk.TreeModelFilter sort)
		{
		}

		private void SetColumnSortHandlers (List<Gtk.TreeViewColumn> columns, Gtk.TreeModelSort sort)
		{
			foreach (Gtk.TreeViewColumn column in columns) {
				column.Clicked += (object sender, EventArgs e) => 
				{
					int colId = -1;
					Gtk.SortType currentSort = Gtk.SortType.Descending;

					bool sorted = sort.GetSortColumnId(out colId, out currentSort);

					if (sorted == false)
					{
						sort.SetSortColumnId(column.SortColumnId, currentSort);
					}
					else
					{
						if (colId == column.SortColumnId)
						{
							sort.ChangeSortColumn();
						}
						else
						{
							sort.SetSortColumnId(column.SortColumnId, currentSort);
						}
					}
				};
			}
		}

		private void addRowBasedOnIssue(Gtk.ListStore listStore, Octokit.Issue issue)
		{
			object[] values = new object[this.columns.Count];

			foreach (IssueColumn column in this.columns) {
				try {
					values [column.OrderFromLeftIndex] = issue.GetType ().GetProperty (column.PropertyName).GetValue (issue).ToString();
				}
				catch (NullReferenceException) {
					values [column.OrderFromLeftIndex] = "Not Available";
				}
			}

			listStore.AppendValues (values);
		}

		private void CreateAndAddColumnSelectionsToTable(Gtk.ListStore list, Type enumType)
		{
			foreach (string enumValue in Enum.GetNames (enumType))
			{
				// Create a checkbox, store it in an array so that we can quickly go through them and get a list of columns to populate
				// Add the checkbox to the table.
				// Have a VBox of checkboxes which can be collapsed.
				// Or have a multiselect list? To look into it
				list.AppendValues (enumValue);
			}
		}

		private Gtk.ListStore createListStore()
		{
			this.columns.Clear ();

			if (this.columnListView != null && this.columnListView.Selection != null) {
				int columnCount = this.columnListView.Selection.CountSelectedRows ();
				Gtk.TreeModel selectedRows = null;

				// Get the paths to reach all the selected rows
				Gtk.TreePath[] selectedRowPaths = this.columnListView.Selection.GetSelectedRows (out selectedRows);

				// Here we will store string representations of the selected column names
				List<String> selectedColumns = new List<String> ();

				// Find what columns where selected  in the list
				foreach (Gtk.TreePath path in selectedRowPaths) {
					Gtk.TreeIter iterator;
					selectedRows.GetIter (out iterator, path);
					selectedColumns.Add((String)selectedRows.GetValue (iterator, 0)); // Get column name
				}

				// Need to get the property names of the columns based on enum from the string list
				int i = 0;

				foreach (String column in selectedColumns) {
					IssueProperties enumValue = (IssueProperties)Enum.Parse (typeof(IssueProperties), column);
					DescriptionAttribute descriptionAttribute = (DescriptionAttribute)Attribute.GetCustomAttribute (typeof(IssueProperties).GetField (column), typeof(DescriptionAttribute));
					this.columns.Add (new IssueColumn (typeof(String), column, descriptionAttribute.Description, i++));
				}

				// Set up the types of the columns in the list store
				Type[] columnTypes = new Type[this.columns.Count];

				for (i = 0; i < this.columns.Count; i++) {
					columnTypes [i] = this.columns [i].Type;
				}

				return new Gtk.ListStore (columnTypes);
			}

			return null;
		}

		private List<Gtk.TreeViewColumn> createAndConfigureColumns(Gtk.TreeView treeView)
		{
			List<Gtk.TreeViewColumn> columns = new List<Gtk.TreeViewColumn> ();
			
			foreach (IssueColumn column in this.columns) {
				columns.Add (treeView.AppendColumn (column.Title, new Gtk.CellRendererText (), "text", column.OrderFromLeftIndex));
				columns [columns.Count - 1].SortColumnId = column.OrderFromLeftIndex;
			}
			
			foreach (Gtk.TreeViewColumn column in columns) {
				column.Resizable = true;
				column.Reorderable = true;
				column.Sizing = Gtk.TreeViewColumnSizing.Autosize;
			}

			return columns;
		}
	}
}