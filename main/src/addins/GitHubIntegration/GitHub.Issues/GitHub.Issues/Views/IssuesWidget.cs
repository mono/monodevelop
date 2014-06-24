using System;
using System.Collections.Generic;
using Octokit;

namespace GitHub.Issues.UserInterface
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class IssuesWidget : Gtk.Bin
	{
		private List<IssueColumn> columns = new List<IssueColumn> ();
		private List<Gtk.TreeViewColumn> treeColumns = new List<Gtk.TreeViewColumn> ();

		private Gtk.ListStore listStore;
		private Gtk.TreeModelFilter filter;
		private Gtk.TreeModelSort sort;

		public Gtk.ListStore ListStore
		{
			get {
				if (listStore == null) {
					listStore = new Gtk.ListStore (typeof(IssueNode));
				}

				return listStore;
			}
		}

		public IssuesWidget (IReadOnlyList<Octokit.Issue> issues)
		{
			this.Build ();

			this.listStore = this.createListStore ();

			Gtk.TreeView treeView = new Gtk.TreeView ();

			this.treeColumns = this.createAndConfigureColumns (treeView);

			foreach (Octokit.Issue issue in issues) {
				this.addRowBasedOnIssue (this.ListStore, issue);
			}

			filter = new Gtk.TreeModelFilter (this.ListStore, null);
			sort = new Gtk.TreeModelSort (filter);

			treeView.Model = sort;

			this.SetColumnSortHandlers (this.treeColumns, this.sort);

			this.SetFilteringHandlers (this.treeColumns, this.filter);

			this.Add (treeView);

			this.ShowAll ();
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

		private void CreateAndAddColumnSelectionsToTable(Gtk.Table table, Type enumType)
		{
			foreach (string enumValue in Enum.GetNames (enumType))
			{
				// Create a checkbox, store it in an array so that we can quickly go through them and get a list of columns to populate
				// Add the checkbox to the table.
				// Have a VBox of checkboxes which can be collapsed.
				// Or have a multiselect list? To look into it
			}
		}

		private Gtk.ListStore createListStore()
		{
			this.columns.Add (new IssueColumn (typeof(String), "Title", "Title", 0));
			this.columns.Add (new IssueColumn (typeof(String), "Body", "Body", 1));
			this.columns.Add (new IssueColumn (typeof(String), "Assigned To", "Assignee.Login", 2));
			this.columns.Add (new IssueColumn (typeof(String), "Last Updated", "UpdatedAt", 3));
			this.columns.Add (new IssueColumn (typeof(String), "State", "State", 4));

			Type[] columnTypes = new Type[this.columns.Count];

			for (int i = 0; i < this.columns.Count; i++) {
				columnTypes [i] = this.columns [i].Type;
			}

			return new Gtk.ListStore (columnTypes);
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