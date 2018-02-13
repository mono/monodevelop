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

namespace MonoDevelop.Ide.Tasks
{	
	internal class UserTasksView : ITaskListView	
	{
		enum Columns
		{
			Priority,
			Completed,
			Description,
			UserTask,
			Foreground,
			Bold,
			Count
		}
		
		Button newButton;
		Button copyButton;
		Button delButton;

		MonoDevelop.Ide.Gui.Components.PadTreeView view;
		ListStore store;
		TreeModelSort sortModel;
		CellRendererText cellRendDesc;
		
		Gdk.Color highPrioColor, normalPrioColor, lowPrioColor;
		
		Clipboard clipboard;
		bool solutionLoaded = false;
		bool updating;
		string[] priorities = { GettextCatalog.GetString ("High"), GettextCatalog.GetString ("Normal"), GettextCatalog.GetString ("Low")};
		
		public UserTasksView ()
		{
			highPrioColor = StringToColor (IdeApp.Preferences.UserTasksHighPrioColor);
			normalPrioColor = StringToColor (IdeApp.Preferences.UserTasksNormalPrioColor);
			lowPrioColor = StringToColor (IdeApp.Preferences.UserTasksLowPrioColor);
			
			store = new ListStore (
				typeof (string),     // priority
				typeof (bool),		 // completed 
				typeof (string),     // desc
				typeof (TaskListEntry),	 // user task
				typeof (Gdk.Color),  // foreground color
				typeof (int));		 // font style

			sortModel = new TreeModelSort (store);

			view = new MonoDevelop.Ide.Gui.Components.PadTreeView (sortModel);
			view.RulesHint = true;
			view.SearchColumn = (int)Columns.Description;
			view.Selection.Changed += new EventHandler (SelectionChanged);
			TreeViewColumn col;
			
			CellRendererComboBox cellRendPriority = new CellRendererComboBox ();
			cellRendPriority.Values = priorities;
			cellRendPriority.Editable = true;
			cellRendPriority.Changed += new ComboSelectionChangedHandler (UserTaskPriorityEdited);
			col = view.AppendColumn (GettextCatalog.GetString ("Priority"), cellRendPriority, "text", Columns.Priority, "foreground-gdk", Columns.Foreground, "weight", Columns.Bold);
			col.Resizable = true;
			col.SortColumnId = (int)Columns.Priority;
			
			CellRendererToggle cellRendCompleted = new CellRendererToggle ();
			cellRendCompleted.Toggled += new ToggledHandler (UserTaskCompletedToggled);
			cellRendCompleted.Activatable = true;
			col = view.AppendColumn (String.Empty, cellRendCompleted, "active", Columns.Completed);
			col.SortColumnId = (int)Columns.Completed;

			cellRendDesc = view.TextRenderer;
			cellRendDesc.Editable = true;
			cellRendDesc.Edited += new EditedHandler (UserTaskDescEdited);
			col = view.AppendColumn (GettextCatalog.GetString ("Description"), cellRendDesc, "text", Columns.Description, "strikethrough", Columns.Completed, "foreground-gdk", Columns.Foreground, "weight", Columns.Bold);
			col.Resizable = true;
			col.SortColumnId = (int)Columns.Description;
			
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
			copyButton.TooltipText = GettextCatalog.GetString ("Copy Task Description");
			
			delButton = new Button ();
			delButton.Label = GettextCatalog.GetString ("Delete Task");
			delButton.Image = new ImageView (Gtk.Stock.Delete, IconSize.Menu);
			delButton.Image.Show ();
			delButton.Clicked += new EventHandler (DeleteUserTaskClicked); 
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

			if (view.IsRealized)
				view.ScrollToPoint (0, 0);

			store.Clear ();
			foreach (TaskListEntry task in TaskService.UserTasks) {
				store.AppendValues (GettextCatalog.GetString (Enum.GetName (typeof (TaskPriority), task.Priority)), task.Completed, task.Description, task, GetColorByPriority (task.Priority), task.Completed ? (int)Pango.Weight.Light : (int)Pango.Weight.Bold);
			}
			ValidateButtons ();
		}
		
		void OnPropertyUpdated (object sender, EventArgs e)
		{
			highPrioColor = StringToColor (IdeApp.Preferences.UserTasksHighPrioColor);
			normalPrioColor = StringToColor (IdeApp.Preferences.UserTasksNormalPrioColor);
			lowPrioColor = StringToColor (IdeApp.Preferences.UserTasksLowPrioColor);

			TreeIter iter;
			if (store.GetIterFirst (out iter))
			{
				do
				{
					TaskListEntry task = (TaskListEntry) store.GetValue (iter, (int)Columns.UserTask);
					store.SetValue (iter, (int)Columns.Foreground, GetColorByPriority (task.Priority));
				} while (store.IterNext (ref iter));
			}
		}
		
		void SelectionChanged (object sender, EventArgs e)
		{
			ValidateButtons ();
		}
		
		void ValidateButtons ()
		{
			newButton.Sensitive = solutionLoaded;
			delButton.Sensitive = copyButton.Sensitive = solutionLoaded && view.Selection.CountSelectedRows () > 0;
		}
		
