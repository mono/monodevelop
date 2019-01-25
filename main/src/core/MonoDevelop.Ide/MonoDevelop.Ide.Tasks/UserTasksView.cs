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
using System.Text;

namespace MonoDevelop.Ide.Tasks
{	
	internal class UserTasksView : ITaskListView	
	{
		Button newButton;
		Button copyButton;
		Button delButton;

		Xwt.ListView view;
		Xwt.ListStore store;

		string highPrioColor, normalPrioColor, lowPrioColor;
		
		Clipboard clipboard;
		bool solutionLoaded = false;
		bool updating;

		Xwt.DataField<Xwt.ItemCollection> itemsField = new Xwt.DataField<Xwt.ItemCollection> ();
		Xwt.DataField<int> priorityDataField = new Xwt.DataField<int> ();

		Xwt.DataField<string> descriptionDataField = new Xwt.DataField<string> ();
		Xwt.DataField<string> formatedDescriptionDataField = new Xwt.DataField<string> ();

		Xwt.DataField<TaskListEntry> entryDataField = new Xwt.DataField<TaskListEntry> ();
		Xwt.DataField<bool> completedDataField = new Xwt.DataField<bool> ();

		Gtk.Widget gtkWidget;

		Xwt.TextCellView descriptionCellView;
		
		public UserTasksView ()
		{
			highPrioColor = ConfigurationStringToHex (IdeApp.Preferences.UserTasksHighPrioColor);
			normalPrioColor = ConfigurationStringToHex (IdeApp.Preferences.UserTasksNormalPrioColor);
			lowPrioColor = ConfigurationStringToHex (IdeApp.Preferences.UserTasksLowPrioColor);

			store = new Xwt.ListStore (priorityDataField, itemsField, descriptionDataField, completedDataField, entryDataField, formatedDescriptionDataField);

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

			descriptionCellView = new Xwt.TextCellView (descriptionDataField) { Editable = true, MarkupField = formatedDescriptionDataField };
			view.Columns.Add (new Xwt.ListViewColumn ("Description", descriptionCellView) { CanResize = true, Expands = true });

			view.SelectionChanged += SelectionChanged;

			comboCellView.EditingFinished += DescriptionCellView_EditingFinished;
			completedCellView.EditingFinished += DescriptionCellView_EditingFinished;
			descriptionCellView.EditingFinished += DescriptionCellView_EditingFinished;

			newButton = new Button ();
			newButton.Label = GettextCatalog.GetString ("New Task");
			newButton.Image = new ImageView (Gtk.Stock.New, IconSize.Menu);
			newButton.Image.Show ();
			newButton.Clicked += NewUserTaskClicked; 
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

			TaskService.UserTasks.TasksChanged += RaiseRefreshListView;
			TaskService.UserTasks.TasksAdded += RaiseRefreshListView;
			TaskService.UserTasks.TasksRemoved += RaiseRefreshListView;
			
			if (IdeApp.Workspace.IsOpen)
				solutionLoaded = true;
			
			IdeApp.Workspace.FirstWorkspaceItemOpened += CombineOpened;
			IdeApp.Workspace.LastWorkspaceItemClosed += CombineClosed;

			IdeApp.Preferences.UserTasksLowPrioColor.Changed += OnPropertyUpdated;
			IdeApp.Preferences.UserTasksNormalPrioColor.Changed += OnPropertyUpdated;
			IdeApp.Preferences.UserTasksHighPrioColor.Changed += OnPropertyUpdated;
			ValidateButtons ();
			
			// Initialize with existing tags.
			RaiseRefreshListView (this, null);
		}

		int lastSelectedIndex;
		void DescriptionCellView_EditingFinished (object sender, EventArgs e)
		{
			lastSelectedIndex = view.SelectedRow;
			var task = store.GetValue (lastSelectedIndex, entryDataField);
			task.Description = store.GetValue (lastSelectedIndex, descriptionDataField);
			task.Completed = store.GetValue (lastSelectedIndex, completedDataField);
			task.Priority = (TaskPriority)store.GetValue (lastSelectedIndex, priorityDataField);
			store.SetValue (lastSelectedIndex, entryDataField, task);
			TaskService.SaveUserTasks (task.WorkspaceObject);

			RaiseRefreshListView (this, null);
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
		
		void RaiseRefreshListView (object sender, TaskEventArgs e)
		{
			if (updating)
				return;

			store.Clear ();
			foreach (var task in TaskService.UserTasks) {
				var r = store.AddRow ();
				store.SetValue (r, descriptionDataField, task.Description);
				store.SetValue (r, priorityDataField, (int)task.Priority);
				store.SetValue (r, completedDataField, task.Completed);
				store.SetValue (r, entryDataField, task);
				store.SetValue (r, formatedDescriptionDataField, GetFormatedText (task));
			}

			if (lastSelectedIndex < store.RowCount) {
				view.SelectRow (lastSelectedIndex);
			}

			ValidateButtons ();
		}

		string GetFormatedText (TaskListEntry task)
		{
			var builder = new StringBuilder ();
			if (task.Completed) {
				builder.Append ("<b>");
			}
			builder.Append (string.Format ("<span color=\"{0}\"", GetColorByPriority (task.Priority)));
			if (task.Completed) {
				builder.Append (" strikethrough=\"true\"");
			}
			builder.Append (">");
			builder.Append (task.Description);
			builder.Append ("</span>");
			if (task.Completed) {
				builder.Append ("</b>");
			}
			return builder.ToString ();
		}

		void OnPropertyUpdated (object sender, EventArgs e)
		{
			highPrioColor = ConfigurationStringToHex (IdeApp.Preferences.UserTasksHighPrioColor);
			normalPrioColor = ConfigurationStringToHex (IdeApp.Preferences.UserTasksNormalPrioColor);
			lowPrioColor = ConfigurationStringToHex (IdeApp.Preferences.UserTasksLowPrioColor);

			RaiseRefreshListView (null, null);
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

			var r =  store.AddRow ();
			store.SetValue (r, descriptionDataField, task.Description);
			store.SetValue (r, priorityDataField, (int)task.Priority);
			store.SetValue (r, completedDataField, task.Completed);
			store.SetValue (r, entryDataField, task);
			store.SetValue (r, formatedDescriptionDataField, GetFormatedText (task));

			view.SelectRow (r);
			view.StartEditingCell (r, descriptionCellView);
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
			store.SetValue (index, entryDataField, task);
			TaskService.SaveUserTasks (task.WorkspaceObject);
		}

		string GetColorByPriority (TaskPriority prio)
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
			} 
			return SortType.Ascending;
		}

		static string ConfigurationStringToHex (string colorStr)
		{
			string[] rgb = colorStr.Substring (colorStr.IndexOf (':') + 1).Split ('/');
			if (rgb.Length != 3) return "#000000";
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
			return color.GetHex ();
		}

		#region ITaskListView members
		Control ITaskListView.Content { get { return gtkWidget; } }
		Control[] ITaskListView.ToolBarItems { get { return new Control[] { newButton, delButton, copyButton }; } }
		#endregion
	}
}
