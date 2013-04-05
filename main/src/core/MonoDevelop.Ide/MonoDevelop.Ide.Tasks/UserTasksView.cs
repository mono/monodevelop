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
		Button delButton;

		MonoDevelop.Ide.Gui.Components.PadTreeView view;
		ListStore store;
		CellRendererText cellRendDesc;
		
		Gdk.Color highPrioColor, normalPrioColor, lowPrioColor;
		
		Clipboard clipboard;
		bool solutionLoaded = false;
		bool updating;
		string[] priorities = { GettextCatalog.GetString ("High"), GettextCatalog.GetString ("Normal"), GettextCatalog.GetString ("Low")};
		
		public UserTasksView ()
		{
			highPrioColor = StringToColor ((string)PropertyService.Get ("Monodevelop.UserTasksHighPrioColor", ""));
			normalPrioColor = StringToColor ((string)PropertyService.Get ("Monodevelop.UserTasksNormalPrioColor", ""));
			lowPrioColor = StringToColor ((string)PropertyService.Get ("Monodevelop.UserTasksLowPrioColor", ""));
			
			store = new ListStore (
				typeof (string),     // priority
				typeof (bool),		 // completed 
				typeof (string),     // desc
				typeof (Task),	 // user task
				typeof (Gdk.Color),  // foreground color
				typeof (int));		 // font style
			
			view = new MonoDevelop.Ide.Gui.Components.PadTreeView (store);
			view.RulesHint = true;
			view.SearchColumn = (int)Columns.Description;
			view.Selection.Changed += new EventHandler (SelectionChanged);
			view.DoPopupMenu = ShowUserPopup;
			TreeViewColumn col;
			
			CellRendererComboBox cellRendPriority = new CellRendererComboBox ();
			cellRendPriority.Values = priorities;
			cellRendPriority.Editable = true;
			cellRendPriority.Changed += new ComboSelectionChangedHandler (UserTaskPriorityEdited);
			col = view.AppendColumn (GettextCatalog.GetString ("Priority"), cellRendPriority, "text", Columns.Priority, "foreground-gdk", Columns.Foreground, "weight", Columns.Bold);
			col.Clickable = true;
			col.Resizable = true;
			TreeIterCompareFunc sortFunc = new TreeIterCompareFunc (PrioirtySortFunc);
			store.SetSortFunc ((int)Columns.Priority, sortFunc);
			col.Clicked += new EventHandler (UserTaskPriorityResort);
			
			CellRendererToggle cellRendCompleted = new CellRendererToggle ();
			cellRendCompleted.Toggled += new ToggledHandler (UserTaskCompletedToggled);
			cellRendCompleted.Activatable = true;
			col = view.AppendColumn (String.Empty, cellRendCompleted, "active", Columns.Completed);
			col.Clickable = true;
			col.Clicked += new EventHandler (UserTaskCompletedResort);
			
			cellRendDesc = view.TextRenderer;
			cellRendDesc.Editable = true;
			cellRendDesc.Edited += new EditedHandler (UserTaskDescEdited);
			col = view.AppendColumn (GettextCatalog.GetString ("Description"), cellRendDesc, "text", Columns.Description, "strikethrough", Columns.Completed, "foreground-gdk", Columns.Foreground, "weight", Columns.Bold);
			col.Clickable = true;
			col.Resizable = true;
			col.Clicked += new EventHandler (UserTaskDescResort);
			
			newButton = new Button ();
			newButton.Image = new Gtk.Image (Gtk.Stock.New, IconSize.Button);
			newButton.Label = GettextCatalog.GetString ("New Task");
			newButton.ImagePosition = PositionType.Left;
			newButton.Clicked += new EventHandler (NewUserTaskClicked); 
			newButton.TooltipText = GettextCatalog.GetString ("Create New Task");
			
			delButton = new Button (new Gtk.Image (Gtk.Stock.Delete, IconSize.Button));
			delButton.Clicked += new EventHandler (DeleteUserTaskClicked); 
			delButton.TooltipText = GettextCatalog.GetString ("Delete Task");

			TaskService.UserTasks.TasksChanged += DispatchService.GuiDispatch<TaskEventHandler> (UserTasksChanged);
			TaskService.UserTasks.TasksAdded += DispatchService.GuiDispatch<TaskEventHandler> (UserTasksChanged);
			TaskService.UserTasks.TasksRemoved += DispatchService.GuiDispatch<TaskEventHandler> (UserTasksChanged);
			
			if (IdeApp.Workspace.IsOpen)
				solutionLoaded = true;
			
			IdeApp.Workspace.FirstWorkspaceItemOpened += CombineOpened;
			IdeApp.Workspace.LastWorkspaceItemClosed += CombineClosed;
			PropertyService.PropertyChanged += DispatchService.GuiDispatch<EventHandler<PropertyChangedEventArgs>> (OnPropertyUpdated);
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
			foreach (Task task in TaskService.UserTasks) {
				store.AppendValues (GettextCatalog.GetString (Enum.GetName (typeof (TaskPriority), task.Priority)), task.Completed, task.Description, task, GetColorByPriority (task.Priority), task.Completed ? (int)Pango.Weight.Light : (int)Pango.Weight.Bold);
			}
			ValidateButtons ();
		}
		
		void OnPropertyUpdated (object sender, PropertyChangedEventArgs e)
		{
			bool change = false;
			if (e.Key == "Monodevelop.UserTasksHighPrioColor" && e.NewValue != e.OldValue)
			{
				highPrioColor = StringToColor ((string)e.NewValue);
				change = true;
			}
			if (e.Key == "Monodevelop.UserTasksNormalPrioColor" && e.NewValue != e.OldValue)
			{
				normalPrioColor = StringToColor ((string)e.NewValue);
				change = true;
			}
			if (e.Key == "Monodevelop.UserTasksLowPrioColor" && e.NewValue != e.OldValue)
			{
				lowPrioColor = StringToColor ((string)e.NewValue);
				change = true;
			}
			if (change)
			{
				TreeIter iter;
				if (store.GetIterFirst (out iter))
				{
					do
					{
						Task task = (Task) store.GetValue (iter, (int)Columns.UserTask);
						store.SetValue (iter, (int)Columns.Foreground, GetColorByPriority (task.Priority));
					} while (store.IterNext (ref iter));
				}
			}
		}
		
		void SelectionChanged (object sender, EventArgs e)
		{
			ValidateButtons ();
		}
		
		void ValidateButtons ()
		{
			newButton.Sensitive = solutionLoaded;
			delButton.Sensitive = solutionLoaded && view.Selection.CountSelectedRows () > 0;
		}
		
		void NewUserTaskClicked (object obj, EventArgs e)
		{
			Task task = new Task ();
			task.WorkspaceObject = IdeApp.ProjectOperations.CurrentSelectedWorkspaceItem;
			updating = true;
			TaskService.UserTasks.Add (task);
			updating = false;
			TreeIter iter = store.AppendValues (GettextCatalog.GetString (Enum.GetName (typeof (TaskPriority), task.Priority)), task.Completed, task.Description, task, GetColorByPriority (task.Priority), task.Completed ? (int)Pango.Weight.Light : (int)Pango.Weight.Bold);
			view.Selection.SelectIter (iter);
			TreePath path = store.GetPath (iter);
			view.ScrollToCell (path, view.Columns[(int)Columns.Description], true, 0, 0);
			view.SetCursorOnCell (path, view.Columns[(int)Columns.Description], cellRendDesc, true);
			TaskService.SaveUserTasks (task.WorkspaceObject);
		}
		
		void DeleteUserTaskClicked (object obj, EventArgs e)
		{
			if (view.Selection.CountSelectedRows () > 0)
			{
				TreeIter iter;
				if (store.GetIter (out iter, view.Selection.GetSelectedRows ()[0]))
				{
					Task task = (Task) store.GetValue (iter, (int)Columns.UserTask);
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
			Gtk.TreeIter iter;
			if (store.GetIterFromString (out iter,  args.Path)) {
				Task task = (Task) store.GetValue (iter, (int)Columns.UserTask);
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
		
		int PrioirtySortFunc (TreeModel model, TreeIter iter1, TreeIter iter2)
		{
			TaskPriority prio1 = (TaskPriority) Enum.Parse (typeof (TaskPriority), (string)model.GetValue (iter1, (int)Columns.Priority));
			TaskPriority prio2 = (TaskPriority) Enum.Parse (typeof (TaskPriority), (string)model.GetValue (iter2, (int)Columns.Priority));

			if (prio1 == prio2)
				return 0;
			return prio1 < prio2 ? 1 : -1;
		}
		
		void UserTaskPriorityResort (object sender, EventArgs args)
		{
			TreeViewColumn col = view.Columns[(int)Columns.Priority];
			foreach (TreeViewColumn c in view.Columns)
			{
				if (c != col) c.SortIndicator = false;
			}
			col.SortOrder = ReverseSortOrder (col);
			col.SortIndicator = true;
			store.SetSortColumnId ((int)Columns.Priority, col.SortOrder);
		}
		
		void UserTaskCompletedToggled (object o, ToggledArgs args)
		{
			Gtk.TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path)) {
				bool val = (bool)store.GetValue (iter, (int)Columns.Completed);
				Task task = (Task) store.GetValue (iter, (int)Columns.UserTask);
				task.Completed = !val;
				store.SetValue (iter, (int)Columns.Completed, !val);
				store.SetValue (iter, (int)Columns.Bold, task.Completed ? (int)Pango.Weight.Light : (int)Pango.Weight.Bold);
				TaskService.SaveUserTasks (task.WorkspaceObject);
			}
		}
		
		void UserTaskCompletedResort (object sender, EventArgs args)
		{
			TreeViewColumn col = view.Columns[(int)Columns.Completed];
			foreach (TreeViewColumn c in view.Columns)
			{
				if (c != col) c.SortIndicator = false;
			}
			col.SortOrder = ReverseSortOrder (col);
			col.SortIndicator = true;
			store.SetSortColumnId ((int)Columns.Completed, col.SortOrder);
		}
		
		void UserTaskDescEdited (object o, EditedArgs args)
		{
			Gtk.TreeIter iter;
			if (store.GetIterFromString (out iter,  args.Path)) {
				Task task = (Task) store.GetValue (iter, (int)Columns.UserTask);
				task.Description = args.NewText;
				store.SetValue (iter, (int)Columns.Description, args.NewText);
				TaskService.SaveUserTasks (task.WorkspaceObject);
			}
		}
		
		void UserTaskDescResort (object sender, EventArgs args)
		{
			TreeViewColumn col = view.Columns[(int)Columns.Description];
			foreach (TreeViewColumn c in view.Columns)
			{
				if (c != col) c.SortIndicator = false;
			}
			col.SortOrder = ReverseSortOrder (col);
			col.SortIndicator = true;
			store.SetSortColumnId ((int)Columns.Description, col.SortOrder);
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

		void ShowUserPopup (Gdk.EventButton evt)
		{
			var menu = new Menu () {
				AccelGroup = new AccelGroup (),
			};
			var copy = new ImageMenuItem (Gtk.Stock.Copy, menu.AccelGroup);
			copy.Activated += OnUserTaskCopied;
			menu.Append (copy);
			IdeApp.CommandService.ShowContextMenu (view, evt, menu);
		}

		void OnUserTaskCopied (object o, EventArgs args)
		{
			Task task;
			TreeModel model;
			TreeIter iter;

			if (view.Selection.GetSelected (out model, out iter))
			{
				task = (Task) model.GetValue (iter, (int)Columns.UserTask);
			}
			else return; // no one selected

			clipboard = Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
			clipboard.Text = task.ToString ();
			clipboard = Clipboard.Get (Gdk.Atom.Intern ("PRIMARY", false));
			clipboard.Text = task.ToString ();
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
		TreeView ITaskListView.Content { get { return view; } }
		Widget[] ITaskListView.ToolBarItems { get { return new Widget[] { newButton, delButton }; } }
		#endregion
	}
}
