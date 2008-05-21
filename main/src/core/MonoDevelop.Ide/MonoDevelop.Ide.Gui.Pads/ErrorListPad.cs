//  ErrorListPad.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Drawing;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

using MonoDevelop.Core.Gui;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Tasks;

using Gtk;

namespace MonoDevelop.Ide.Gui.Pads
{
	public class ErrorListPad : IPadContent, ILocationListPad
	{
		VBox control;
		ScrolledWindow sw;
		Gtk.TreeView view;
		ListStore store;
		TreeModelFilter filter;
		ToggleToolButton errorBtn, warnBtn, msgBtn;
		Gtk.Tooltips tips = new Gtk.Tooltips ();
		Hashtable tasks = new Hashtable ();
		IPadWindow window;
		bool initializeLocation = true;

		Menu menu;
		Dictionary<ToggleAction, int> columnsActions = new Dictionary<ToggleAction, int> ();
		Clipboard clipboard;

		Gdk.Pixbuf iconWarning;
		Gdk.Pixbuf iconError;
		Gdk.Pixbuf iconInfo;
		Gdk.Pixbuf iconQuestion;
		
		const string showErrorsPropertyName = "SharpDevelop.TaskList.ShowErrors";
		const string showWarningsPropertyName = "SharpDevelop.TaskList.ShowWarnings";
		const string showMessagesPropertyName = "SharpDevelop.TaskList.ShowMessages";

		enum Columns
		{
			Type,
			Marked,
			Line,
			Description,
			File,
			Path,
			Task,
			Read,
			Weight,
			Count
		}

		void IPadContent.Initialize (IPadWindow window)
		{
			this.window = window;
			window.Title = GettextCatalog.GetString ("Error List");
			window.Icon = MonoDevelop.Core.Gui.Stock.Error;
		}
		
		public Gtk.Widget Control {
			get {
				return control;
			}
		}

		public string Id {
			get { return "MonoDevelop.Ide.Gui.Pads.ErrorListPad"; }
		}
		
		public void RedrawContent()
		{
			// FIXME
		}