		void NewUserTaskClicked (object obj, EventArgs e)
		{
			TaskListEntry task = new TaskListEntry ();
			task.WorkspaceObject = IdeApp.ProjectOperations.CurrentSelectedWorkspaceItem;
			updating = true;
			TaskService.UserTasks.Add (task);
			updating = false;
			TreeIter iter = store.AppendValues (GettextCatalog.GetString (Enum.GetName (typeof (TaskPriority), task.Priority)), task.Completed, task.Description, task, GetColorByPriority (task.Priority), task.Completed ? (int)Pango.Weight.Light : (int)Pango.Weight.Bold);
			view.Selection.SelectIter (sortModel.ConvertChildIterToIter (iter));
			TreePath sortedPath = sortModel.ConvertChildPathToPath (store.GetPath (iter));
			view.ScrollToCell (sortedPath, view.Columns[(int)Columns.Description], true, 0, 0);
			view.SetCursorOnCell (sortedPath, view.Columns[(int)Columns.Description], cellRendDesc, true);
			TaskService.SaveUserTasks (task.WorkspaceObject);
		}

		void CopyUserTaskClicked (object o, EventArgs args)
		{
			TaskListEntry task;
			TreeModel model;
			TreeIter iter;

			if (view.Selection.GetSelected (out model, out iter))
			{
				task = (TaskListEntry) model.GetValue (iter, (int)Columns.UserTask);
			}
			else return; // no one selected

			clipboard = Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
			clipboard.Text = task.Description;
			clipboard = Clipboard.Get (Gdk.Atom.Intern ("PRIMARY", false));
			clipboard.Text = task.Description;
		}

		
		void DeleteUserTaskClicked (object obj, EventArgs e)
		{
			if (view.Selection.CountSelectedRows () > 0)
			{
				TreeIter iter;
				Gtk.TreePath path = sortModel.ConvertPathToChildPath (view.Selection.GetSelectedRows () [0]);
				if (store.GetIter (out iter, path))
				{
					TaskListEntry task = (TaskListEntry) store.GetValue (iter, (int)Columns.UserTask);
					updating = true;
					TaskService.UserTasks.Remove (task);
					updating = false;
					store.Remove (ref iter);
					TaskService.SaveUserTasks (task.WorkspaceObject);
				}
			}
		}

		void UserTaskPriorityEdited (object o, ComboSelectionChangedArgs args)
		{
			Gtk.TreeIter iter, sortedIter;

			if (sortModel.GetIterFromString (out sortedIter, args.Path)) {
				iter = sortModel.ConvertIterToChildIter (sortedIter);
				TaskListEntry task = (TaskListEntry) sortModel.GetValue (sortedIter, (int)Columns.UserTask);
				if (args.Active == 0)
				{
					task.Priority = TaskPriority.High;
				} else if (args.Active == 1)
				{
					task.Priority = TaskPriority.Normal;
				} else
				{
					task.Priority = TaskPriority.Low;
				}
				store.SetValue (iter, (int)Columns.Priority, priorities [args.Active]);
				store.SetValue (iter, (int)Columns.Foreground, GetColorByPriority (task.Priority));
				TaskService.SaveUserTasks (task.WorkspaceObject);
			}
		}
		
		void UserTaskCompletedToggled (object o, ToggledArgs args)
		{
			Gtk.TreeIter iter, sortedIter;

			if (sortModel.GetIterFromString (out sortedIter, args.Path)) {
				iter = sortModel.ConvertIterToChildIter (sortedIter);
				bool val = (bool)sortModel.GetValue (sortedIter, (int)Columns.Completed);
				TaskListEntry task = (TaskListEntry) sortModel.GetValue (sortedIter, (int)Columns.UserTask);
				task.Completed = !val;
				store.SetValue (iter, (int)Columns.Completed, !val);
				store.SetValue (iter, (int)Columns.Bold, task.Completed ? (int)Pango.Weight.Light : (int)Pango.Weight.Bold);
				TaskService.SaveUserTasks (task.WorkspaceObject);
			}
		}
		
		void UserTaskDescEdited (object o, EditedArgs args)
		{
			Gtk.TreeIter iter, sortedIter;

			if (sortModel.GetIterFromString (out sortedIter, args.Path)) {
				iter = sortModel.ConvertIterToChildIter (sortedIter);
				TaskListEntry task = (TaskListEntry) sortModel.GetValue (sortedIter, (int)Columns.UserTask);
				task.Message = args.NewText;
				store.SetValue (iter, (int)Columns.Description, args.NewText);
				TaskService.SaveUserTasks (task.WorkspaceObject);
			}
		}
		
		Gdk.Color GetColorByPriority (TaskPriority prio)
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
		
		static Gdk.Color StringToColor (string colorStr)
		{
			string[] rgb = colorStr.Substring (colorStr.IndexOf (':') + 1).Split ('/');
			if (rgb.Length != 3) return new Gdk.Color (0, 0, 0);
			Gdk.Color color = Gdk.Color.Zero;
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
			return color;
		}
		
		#region ITaskListView members
		Control ITaskListView.Content { get { return view; } }
		Control[] ITaskListView.ToolBarItems { get { return new Control[] { newButton, delButton, copyButton }; } }
		#endregion
	}
}
