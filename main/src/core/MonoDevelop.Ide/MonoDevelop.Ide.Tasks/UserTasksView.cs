//
// UserTasksView.cs
//
// Author:
//   David Makovský <yakeen@sannyas-on.net>
//
// Copyright (C) 2006 David Makovský
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Components;
using System.Runtime.CompilerServices;


namespace MonoDevelop.Ide.Tasks
{	
	internal class UserTasksView : ITaskListView	
	{
		Button newButton;
		Button copyButton;
		Button delButton;

		Xwt.ListView view;
		Xwt.ListStore store;

		Xwt.Drawing.Color highPrioColor, normalPrioColor, lowPrioColor;
		
		Clipboard clipboard;
		bool solutionLoaded = false;
		bool updating;

		Xwt.DataField<Xwt.ItemCollection> itemsField = new Xwt.DataField<Xwt.ItemCollection> ();
		Xwt.DataField<int> priorityDataField = new Xwt.DataField<int> ();
		Xwt.DataField<string> descriptionDataField = new Xwt.DataField<string> ();
		Xwt.DataField<TaskListEntry> entryDataField = new Xwt.DataField<TaskListEntry> ();
		Xwt.DataField<bool> completedDataField = new Xwt.DataField<bool> ();

		Xwt.DataField<Xwt.Drawing.Color> foregroundColorDataField = new Xwt.DataField<Xwt.Drawing.Color> (); // foreground color
		Xwt.DataField<int> fontStyleDataField = new Xwt.DataField<int> (); // font style

		Gtk.Widget gtkWidget;

		Xwt.TextCellView descriptionCellView;
		
		public UserTasksView ()
		{
			highPrioColor = StringToColor (IdeApp.Preferences.UserTasksHighPrioColor);
			normalPrioColor = StringToColor (IdeApp.Preferences.UserTasksNormalPrioColor);
			lowPrioColor = StringToColor (IdeApp.Preferences.UserTasksLowPrioColor);

			store = new Xwt.ListStore (priorityDataField, itemsField, descriptionDataField, completedDataField, entryDataField, foregroundColorDataField, fontStyleDataField);

			view = new Xwt.ListView ();
			gtkWidget = view.ToGtkWidget ();
			view.DataSource = store;
			view.BorderVisible = false;

			var comboCellView = new Xwt.ComboBoxCellView { Editable = true, SelectedIndexField = priorityDataField };
			foreach (var item in Enum.GetValues (typeof (TaskPriority))) {
				comboCellView.Items.Add ((int) item, item.ToString ());
			}
			view.Columns.Add (new Xwt.ListViewColumn ("Priority", comboCellView) { CanResize = true });

			var completedCellView = new Xwt.CheckBoxCellView (completedDataField) { Editable = true };
			view.Columns.Add (new Xwt.ListViewColumn ("", completedCellView) { CanResize = true });

			descriptionCellView = new Xwt.TextCellView (descriptionDataField) { Editable = true };
			view.Columns.Add (new Xwt.ListViewColumn ("Description", descriptionCellView) { CanResize = true, Expands = true });

			view.SelectionChanged += SelectionChanged;
			comboCellView.SelectionChanged += ComboCellView_SelectionChanged;
			completedCellView.Toggled += CheckCellView_SelectionChanged;
			descriptionCellView.TextChanged += ComboCellView_SelectionChanged;

			newButton = new Button ();
			newButton.Label = GettextCatalog.GetString ("New Task");
			newButton.Image = new ImageView (Gtk.Stock.New, IconSize.Menu);
			newButton.Image.Show ();
			newButton.Clicked += new EventHandler (NewUserTaskClicked); 
			newButton.TooltipText = GettextCatalog.GetString ("Create New Task");

			copyButton = new Button ();
			copyButton.Label = GettextCatalog.GetString ("Copy Task");
			copyButton.Image = new ImageView (Gtk.Stock.Copy, IconSize.Menu);
			copyButton.Image.Show ();
			copyButton.Clicked += CopyUserTaskClicked;
			copyButton.TooltipText = GettextCatalog.GetString ("Copy to Clipboard Task Description");
			
			delButton = new Button ();
			delButton.Label = GettextCatalog.GetString ("Delete Task");
			delButton.Image = new ImageView (Gtk.Stock.Delete, IconSize.Menu);
			delButton.Image.Show ();
			delButton.Clicked += DeleteUserTaskClicked; 
			delButton.TooltipText = GettextCatalog.GetString ("Delete Task");

			TaskService.UserTasks.TasksChanged += UserTasksChanged;
			TaskService.UserTasks.TasksAdded += UserTasksChanged;
			TaskService.UserTasks.TasksRemoved += UserTasksChanged;
			
			if (IdeApp.Workspace.IsOpen)
				solutionLoaded = true;
			
			IdeApp.Workspace.FirstWorkspaceItemOpened += CombineOpened;
			IdeApp.Workspace.LastWorkspaceItemClosed += CombineClosed;

			IdeApp.Preferences.UserTasksLowPrioColor.Changed += OnPropertyUpdated;
			IdeApp.Preferences.UserTasksNormalPrioColor.Changed += OnPropertyUpdated;
			IdeApp.Preferences.UserTasksHighPrioColor.Changed += OnPropertyUpdated;
			ValidateButtons ();
			
			// Initialize with existing tags.
			UserTasksChanged (this, null);
		}