		public ErrorListPad ()
		{
			control = new VBox ();

			Toolbar toolbar = new Toolbar ();
			toolbar.IconSize = IconSize.Menu;
			control.PackStart (toolbar, false, false, 0);
			
			errorBtn = new ToggleToolButton ();
			UpdateErrorsNum();
			errorBtn.Active = (bool)PropertyService.Get (showErrorsPropertyName, true);
			errorBtn.IconWidget = new Gtk.Image (Gtk.Stock.DialogError, Gtk.IconSize.Button);
			errorBtn.IsImportant = true;
			errorBtn.Toggled += new EventHandler (FilterChanged);
			errorBtn.SetTooltip (tips, GettextCatalog.GetString ("Show Errors"), GettextCatalog.GetString ("Show Errors"));
			toolbar.Insert (errorBtn, -1);
			
			toolbar.Insert (new SeparatorToolItem (), -1);

			warnBtn = new ToggleToolButton ();
			UpdateWarningsNum();
			warnBtn.Active = (bool)PropertyService.Get (showWarningsPropertyName, true);
			warnBtn.IconWidget = new Gtk.Image (Gtk.Stock.DialogWarning, Gtk.IconSize.Button);
			warnBtn.IsImportant = true;
			warnBtn.Toggled += new EventHandler (FilterChanged);
			warnBtn.SetTooltip (tips, GettextCatalog.GetString ("Show Warnings"), GettextCatalog.GetString ("Show Warnings"));
			toolbar.Insert (warnBtn, -1);
			
			toolbar.Insert (new SeparatorToolItem (), -1);

			msgBtn = new ToggleToolButton ();
			UpdateMessagesNum();
			msgBtn.Active = (bool)PropertyService.Get (showMessagesPropertyName, true);
			msgBtn.IconWidget = new Gtk.Image (Gtk.Stock.DialogInfo, Gtk.IconSize.Button);
			msgBtn.IsImportant = true;
			msgBtn.Toggled += new EventHandler (FilterChanged);
			msgBtn.SetTooltip (tips, GettextCatalog.GetString ("Show Messages"), GettextCatalog.GetString ("Show Messages"));
			toolbar.Insert (msgBtn, -1);
			
			store = new Gtk.ListStore (typeof (Gdk.Pixbuf), // image - type
			                           typeof (bool),       // marked?
			                           typeof (int),        // line
			                           typeof (string),     // desc
			                           typeof (string),     // file
			                           typeof (string),     // path
			                           typeof (Task),       // task
			                           typeof (bool),       // read?
			                           typeof (int));       // read? -- use Pango weight

			TreeIterCompareFunc sortFunc = new TreeIterCompareFunc (TaskSortFunc);
			store.SetSortFunc ((int)Columns.Task, sortFunc);
			store.DefaultSortFunc = sortFunc;
			store.SetSortColumnId ((int)Columns.Task, SortType.Ascending);
			
			TreeModelFilterVisibleFunc filterFunct = new TreeModelFilterVisibleFunc (FilterTaskTypes);
			filter = new TreeModelFilter (store, null);
            filter.VisibleFunc = filterFunct;
			
			view = new Gtk.TreeView (filter);
			view.RulesHint = true;
			view.PopupMenu += new PopupMenuHandler (OnPopupMenu);
			view.ButtonPressEvent += new ButtonPressEventHandler (OnButtonPressed);
			view.HeadersClickable = true;
			AddColumns ();
			LoadColumnsVisibility ();
			
			sw = new Gtk.ScrolledWindow ();
			sw.ShadowType = ShadowType.None;
			sw.Add (view);
			
			Services.TaskService.TaskRemoved      += (TaskEventHandler) DispatchService.GuiDispatch (new TaskEventHandler (ShowResults));
			Services.TaskService.TaskAdded        += (TaskEventHandler) DispatchService.GuiDispatch (new TaskEventHandler (TaskAdded));
			Services.TaskService.TaskChanged      += (TaskEventHandler) DispatchService.GuiDispatch (new TaskEventHandler (TaskChanged));
			
			IdeApp.Workspace.FirstWorkspaceItemOpened += OnCombineOpen;
			IdeApp.Workspace.LastWorkspaceItemClosed += OnCombineClosed;
			
			view.RowActivated            += new RowActivatedHandler (OnRowActivated);
						
			iconWarning = sw.RenderIcon (Gtk.Stock.DialogWarning, Gtk.IconSize.Menu, "");
			iconError = sw.RenderIcon (Gtk.Stock.DialogError, Gtk.IconSize.Menu, "");
			iconInfo = sw.RenderIcon (Gtk.Stock.DialogInfo, Gtk.IconSize.Menu, "");
			iconQuestion = sw.RenderIcon (Gtk.Stock.DialogQuestion, Gtk.IconSize.Menu, "");
			
			control.Add (sw);
			toolbar.ToolbarStyle = ToolbarStyle.BothHoriz;
			toolbar.ShowArrow = false;
			Control.ShowAll ();
			
			CreateMenu ();

			// Load existing tasks
			foreach (Task t in Services.TaskService.Tasks) {
				AddTask (t);
			}
		}
		
		void LoadColumnsVisibility ()
		{
			string columns = (string)PropertyService.Get ("Monodevelop.ErrorListColumns", "TRUE;TRUE;TRUE;TRUE;TRUE;TRUE");
			string[] tokens = columns.Split (new char[] {';'}, StringSplitOptions.RemoveEmptyEntries);
			if (tokens.Length == 6 && view != null && view.Columns.Length == 6)
			{
				for (int i = 0; i < 6; i++)
				{
					bool visible;
					if (bool.TryParse (tokens[i], out visible))
						view.Columns[i].Visible = visible;
				}
			}
		}

		void StoreColumnsVisibility ()
		{
			string columns = String.Format ("{0};{1};{2};{3};{4};{5}",
			                                view.Columns[(int)Columns.Type].Visible,
			                                view.Columns[(int)Columns.Marked].Visible,
			                                view.Columns[(int)Columns.Line].Visible,
			                                view.Columns[(int)Columns.Description].Visible,
			                                view.Columns[(int)Columns.File].Visible,
			                                view.Columns[(int)Columns.Path].Visible);
			PropertyService.Set ("Monodevelop.ErrorListColumns", columns);
		}

