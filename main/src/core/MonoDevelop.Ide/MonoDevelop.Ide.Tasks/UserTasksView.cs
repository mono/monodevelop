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
using System.IO;
using System.Collections;
using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components;
using MonoDevelop.Core.Gui;

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
		
		ToolButton newButton;
		ToolButton delButton;

		MonoDevelop.Ide.Gui.Components.PadTreeView view;
		ListStore store;
		
		Gdk.Color highPrioColor, normalPrioColor, lowPrioColor;
		
		Tooltips tips;
		
		Clipboard clipboard;
		bool solutionLoaded = false;
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
				typeof (UserTask),	 // user task
				typeof (Gdk.Color),  // foreground color
				typeof (int));		 // font style
			
			tips = new Tooltips ();
			
			view = new MonoDevelop.Ide.Gui.Components.PadTreeView (store);
			view.RulesHint = true;
			view.SearchColumn = (int)Columns.Description;
			view.Selection.Changed += new EventHandler (SelectionChanged);
			view.PopupMenu += new PopupMenuHandler (OnUserPopupMenu);
			view.ButtonPressEvent += new ButtonPressEventHandler (OnUserButtonPressed);
			
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
			
			CellRendererText cellRendDesc = view.TextRenderer;
			cellRendDesc.Editable = true;
			cellRendDesc.Edited += new EditedHandler (UserTaskDescEdited);
			col = view.AppendColumn (GettextCatalog.GetString ("Description"), cellRendDesc, "text", Columns.Description, "strikethrough", Columns.Completed, "foreground-gdk", Columns.Foreground, "weight", Columns.Bold);
			col.Clickable = true;
			col.Resizable = true;
			col.Clicked += new EventHandler (UserTaskDescResort);
			
			newButton = new ToolButton (new Gtk.Image (Gtk.Stock.New, IconSize.Button), GettextCatalog.GetString ("New Task"));
			newButton.IsImportant = true;
			newButton.Clicked += new EventHandler (NewUserTaskClicked); 
			newButton.SetTooltip (tips, GettextCatalog.GetString ("Create New Task"), GettextCatalog.GetString ("Create New Task"));
			
			delButton = new ToolButton (new Gtk.Image (Gtk.Stock.Delete, IconSize.Button), GettextCatalog.GetString ("Delete Task"));
			delButton.IsImportant = true;
			delButton.Clicked += new EventHandler (DeleteUserTaskClicked); 
			delButton.SetTooltip (tips, GettextCatalog.GetString ("Delete Task"), GettextCatalog.GetString ("Delete Task"));

			Services.TaskService.UserTasksChanged += (EventHandler) DispatchService.GuiDispatch (new EventHandler (UserTasksChanged));
			IdeApp.Workspace.FirstWorkspaceItemOpened += CombineOpened;
			IdeApp.Workspace.LastWorkspaceItemClosed += CombineClosed;
			PropertyService.PropertyChanged += (EventHandler<PropertyChangedEventArgs>) DispatchService.GuiDispatch (new EventHandler<PropertyChangedEventArgs> (OnPropertyUpdated));	
			ValidateButtons ();
			// Initialize with existing tags.
			UserTasksChanged (this, EventArgs.Empty);
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
		
		void UserTasksChanged (object sender, EventArgs e)
		{
			store.Clear ();
			foreach (UserTask task in IdeApp.Services.TaskService.UserTasks)
			{
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
						UserTask task = (UserTask) store.GetValue (iter, (int)Columns.UserTask);
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
			UserTask task = new UserTask ();
			IdeApp.Services.TaskService.UserTasks.Add (task);
			TreeIter iter = store.AppendValues (GettextCatalog.GetString (Enum.GetName (typeof (TaskPriority), task.Priority)), task.Completed, task.Description, task, GetColorByPriority (task.Priority), task.Completed ? (int)Pango.Weight.Light : (int)Pango.Weight.Bold);
			view.Selection.SelectIter (iter);
			TreePath path = store.GetPath (iter);
			view.ScrollToCell (path, view.Columns[(int)Columns.Description], false, 0, 0);
		}
		
		void DeleteUserTaskClicked (object obj, EventArgs e)
		{
			if (view.Selection.CountSelectedRows () > 0)
			{
				TreeIter iter;
				if (store.GetIter (out iter, view.Selection.GetSelectedRows ()[0]))
				{
					UserTask task = (UserTask) store.GetValue (iter, (int)Columns.UserTask);
					IdeApp.Services.TaskService.UserTasks.Remove (task);
					store.Remove (ref iter);
				}
			}
		}
		
		void UserTaskPriorityEdited (object o, ComboSelectionChangedArgs args)
		{
			Gtk.TreeIter iter;
			if (store.GetIterFromString (out iter,  args.Path)) {
				UserTask task = (UserTask) store.GetValue (iter, (int)Columns.UserTask);
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
				UserTask task = (UserTask) store.GetValue (iter, (int)Columns.UserTask);
				task.Completed = !val;
				store.SetValue (iter, (int)Columns.Completed, !val);
				store.SetValue (iter, (int)Columns.Bold, task.Completed ? (int)Pango.Weight.Light : (int)Pango.Weight.Bold);
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
				UserTask task = (UserTask) store.GetValue (iter, (int)Columns.UserTask);
				task.Description = args.NewText;
				store.SetValue (iter, (int)Columns.Description, args.NewText);
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
		
		[GLib.ConnectBefore]
		void OnUserButtonPressed (object o, ButtonPressEventArgs args)
		{
			if (args.Event.Button == 3)
				ShowUserPopup ();
		}
		
		void OnUserPopupMenu (object o, PopupMenuArgs args)
		{
			ShowUserPopup ();
		}

		void ShowUserPopup ()
		{
			Menu menu = new Menu ();
			menu.AccelGroup = new AccelGroup ();
			ImageMenuItem copy = new ImageMenuItem (Gtk.Stock.Copy, menu.AccelGroup);
			copy.Activated += new EventHandler (OnUserTaskCopied);
			menu.Append (copy);
			menu.Popup (null, null, null, 3, Gtk.Global.CurrentEventTime);
			menu.ShowAll ();
		}

		void OnUserTaskCopied (object o, EventArgs args)
		{
			UserTask task;
			TreeModel model;
			TreeIter iter;

			if (view.Selection.GetSelected (out model, out iter))
			{
				task = (UserTask) model.GetValue (iter, (int)Columns.UserTask);
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
		ToolItem[] ITaskListView.ToolBarItems { get { return new ToolItem[] { newButton, delButton }; } }
		#endregion
	}
}