		void CombineOpened (object sender, EventArgs e)
		{
			solutionLoaded = true;
			ValidateButtons ();
		}
		
		void CombineClosed (object sender, EventArgs e)
		{
			solutionLoaded = true;
			ValidateButtons ();
		}
		
		void UserTasksChanged (object sender, TaskEventArgs e)
		{
			if (updating)
				return;

			//if (view.IsRealized)
				//view.ScrollToPoint (0, 0);

			store.Clear ();
			foreach (var task in TaskService.UserTasks) {
				var r = store.AddRow ();
				store.SetValue (r, descriptionDataField, task.Description);
				store.SetValue (r, priorityDataField, (int)task.Priority);
				store.SetValue (r, completedDataField, task.Completed);
				store.SetValue (r, entryDataField, task);

				store.SetValue (r, foregroundColorDataField, GetColorByPriority (task.Priority));
				store.SetValue (r, fontStyleDataField, task.Completed ? (int)Pango.Weight.Light : (int)Pango.Weight.Bold);
				//store.AppendValues (text, task.Completed, task.Description, task, GetColorByPriority (task.Priority), );
			}
			ValidateButtons ();
		}
		
		void OnPropertyUpdated (object sender, EventArgs e)
		{
			highPrioColor = StringToColor (IdeApp.Preferences.UserTasksHighPrioColor);
			normalPrioColor = StringToColor (IdeApp.Preferences.UserTasksNormalPrioColor);
			lowPrioColor = StringToColor (IdeApp.Preferences.UserTasksLowPrioColor);

			for (int i = 0; i < store.RowCount; i++) {
				var task = store.GetValue (i, entryDataField);
				store.SetValue (i, foregroundColorDataField, GetColorByPriority (task.Priority));
			}
		}
		
		void SelectionChanged (object sender, EventArgs e)
		{
			ValidateButtons ();
		}
		
		void ValidateButtons ()
		{
			newButton.Sensitive = solutionLoaded;
			delButton.Sensitive = copyButton.Sensitive = solutionLoaded && view.SelectedRow > -1;
		}
		