		void CreateMenu ()
		{
			if (menu == null)
			{
				ActionGroup group = new ActionGroup ("Popup");

				Gtk.Action help = new Gtk.Action ("help", GettextCatalog.GetString ("Show Error Reference"),
				                          GettextCatalog.GetString ("Show Error Reference"), Gtk.Stock.Help);
				help.Activated += new EventHandler (OnShowReference);
				group.Add (help, "F1");

				Gtk.Action copy = new Gtk.Action ("copy", GettextCatalog.GetString ("_Copy"),
				                          GettextCatalog.GetString ("Copy task"), Gtk.Stock.Copy);
				copy.Activated += new EventHandler (OnTaskCopied);
				group.Add (copy, "<Control><Mod2>c");

				Gtk.Action jump = new Gtk.Action ("jump", GettextCatalog.GetString ("_Go to"),
				                          GettextCatalog.GetString ("Go to task"), Gtk.Stock.JumpTo);
				jump.Activated += new EventHandler (OnTaskJumpto);
				group.Add (jump);

				Gtk.Action columns = new Gtk.Action ("columns", GettextCatalog.GetString ("Columns"));
				group.Add (columns, null);

				ToggleAction columnType = new ToggleAction ("columnType", GettextCatalog.GetString ("Type"),
				                                            GettextCatalog.GetString ("Toggle visibility of Type column"), null);
				columnType.Toggled += new EventHandler (OnColumnVisibilityChanged);
				columnsActions[columnType] = (int)Columns.Type;
				group.Add (columnType);

				ToggleAction columnValidity = new ToggleAction ("columnValidity", GettextCatalog.GetString ("Validity"),
				                                                GettextCatalog.GetString ("Toggle visibility of Validity column"), null);
				columnValidity.Toggled += new EventHandler (OnColumnVisibilityChanged);
				columnsActions[columnValidity] = (int)Columns.Marked;
				group.Add (columnValidity);

				ToggleAction columnLine = new ToggleAction ("columnLine", GettextCatalog.GetString ("Line"),
				                                            GettextCatalog.GetString ("Toggle visibility of Line column"), null);
				columnLine.Toggled += new EventHandler (OnColumnVisibilityChanged);
				columnsActions[columnLine] = (int)Columns.Line;
				group.Add (columnLine);

				ToggleAction columnDescription = new ToggleAction ("columnDescription", GettextCatalog.GetString ("Description"),
				                                                   GettextCatalog.GetString ("Toggle visibility of Description column"), null);
				columnDescription.Toggled += new EventHandler (OnColumnVisibilityChanged);
				columnsActions[columnDescription] = (int)Columns.Description;
				group.Add (columnDescription);

				ToggleAction columnFile = new ToggleAction ("columnFile", GettextCatalog.GetString ("File"),
				                                            GettextCatalog.GetString ("Toggle visibility of File column"), null);
				columnFile.Toggled += new EventHandler (OnColumnVisibilityChanged);
				columnsActions[columnFile] = (int)Columns.File;
				group.Add (columnFile);

				ToggleAction columnPath = new ToggleAction ("columnPath", GettextCatalog.GetString ("Path"),
				                                            GettextCatalog.GetString ("Toggle visibility of Path column"), null);
				columnPath.Toggled += new EventHandler (OnColumnVisibilityChanged);
				columnsActions[columnPath] = (int)Columns.Path;
				group.Add (columnPath);

				UIManager uiManager = new UIManager ();
				uiManager.InsertActionGroup (group, 0);
				
				string uiStr = "<ui><popup name='popup'>"
					+ "<menuitem action='help'/>"
					+ "<menuitem action='copy'/>"
					+ "<menuitem action='jump'/>"
					+ "<separator/>"
					+ "<menu action='columns'>"
					+ "<menuitem action='columnType' />"
					+ "<menuitem action='columnValidity' />"
					+ "<menuitem action='columnLine' />"
					+ "<menuitem action='columnDescription' />"
					+ "<menuitem action='columnFile' />"
					+ "<menuitem action='columnPath' />"
					+ "</menu>"
					+ "</popup></ui>";

				uiManager.AddUiFromString (uiStr);
				menu = (Menu)uiManager.GetWidget ("/popup");
				menu.ShowAll ();

				menu.Shown += delegate (object o, EventArgs args)
				{
					columnType.Active = view.Columns[(int)Columns.Type].Visible;
					columnValidity.Active = view.Columns[(int)Columns.Marked].Visible;
					columnLine.Active = view.Columns[(int)Columns.Line].Visible;
					columnDescription.Active = view.Columns[(int)Columns.Description].Visible;
					columnFile.Active = view.Columns[(int)Columns.File].Visible;
					columnPath.Active = view.Columns[(int)Columns.Path].Visible;
					help.Sensitive = copy.Sensitive = jump.Sensitive =
						view.Selection != null &&
						view.Selection.CountSelectedRows () > 0 &&
						(columnType.Active ||
						columnValidity.Active ||
						columnLine.Active ||
						columnDescription.Active ||
						columnFile.Active ||
						columnPath.Active);
				};
			}
		}


