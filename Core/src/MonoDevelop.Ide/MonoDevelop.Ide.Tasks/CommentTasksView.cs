//
// CommentTasksView.cs
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
using MonoDevelop.Core.Properties;
using MonoDevelop.Projects;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Tasks
{
	internal class CommentTasksView : ITaskListView
	{
		enum Columns
		{
			Line,
			Description,
			File,
			Path,
			Task,
			Foreground,
			Bold,
			Count
		}
		
		TreeView view;
		ListStore store;
		Hashtable tasks = new Hashtable ();
		
		Gdk.Color highPrioColor, normalPrioColor, lowPrioColor;
		
		Clipboard clipboard;
		bool solutionLoaded = false;
		
		public CommentTasksView ()
		{
			highPrioColor = StringToColor ((string)Runtime.Properties.GetProperty ("Monodevelop.UserTasksHighPrioColor", ""));
			normalPrioColor = StringToColor ((string)Runtime.Properties.GetProperty ("Monodevelop.UserTasksNormalPrioColor", ""));
			lowPrioColor = StringToColor ((string)Runtime.Properties.GetProperty ("Monodevelop.UserTasksLowPrioColor", ""));
			
			store = new Gtk.ListStore (
				typeof (int),        // line
				typeof (string),     // desc
				typeof (string),     // file
				typeof (string),     // path
				typeof (Task),		 // task
				typeof (Gdk.Color),  // foreground color
				typeof (int));		 // font weight

			
			view = new Gtk.TreeView (store);
			view.RulesHint = true;
			view.SearchColumn = (int)Columns.Description;
			view.PopupMenu += new PopupMenuHandler (OnPopupMenu);
			view.ButtonPressEvent += new ButtonPressEventHandler (OnButtonPressed);
			view.RowActivated += new RowActivatedHandler (OnRowActivated);
			
			TreeViewColumn col;
			col = view.AppendColumn (GettextCatalog.GetString ("Line"), new CellRendererText (), "text", Columns.Line, "foreground-gdk", Columns.Foreground, "weight", Columns.Bold);
			col.Clickable = false;

			col = view.AppendColumn (GettextCatalog.GetString ("Description"), new CellRendererText (), "text", Columns.Description, "foreground-gdk", Columns.Foreground, "weight", Columns.Bold);
			col.Clickable = true;
			col.SortColumnId = (int)Columns.Description;
			col.Resizable = true;
			col.Clicked += new EventHandler (Resort);
			
			col = view.AppendColumn (GettextCatalog.GetString ("File"), new CellRendererText (), "text", Columns.File, "foreground-gdk", Columns.Foreground, "weight", Columns.Bold);
			col.Clickable = true;
			col.SortColumnId = (int)Columns.File;
			col.Resizable = true;
			col.Clicked += new EventHandler (Resort);
			
			col = view.AppendColumn (GettextCatalog.GetString ("Path"), new CellRendererText (), "text", Columns.Path, "foreground-gdk", Columns.Foreground, "weight", Columns.Bold);
			col.Clickable = true;
			col.SortColumnId = (int)Columns.Path;
			col.Resizable = true;
			col.Clicked += new EventHandler (Resort);
			
			Services.TaskService.TasksCleared += (EventHandler) Services.DispatchService.GuiDispatch (new EventHandler (GeneratedTasksCleared));
			Services.TaskService.TaskAdded += (TaskEventHandler) Services.DispatchService.GuiDispatch (new TaskEventHandler (GeneratedTaskAdded));
			Services.TaskService.TaskRemoved += (TaskEventHandler) Services.DispatchService.GuiDispatch (new TaskEventHandler (GeneratedTaskRemoved));
			
			IdeApp.ProjectOperations.CombineOpened += (CombineEventHandler) Services.DispatchService.GuiDispatch (new CombineEventHandler (CombineOpened));
			IdeApp.ProjectOperations.CombineClosed += (CombineEventHandler) Services.DispatchService.GuiDispatch (new CombineEventHandler (CombineClosed));
			Runtime.Properties.PropertyChanged += (PropertyEventHandler) Services.DispatchService.GuiDispatch (new PropertyEventHandler (OnPropertyUpdated));
		}
		
		void OnRowActivated (object o, RowActivatedArgs args)
		{
			Gtk.TreeIter iter;
			if (store.GetIter (out iter, args.Path)) {
				((Task) store.GetValue (iter, (int)Columns.Task)).JumpToPosition ();
			}
		}

		void GeneratedTasksCleared (object sender, EventArgs e)
		{
			store.Clear ();
			tasks.Clear ();
		}

		void GeneratedTaskAdded (object sender, TaskEventArgs e)
		{
			if (e.Task.TaskType == TaskType.Comment)
				AddGeneratedTask (e.Task);
		}
		
		void AddGeneratedTask (Task t)
		{
			if (tasks.Contains (t)) return;
			
			tasks [t] = t;
			
			string tmpPath = t.FileName;
			if (t.Project != null)
				tmpPath = Runtime.FileUtilityService.AbsoluteToRelativePath (t.Project.BaseDirectory, t.FileName);
			
			string fileName = tmpPath;
			string path     = tmpPath;
			
			try {
				fileName = Path.GetFileName (tmpPath);
			} catch (Exception) {}
			
			try {
				path = Path.GetDirectoryName (tmpPath);
			} catch (Exception) {}
			
			store.AppendValues (t.Line, t.Description, fileName, path, t, GetColorByPriority (t.Priority), (int)Pango.Weight.Bold);
		}
		
		void GeneratedTaskRemoved (object sender, TaskEventArgs e)
		{
			if (e.Task.TaskType == TaskType.Comment)
				RemoveGeneratedTask (e.Task);
		}
		
		void RemoveGeneratedTask (Task t)
		{
			if (!tasks.Contains (t)) return;
			
			tasks[t] = null;
			
			TreeIter iter = FindTask (store, t);
			if (!iter.Equals (TreeIter.Zero))
				store.Remove (ref iter);
		}
		
		static TreeIter FindTask (ListStore store, Task task)
		{
			TreeIter iter;
			store.GetIterFirst (out iter);
			Task t = store.GetValue (iter, (int)Columns.Task) as Task;
			if (t != null && t == task) {
				return iter;
			}	
			while (store.IterNext (ref iter)) {
				t = store.GetValue (iter, (int)Columns.Task) as Task;
				if (t != null && t == task) {
					return iter;
				}
			}
			return TreeIter.Zero;
		}
		
		[GLib.ConnectBefore]
		void OnButtonPressed (object o, ButtonPressEventArgs args)
		{
			if (args.Event.Button == 3)
				ShowGenPopup ();
		}

		void OnPopupMenu (object o, PopupMenuArgs args)
		{
			ShowGenPopup ();
		}

		void ShowGenPopup ()
		{
			Menu menu = new Menu ();
			menu.AccelGroup = new AccelGroup ();
			ImageMenuItem copy = new ImageMenuItem (Gtk.Stock.Copy, menu.AccelGroup);
			copy.Activated += new EventHandler (OnGenTaskCopied);
			menu.Append (copy);
			menu.Popup (null, null, null, 3, Gtk.Global.CurrentEventTime);
			menu.ShowAll ();
		}

		void OnGenTaskCopied (object o, EventArgs args)
		{
			Task task;
			TreeModel model;
			TreeIter iter;

			if (view.Selection.GetSelected (out model, out iter))
			{
				task = (Task) model.GetValue (iter, (int)Columns.Task);
			}
			else return; // no one selected

			clipboard = Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
			clipboard.Text = task.ToString ();
			clipboard = Clipboard.Get (Gdk.Atom.Intern ("PRIMARY", false));
			clipboard.Text = task.ToString ();
		}
		
		void Resort (object sender, EventArgs args)
		{
			TreeViewColumn col = (TreeViewColumn)sender;
			foreach (TreeViewColumn c in view.Columns)
			{
				if (c != col) c.SortIndicator = false;
			}
			col.SortOrder = ReverseSortOrder (col);
			col.SortIndicator = true;
			store.SetSortColumnId (col.SortColumnId, col.SortOrder);
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
		
		void CombineOpened (object sender, CombineEventArgs e)
		{
			solutionLoaded = true;
		}
		
		void CombineClosed (object sender, CombineEventArgs e)
		{
			solutionLoaded = true;
		}
		
		void OnPropertyUpdated (object sender, PropertyEventArgs e)
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
						Task task = (Task) store.GetValue (iter, (int)Columns.Task);
						store.SetValue (iter, (int)Columns.Foreground, GetColorByPriority (task.Priority));
					} while (store.IterNext (ref iter));
				}
			}
		}
		
		#region ITaskListView members
		TreeView ITaskListView.Content { get { return view; } }
		ToolItem[] ITaskListView.ToolBarItems { get { return null; } }
		#endregion
	}
}