		void NewUserTaskClicked (object obj, EventArgs e)
		{
			var task = new TaskListEntry ();
			task.WorkspaceObject = IdeApp.ProjectOperations.CurrentSelectedWorkspaceItem;
			updating = true;
			TaskService.UserTasks.Add (task);
			updating = false;

			var row =  store.AddRow ();
			store.SetValue (row, descriptionDataField, task.Description);
			store.SetValue (row, priorityDataField, (int)task.Priority);
			store.SetValue (row, completedDataField, task.Completed);
			store.SetValue (row, entryDataField, task);

			view.SelectRow (row);
			view.StartEditingCell (row, descriptionCellView);
			TaskService.SaveUserTasks (task.WorkspaceObject);
		}

		void CopyUserTaskClicked (object o, EventArgs args)
		{
			var index = view.SelectedRow;
			if (index == -1) {
				return;
			}
			var task = store.GetValue (index, entryDataField);
			clipboard = Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
			clipboard.Text = task.Description;
			clipboard = Clipboard.Get (Gdk.Atom.Intern ("PRIMARY", false));
			clipboard.Text = task.Description;
		}
		
		void DeleteUserTaskClicked (object obj, EventArgs e)
		{
			var index = view.SelectedRow;
			if (index == -1) {
				return;
			}
			updating = true;
			var task = store.GetValue (index, entryDataField);
			TaskService.UserTasks.Remove (task);
			updating = false;
			store.RemoveRow (index);
			TaskService.SaveUserTasks (task.WorkspaceObject);
		}

		void CheckCellView_SelectionChanged (object sender, Xwt.WidgetEventArgs e)
		{
			var index = view.SelectedRow;
			if (index == -1) {
				return;
			}

			var task = store.GetValue (index, entryDataField);
			task.Completed = !store.GetValue (index, completedDataField);

			TaskService.SaveUserTasks (task.WorkspaceObject);
		}

		void ComboCellView_SelectionChanged (object sender, Xwt.WidgetEventArgs e)
		{
			var index = view.SelectedRow;
			if (index == -1) {
				return;
			}

			var task = store.GetValue (index, entryDataField);
			task.Description = store.GetValue (index, descriptionDataField);
			task.Completed = store.GetValue (index, completedDataField);
			task.Priority = (TaskPriority)store.GetValue (index, priorityDataField);

			store.SetValue (index, foregroundColorDataField, GetColorByPriority (task.Priority));
			store.SetValue (index, entryDataField, task);
			TaskService.SaveUserTasks (task.WorkspaceObject);
		}

		Xwt.Drawing.Color GetColorByPriority (TaskPriority prio)
		{
			switch (prio)
			{
				case TaskPriority.High:
					return highPrioColor;
				case TaskPriority.Normal:
					return normalPrioColor;
				default:
					return lowPrioColor;
			}
		}

		static SortType ReverseSortOrder (TreeViewColumn col)
		{
			if (col.SortIndicator) {
				if (col.SortOrder == SortType.Ascending)
					return SortType.Descending;
				else
					return SortType.Ascending;
			} else
			{
				return SortType.Ascending;
			}
		}
		
		static Xwt.Drawing.Color StringToColor (string colorStr)
		{
			string[] rgb = colorStr.Substring (colorStr.IndexOf (':') + 1).Split ('/');
			if (rgb.Length != 3) return new Xwt.Drawing.Color (0, 0, 0);
			var color = Gdk.Color.Zero;
			try
			{
				color.Red = UInt16.Parse (rgb[0], System.Globalization.NumberStyles.HexNumber);
				color.Green = UInt16.Parse (rgb[1], System.Globalization.NumberStyles.HexNumber);
				color.Blue = UInt16.Parse (rgb[2], System.Globalization.NumberStyles.HexNumber);
			}
			catch
			{
				// something went wrong, then use neutral black color
				color = new Gdk.Color (0, 0, 0);
			}
			return color.ToXwtColor ();
		}
		
		#region ITaskListView members
		Control ITaskListView.Content { get { return gtkWidget; } }
		Control[] ITaskListView.ToolBarItems { get { return new Control[] { newButton, delButton, copyButton }; } }
		#endregion
	}
}