		[GLib.ConnectBefore]
		void OnButtonPressed (object o, ButtonPressEventArgs args)
		{
			if (args.Event.Button == 3)
				menu.Popup ();
		}

		void OnPopupMenu (object o, PopupMenuArgs args)
		{
			menu.Popup ();
		}

		Task SelectedTask
		{
			get {
				TreeModel model;
				TreeIter iter;
				if (view.Selection.GetSelected (out model, out iter))
				{
					return (Task)model.GetValue (iter, (int)Columns.Task);
				}
				else return null; // no one selected
			}
		}

		void OnTaskCopied (object o, EventArgs args)
		{
			Task task = SelectedTask;
			if (task != null) {
				clipboard = Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
				clipboard.Text = task.ToString ();
				clipboard = Clipboard.Get (Gdk.Atom.Intern ("PRIMARY", false));
				clipboard.Text = task.ToString ();
			}
		}

		void OnShowReference (object o, EventArgs args)
		{
			string reference = null;
			if (GetSelectedErrorReference (out reference)) {
				IdeApp.HelpOperations.ShowHelp ("error:" + reference);
				return;
			}
		}

		bool GetSelectedErrorReference (out string reference)
		{
			Task task = SelectedTask;
			if (task != null && !String.IsNullOrEmpty (task.ErrorNumber)) {
				reference = task.ErrorNumber;
				return true;
			}
			reference = null;
			return false;
		}

		void OnTaskJumpto (object o, EventArgs args)
		{
			TreeIter iter;
			TreeModel model;
			if (view.Selection.GetSelected (out model, out iter)) {
				iter = filter.ConvertIterToChildIter (iter);
				store.SetValue (iter, (int)Columns.Weight, (int) Pango.Weight.Normal);
				Task task = (Task) store.GetValue (iter, (int)Columns.Task);
				if (task != null)
					task.JumpToPosition ();
			}
		}

		void OnColumnVisibilityChanged (object o, EventArgs args)
		{
			ToggleAction action = o as ToggleAction;
			if (action != null)
			{
				view.Columns[columnsActions[action]].Visible = action.Active;
				StoreColumnsVisibility ();
			}
		}

		void AddColumns ()
		{
			Gtk.CellRendererPixbuf iconRender = new Gtk.CellRendererPixbuf ();
			
			Gtk.CellRendererToggle toggleRender = new Gtk.CellRendererToggle ();
			toggleRender.Toggled += new ToggledHandler (ItemToggled);

			Gtk.CellRendererText line = new Gtk.CellRendererText (), desc = new Gtk.CellRendererText () , path = new Gtk.CellRendererText (),
			file = new Gtk.CellRendererText ();

			TreeViewColumn col;
			col = view.AppendColumn ("!", iconRender, "pixbuf", Columns.Type);
			col.Clickable = true;
			col.Clicked += new EventHandler (OnResortTasks);
			col.SortIndicator = true;
			view.AppendColumn ("", toggleRender, "active", Columns.Marked);
			view.AppendColumn (GettextCatalog.GetString ("Line"), line, "text", Columns.Line, "weight", Columns.Weight);
			col = view.AppendColumn (GettextCatalog.GetString ("Description"), desc, "text", Columns.Description, "weight", Columns.Weight, "strikethrough", Columns.Marked);
			col.Resizable = true;
			col = view.AppendColumn (GettextCatalog.GetString ("File"), file, "text", Columns.File, "weight", Columns.Weight);
			col.Resizable = true;
			col = view.AppendColumn (GettextCatalog.GetString ("Path"), path, "text", Columns.Path, "weight", Columns.Weight);
			col.Resizable = true;
		}
		
		void OnCombineOpen(object sender, EventArgs e)
		{
			Clear();
		}
		
		void OnCombineClosed(object sender, EventArgs e)
		{
			Clear();
		}
		
		public void Dispose ()
		{
		}
		
		void SelectTaskView (object s, BuildEventArgs args)
		{
			if (Services.TaskService.Tasks.Count > 0) {
				try {
					if (window.Visible)
						window.Activate ();
					else if ((bool) PropertyService.Get ("SharpDevelop.ShowTaskListAfterBuild", true)) {
						window.Visible = true;
						window.Activate ();
					}
				} catch {}
			}
		}
		
		void OnRowActivated (object o, RowActivatedArgs args)
		{
			OnTaskJumpto (null, null);
		}
		
		public CompilerResults CompilerResults = null;
		
		void FilterChanged (object sender, EventArgs e)
		{
			
			PropertyService.Set (showErrorsPropertyName, errorBtn.Active);
			PropertyService.Set (showWarningsPropertyName, warnBtn.Active);
			PropertyService.Set (showMessagesPropertyName, msgBtn.Active);
			
			filter.Refilter ();
		}

		bool FilterTaskTypes (TreeModel model, TreeIter iter)
		{
			bool canShow = false;

			try {
				Task task = (Task) store.GetValue (iter, (int)Columns.Task);
				if (task == null)
					return true;
				if (task.TaskType == TaskType.Error && errorBtn.Active) canShow = true;
				else if (task.TaskType == TaskType.Warning && warnBtn.Active) canShow = true;
				else if (task.TaskType == TaskType.Comment && msgBtn.Active) canShow = true;
			} catch {
				//Not yet fully added
				return false;
			}
			
			return canShow;
		}

		public void ShowResults (object sender, EventArgs e)
		{
			Clear();

			foreach (Task t in Services.TaskService.Tasks) {
				AddTask (t);
			}
			SelectTaskView (null, null);
		}

		private void Clear()
		{
			store.Clear ();
			tasks.Clear ();
			UpdateErrorsNum ();
			UpdateWarningsNum ();
			UpdateMessagesNum ();
			initializeLocation = true;
		}
		
				
		void TaskChanged (object sender, TaskEventArgs e)
		{
			TreeIter iter;
			if (store.GetIterFirst (out iter)) {
				do {
					Task curTask = (Task)store.GetValue (iter, 6);
					foreach (Task task in e.Tasks) {
						if (task == curTask) {
							store.SetValue (iter, 2, task.Line);
						}
					}
					
				} while (store.IterNext (ref iter));
			}
		}
	
		void TaskAdded (object sender, TaskEventArgs e)
		{
			AddTasks (e.Tasks);
		}
		
		public void AddTasks (IEnumerable<Task> tasks)
		{
			int n = 1;
			foreach (Task t in tasks) {
				AddTaskInternal (t);
				if ((n++ % 100) == 0) {
					// Adding many tasks is a bit slow, so refresh the
					// ui at every block of 100.
					DispatchService.RunPendingEvents ();
				}
			}
			filter.Refilter ();
		}
		
		public void AddTask (Task t)
		{
			AddTaskInternal (t);
			filter.Refilter ();
		}
		
		void AddTaskInternal (Task t)
		{
			if (t.TaskType == TaskType.Comment) 
				return;
				
			if (tasks.Contains (t)) return;
			
			switch (t.TaskType) {
				case TaskType.Error:
					UpdateErrorsNum ();
					break; 
				case TaskType.Warning:
					UpdateWarningsNum ();	
					break;
				default:
					UpdateMessagesNum ();
					break;
			}
		
			tasks [t] = t;
			
			Gdk.Pixbuf stock;
			switch (t.TaskType) {
				case TaskType.Warning:
					stock = iconWarning;
					break;
				case TaskType.Error:
					stock = iconError;
					break;
				case TaskType.Comment:
					stock = iconInfo;
					break;
				case TaskType.SearchResult:
					stock = iconQuestion;
					break;
				default:
					stock = null;
					break;
			}
			
			string tmpPath = t.FileName;
			if (t.Project != null)
				tmpPath = FileService.AbsoluteToRelativePath (t.Project.BaseDirectory, t.FileName);
			
			string fileName = tmpPath;
			string path     = tmpPath;
			
			try {
				fileName = Path.GetFileName(tmpPath);
			} catch (Exception) {}
			
			try {
				path = Path.GetDirectoryName(tmpPath);
			} catch (Exception) {}
			
			store.AppendValues (stock,
			                    false,
			                    t.Line,
			                    t.Description,
			                    fileName,
			                    path,
			                    t,
			                    false,
			                    (int) Pango.Weight.Bold);
		}

		void UpdateErrorsNum () 
		{
			errorBtn.Label = " " + string.Format(GettextCatalog.GetPluralString("{0} Error", "{0} Errors", IdeApp.Services.TaskService.ErrorsCount), IdeApp.Services.TaskService.ErrorsCount);
		}

		void UpdateWarningsNum ()
		{
			warnBtn.Label = " " + string.Format(GettextCatalog.GetPluralString("{0} Warning", "{0} Warnings", IdeApp.Services.TaskService.WarningsCount), IdeApp.Services.TaskService.WarningsCount); 
		}

		void UpdateMessagesNum ()
		{
			msgBtn.Label = " " + string.Format(GettextCatalog.GetPluralString("{0} Message", "{0} Messages", IdeApp.Services.TaskService.MessagesCount), IdeApp.Services.TaskService.MessagesCount);
		}

		private void ItemToggled (object o, ToggledArgs args)
		{
			Gtk.TreeIter iter;
			if (store.GetIterFromString(out iter, args.Path)) {
				bool val = (bool) store.GetValue(iter, (int)Columns.Marked);
				store.SetValue(iter, (int)Columns.Marked, !val);
			}
		}

		private SortType ReverseSortOrder (TreeViewColumn col) {
			if (col.SortIndicator) {
				if (col.SortOrder == SortType.Ascending)
					return SortType.Descending;
				else
					return SortType.Ascending;
			} else {
				return SortType.Ascending;
			}
		}

		private void OnResortTasks (object sender, EventArgs args)
		{
			TreeViewColumn col = sender as TreeViewColumn;
			col.SortOrder = ReverseSortOrder (col);
			col.SortIndicator = true;
			store.SetSortColumnId ((int)Columns.Task, col.SortOrder);
		}

		private int TaskSortFunc (TreeModel model, TreeIter iter1, TreeIter iter2)
		{
			Task task1 = model.GetValue (iter1, (int)Columns.Task) as Task;
			Task task2 = model.GetValue (iter2, (int)Columns.Task) as Task;

			if (task1 == null && task2 == null) return 0;
			else if (task1 == null) return -1;
			else if (task2 == null) return 1;

			int compare = ((int)task1.TaskType).CompareTo ((int)task2.TaskType);
			if (compare != 0)
				return compare;

			if (task1.FileName != null || task2.FileName != null) {
				if (task1.FileName == null) return -1;
				if (task2.FileName == null) return 1;
				compare = task1.FileName.CompareTo (task2.FileName);
				if (compare == 0)
					compare = task1.Line.CompareTo (task2.Line);
			}
			return compare;
		}

		public virtual bool GetNextLocation (out string file, out int line, out int column)
		{
			bool hasNext;
			TreeIter iter;
			TreeModel model;
			
			if (!initializeLocation && view.Selection.GetSelected (out model, out iter)) {
				hasNext = model.IterNext (ref iter);
			} else {
				model = view.Model;
				hasNext = model.GetIterFirst (out iter);
				initializeLocation = false;
			}
			
			if (!hasNext) {
				file = null;
				line = 0;
				column = 0;
				view.Selection.UnselectAll ();
				return false;
			} else {
				view.Selection.SelectIter (iter);
				Task t = (Task) model.GetValue (iter, (int)Columns.Task);
				file = t.FileName;
				if (file == null)
					return GetNextLocation (out file, out line, out column);
				line = t.Line;
				column = t.Column;
				view.ScrollToCell (view.Model.GetPath (iter), view.Columns[0], false, 0, 0);
				iter = filter.ConvertIterToChildIter (iter);
				store.SetValue (iter, (int)Columns.Weight, (int) Pango.Weight.Normal);
				return true;
			}
		}

		public virtual bool GetPreviousLocation (out string file, out int line, out int column)
		{
			bool hasNext, hasSel;
			TreeIter iter;
			TreeIter selIter = TreeIter.Zero;
			TreeIter prevIter = TreeIter.Zero;
			
			hasSel = !initializeLocation && view.Selection.GetSelected (out selIter);
			hasNext = view.Model.GetIterFirst (out iter);
			initializeLocation = false;
			
			while (hasNext) {
				if (hasSel && iter.Equals (selIter))
					break;
				prevIter = iter;
				hasNext = view.Model.IterNext (ref iter);
			}
			
			if (prevIter.Equals (TreeIter.Zero)) {
				file = null;
				line = 0;
				column = 0;
				view.Selection.UnselectAll ();
				return false;
			} else {
				view.Selection.SelectIter (prevIter);
				Task t = (Task) view.Model.GetValue (prevIter, (int)Columns.Task);
				file = t.FileName;
				if (file == null)
					return GetPreviousLocation (out file, out line, out column);
				line = t.Line;
				column = t.Column;
				view.ScrollToCell (view.Model.GetPath (prevIter), view.Columns[0], false, 0, 0);
				prevIter = filter.ConvertIterToChildIter (prevIter);
				store.SetValue (prevIter, (int)Columns.Weight, (int) Pango.Weight.Normal);
				return true;
			}
		}
	}
}
