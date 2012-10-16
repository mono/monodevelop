// ErrorListPad.cs
//  
// Author:
//       Todd Berman <tberman@sevenl.net>
//       David Makovský <yakeen@sannyas-on.net>
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2004 Todd Berman
// Copyright (c) 2006 David Makovský
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.


using System;
using System.Drawing;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Ide.Gui.Content;

using Gtk;
using System.Text;
using MonoDevelop.Components.Docking;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;

namespace MonoDevelop.Ide.Gui.Pads
{
	public class ErrorListPad : IPadContent
	{
		HPaned control;
		ScrolledWindow sw;
		MonoDevelop.Ide.Gui.Components.PadTreeView view;
		LogView outputView;
		ListStore store;
		TreeModelFilter filter;
		TreeModelSort sort;
		ToggleButton errorBtn, warnBtn, msgBtn, logBtn;
		Hashtable tasks = new Hashtable ();
		int errorCount;
		int warningCount;
		int infoCount;
		bool initialLogShow = true;

		Menu menu;
		Dictionary<ToggleAction, int> columnsActions = new Dictionary<ToggleAction, int> ();
		Clipboard clipboard;

		Gdk.Pixbuf iconWarning;
		Gdk.Pixbuf iconError;
		Gdk.Pixbuf iconInfo;
		
		const string showErrorsPropertyName = "SharpDevelop.TaskList.ShowErrors";
		const string showWarningsPropertyName = "SharpDevelop.TaskList.ShowWarnings";
		const string showMessagesPropertyName = "SharpDevelop.TaskList.ShowMessages";
		const string logSeparatorPositionPropertyName = "SharpDevelop.TaskList.LogSeparatorPosition";
		const string outputViewVisiblePropertyName = "SharpDevelop.TaskList.OutputViewVisible";

		static class DataColumns
		{
			internal const int Type = 0;
			internal const int Read = 1;
			internal const int Task = 2;
		}
		
		static class VisibleColumns
		{
			internal const int Type        = 0;
			internal const int Marked      = 1;
			internal const int Line        = 2;
			internal const int Description = 3;
			internal const int File        = 4;
			internal const int Project     = 5;
			internal const int Path        = 6;
		}

		public Gtk.Widget Control {
			get {
				if (control == null)
					CreateControl ();
				return control;
			}
		}

		public string Id {
			get { return "MonoDevelop.Ide.Gui.Pads.ErrorListPad"; }
		}

		void IPadContent.Initialize (IPadWindow window)
		{
			window.Title = GettextCatalog.GetString ("Errors");

			DockItemToolbar toolbar = window.GetToolbar (PositionType.Top);
			
			errorBtn = new ToggleButton ();
			errorBtn.Active = (bool)PropertyService.Get (showErrorsPropertyName, true);
			errorBtn.Image = new Gtk.Image (Stock.Error, Gtk.IconSize.Menu);
			errorBtn.Image.Show ();
			errorBtn.Toggled += new EventHandler (FilterChanged);
			errorBtn.TooltipText = GettextCatalog.GetString ("Show Errors");
			UpdateErrorsNum();
			toolbar.Add (errorBtn);
			
			warnBtn = new ToggleButton ();
			warnBtn.Active = (bool)PropertyService.Get (showWarningsPropertyName, true);
			warnBtn.Image = new Gtk.Image (Stock.Warning, Gtk.IconSize.Menu);
			warnBtn.Image.Show ();
			warnBtn.Toggled += new EventHandler (FilterChanged);
			warnBtn.TooltipText = GettextCatalog.GetString ("Show Warnings");
			UpdateWarningsNum();
			toolbar.Add (warnBtn);
			
			msgBtn = new ToggleButton ();
			msgBtn.Active = (bool)PropertyService.Get (showMessagesPropertyName, true);
			msgBtn.Image = new Gtk.Image (Gtk.Stock.DialogInfo, Gtk.IconSize.Menu);
			msgBtn.Image.Show ();
			msgBtn.Toggled += new EventHandler (FilterChanged);
			msgBtn.TooltipText = GettextCatalog.GetString ("Show Messages");
			UpdateMessagesNum();
			toolbar.Add (msgBtn);
			
			toolbar.Add (new SeparatorToolItem ());
			
			logBtn = new ToggleButton ();
			logBtn.Label = GettextCatalog.GetString ("Build Output");
			logBtn.Image = ImageService.GetImage ("md-message-log", Gtk.IconSize.Menu);
			logBtn.Image.Show ();
			logBtn.TooltipText = GettextCatalog.GetString ("Show build output");
			logBtn.Toggled += HandleLogBtnToggled;
			toolbar.Add (logBtn);
			
			toolbar.ShowAll ();
		}
		
		void CreateControl ()
		{
			control = new HPaned ();

			store = new Gtk.ListStore (typeof (Gdk.Pixbuf), // image - type
			                           typeof (bool),       // read?
			                           typeof (Task));       // read? -- use Pango weight

			TreeModelFilterVisibleFunc filterFunct = new TreeModelFilterVisibleFunc (FilterTaskTypes);
			filter = new TreeModelFilter (store, null);
			filter.VisibleFunc = filterFunct;
			
			sort = new TreeModelSort (filter);
			sort.SetSortFunc (VisibleColumns.Type, SeverityIterSort);
			sort.SetSortFunc (VisibleColumns.Project, ProjectIterSort);
			sort.SetSortFunc (VisibleColumns.File, FileIterSort);
			
			view = new MonoDevelop.Ide.Gui.Components.PadTreeView (sort);
			view.RulesHint = true;
			view.DoPopupMenu = (evnt) => IdeApp.CommandService.ShowContextMenu (view, evnt, CreateMenu ());
			AddColumns ();
			LoadColumnsVisibility ();
			view.Columns[VisibleColumns.Type].SortColumnId = VisibleColumns.Type;
			view.Columns[VisibleColumns.Project].SortColumnId = VisibleColumns.Project;
			view.Columns[VisibleColumns.File].SortColumnId = VisibleColumns.File;
			
			sw = new MonoDevelop.Components.CompactScrolledWindow ();
			sw.ShadowType = ShadowType.None;
			sw.Add (view);
			TaskService.Errors.TasksRemoved      += DispatchService.GuiDispatch<TaskEventHandler> (ShowResults);
			TaskService.Errors.TasksAdded        += DispatchService.GuiDispatch<TaskEventHandler> (TaskAdded);
			TaskService.Errors.TasksChanged      += DispatchService.GuiDispatch<TaskEventHandler> (TaskChanged);
			TaskService.Errors.CurrentLocationTaskChanged += HandleTaskServiceErrorsCurrentLocationTaskChanged;
			
			IdeApp.Workspace.FirstWorkspaceItemOpened += OnCombineOpen;
			IdeApp.Workspace.LastWorkspaceItemClosed += OnCombineClosed;
			
			view.RowActivated += new RowActivatedHandler (OnRowActivated);
			
			iconWarning = sw.RenderIcon (Stock.Warning, Gtk.IconSize.Menu, "");
			iconError = sw.RenderIcon (Stock.Error, Gtk.IconSize.Menu, "");
			iconInfo = sw.RenderIcon (Gtk.Stock.DialogInfo, Gtk.IconSize.Menu, "");
			
			control.Add1 (sw);
			
			outputView = new LogView ();
			control.Add2 (outputView);
			
			Control.ShowAll ();
			
			control.SizeAllocated += HandleControlSizeAllocated;
			
			bool outputVisible = PropertyService.Get<bool> (outputViewVisiblePropertyName, false);
			if (outputVisible) {
				outputView.Visible = true;
				logBtn.Active = true;
			} else {
				outputView.Hide ();
			}
			
			sw.SizeAllocated += HandleSwSizeAllocated;
			
			// Load existing tasks
			foreach (Task t in TaskService.Errors) {
				AddTask (t);
			}

			control.FocusChain = new Gtk.Widget [] { sw };
		}
		
		void HandleSwSizeAllocated (object o, SizeAllocatedArgs args)
		{
			if (!initialLogShow && outputView.Visible) {
				var val = (double) ((double) control.Position / (double) control.Allocation.Width);
				PropertyService.Set (logSeparatorPositionPropertyName, val);
			}
		}
		
		[GLib.ConnectBefore]
		void HandleControlSizeAllocated (object o, SizeAllocatedArgs args)
		{
			if (initialLogShow && outputView.Visible) {
				SetInitialOutputViewSize (args.Allocation.Width);
				initialLogShow = false;
			}
		}
		
		public IProgressMonitor GetBuildProgressMonitor ()
		{
			if (control == null)
				CreateControl ();
			return outputView.GetProgressMonitor ();
		}

		void HandleTaskServiceErrorsCurrentLocationTaskChanged (object sender, EventArgs e)
		{
			if (TaskService.Errors.CurrentLocationTask == null) {
				view.Selection.UnselectAll ();
				return;
			}
			TreeIter it;
			if (!view.Model.GetIterFirst (out it))
				return;
			do {
				Task t = (Task) view.Model.GetValue (it, DataColumns.Task);
				if (t == TaskService.Errors.CurrentLocationTask) {
					view.Selection.SelectIter (it);
					view.ScrollToCell (view.Model.GetPath (it), view.Columns[0], false, 0, 0);
					it = filter.ConvertIterToChildIter (sort.ConvertIterToChildIter (it));
					store.SetValue (it, DataColumns.Read, true);
					return;
				}
			} while (view.Model.IterNext (ref it));
		}
		
		void LoadColumnsVisibility ()
		{
			string columns = (string)PropertyService.Get ("Monodevelop.ErrorListColumns", "TRUE;TRUE;TRUE;TRUE;TRUE;TRUE;TRUE");
			string[] tokens = columns.Split (new char[] {';'}, StringSplitOptions.RemoveEmptyEntries);
			if (tokens.Length == 7 && view != null && view.Columns.Length == 7)
			{
				for (int i = 0; i < 7; i++)
				{
					bool visible;
					if (bool.TryParse (tokens[i], out visible))
						view.Columns[i].Visible = visible;
				}
			}
		}

		void StoreColumnsVisibility ()
		{
			string columns = String.Format ("{0};{1};{2};{3};{4};{5};{6}",
			                                view.Columns[VisibleColumns.Type].Visible,
			                                view.Columns[VisibleColumns.Marked].Visible,
			                                view.Columns[VisibleColumns.Line].Visible,
			                                view.Columns[VisibleColumns.Description].Visible,
			                                view.Columns[VisibleColumns.File].Visible,
			                                view.Columns[VisibleColumns.Project].Visible,
			                                view.Columns[VisibleColumns.Path].Visible);
			PropertyService.Set ("Monodevelop.ErrorListColumns", columns);
		}
		
		public void RedrawContent()
		{
		}

		Gtk.Menu CreateMenu ()
		{
			if (menu != null)
				return menu;
			
			var group = new ActionGroup ("Popup");

			var help = new Gtk.Action ("help", GettextCatalog.GetString ("Show Error Reference"),
				GettextCatalog.GetString ("Show Error Reference"), Gtk.Stock.Help);
			help.Activated += OnShowReference;
			group.Add (help, "F1");

			var copy = new Gtk.Action ("copy", GettextCatalog.GetString ("_Copy"),
				GettextCatalog.GetString ("Copy task"), Gtk.Stock.Copy);
			copy.Activated += OnTaskCopied;
			group.Add (copy, "<Control><Mod2>c");

			var jump = new Gtk.Action ("jump", GettextCatalog.GetString ("_Go to"),
				GettextCatalog.GetString ("Go to task"), Gtk.Stock.JumpTo);
			jump.Activated += OnTaskJumpto;
			group.Add (jump);

			var columns = new Gtk.Action ("columns", GettextCatalog.GetString ("Columns"));
			group.Add (columns, null);

			var columnType = new ToggleAction ("columnType", GettextCatalog.GetString ("Type"),
				GettextCatalog.GetString ("Toggle visibility of Type column"), null);
			columnType.Toggled += OnColumnVisibilityChanged;
			columnsActions[columnType] = VisibleColumns.Type;
			group.Add (columnType);

			var columnValidity = new ToggleAction ("columnValidity", GettextCatalog.GetString ("Validity"),
				GettextCatalog.GetString ("Toggle visibility of Validity column"), null);
			columnValidity.Toggled += OnColumnVisibilityChanged;
			columnsActions[columnValidity] = VisibleColumns.Marked;
			group.Add (columnValidity);

			var columnLine = new ToggleAction ("columnLine", GettextCatalog.GetString ("Line"),
				GettextCatalog.GetString ("Toggle visibility of Line column"), null);
			columnLine.Toggled += OnColumnVisibilityChanged;
			columnsActions[columnLine] = VisibleColumns.Line;
			group.Add (columnLine);

			var columnDescription = new ToggleAction ("columnDescription", GettextCatalog.GetString ("Description"),
				GettextCatalog.GetString ("Toggle visibility of Description column"), null);
			columnDescription.Toggled += OnColumnVisibilityChanged;
			columnsActions[columnDescription] = VisibleColumns.Description;
			group.Add (columnDescription);

			var columnFile = new ToggleAction ("columnFile", GettextCatalog.GetString ("File"),
				GettextCatalog.GetString ("Toggle visibility of File column"), null);
			columnFile.Toggled += OnColumnVisibilityChanged;
			columnsActions[columnFile] = VisibleColumns.File;
			group.Add (columnFile);

			var columnProject = new ToggleAction ("columnProject", GettextCatalog.GetString ("Project"),
				GettextCatalog.GetString ("Toggle visibility of Project column"), null);
			columnProject.Toggled += OnColumnVisibilityChanged;
			columnsActions[columnProject] = VisibleColumns.Project;
			group.Add (columnProject);

			var columnPath = new ToggleAction ("columnPath", GettextCatalog.GetString ("Path"),
				GettextCatalog.GetString ("Toggle visibility of Path column"), null);
			columnPath.Toggled += OnColumnVisibilityChanged;
			columnsActions[columnPath] = VisibleColumns.Path;
			group.Add (columnPath);

			var uiManager = new UIManager ();
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
				+ "<menuitem action='columnProject' />"
				+ "<menuitem action='columnPath' />"
				+ "</menu>"
				+ "</popup></ui>";

			uiManager.AddUiFromString (uiStr);
			menu = (Menu)uiManager.GetWidget ("/popup");
			menu.ShowAll ();

			menu.Shown += delegate {
				columnType.Active = view.Columns[VisibleColumns.Type].Visible;
				columnValidity.Active = view.Columns[VisibleColumns.Marked].Visible;
				columnLine.Active = view.Columns[VisibleColumns.Line].Visible;
				columnDescription.Active = view.Columns[VisibleColumns.Description].Visible;
				columnFile.Active = view.Columns[VisibleColumns.File].Visible;
				columnProject.Active = view.Columns[VisibleColumns.Project].Visible;
				columnPath.Active = view.Columns[VisibleColumns.Path].Visible;
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
			
			return menu;
		}

		Task SelectedTask
		{
			get {
				TreeModel model;
				TreeIter iter;
				if (view.Selection.GetSelected (out model, out iter)) 
					return model.GetValue (iter, DataColumns.Task) as Task;
				return null; // no one selected
			}
		}

		[CommandHandler (EditCommands.Copy)]
		protected void OnCopy ()
		{
			OnTaskCopied (null, null);
		}

		void OnTaskCopied (object o, EventArgs args)
		{
			Task task = SelectedTask;
			if (task != null) {
				StringBuilder text = new StringBuilder ();
				if (!string.IsNullOrEmpty (task.FileName)) {
					text.Append (task.FileName);
					if (task.Line >= 1) {
						text.Append ("(").Append (task.Column);
						if (task.Column >= 0)
							text.Append (",").Append (task.Column);
						text.Append (")");
					}
					text.Append (": ");
				}
				text.Append (task.Severity);
				if (!string.IsNullOrEmpty (task.Code)) {
					text.Append (" ").Append (task.Code);
				}
				text.Append (": ");
				text.Append (task.Description);
				if (task.WorkspaceObject != null)
					text.Append (" (").Append (task.WorkspaceObject.Name).Append (")");
				
				clipboard = Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
				clipboard.Text = text.ToString ();
				clipboard = Clipboard.Get (Gdk.Atom.Intern ("PRIMARY", false));
				clipboard.Text = text.ToString ();
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
			if (task != null && !String.IsNullOrEmpty (task.Code)) {
				reference = task.Code;
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
				iter = filter.ConvertIterToChildIter (sort.ConvertIterToChildIter (iter));
				store.SetValue (iter, DataColumns.Read, true);
				Task task = store.GetValue (iter, DataColumns.Task) as Task;
				if (task != null) {
					TaskService.ShowStatus (task);
					task.JumpToPosition ();
					TaskService.Errors.CurrentLocationTask = task;
					IdeApp.Workbench.ActiveLocationList = TaskService.Errors;
				}
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
			
			TreeViewColumn col;
			col = view.AppendColumn ("!", iconRender, "pixbuf", DataColumns.Type);
			
			col = view.AppendColumn ("", toggleRender);
			col.SetCellDataFunc (toggleRender, new Gtk.TreeCellDataFunc (ToggleDataFunc));
			
			col = view.AppendColumn (GettextCatalog.GetString ("Line"), view.TextRenderer);
			col.SetCellDataFunc (view.TextRenderer, new Gtk.TreeCellDataFunc (LineDataFunc));
			
			col = view.AppendColumn (GettextCatalog.GetString ("Description"), view.TextRenderer);
			col.SetCellDataFunc (view.TextRenderer, new Gtk.TreeCellDataFunc (DescriptionDataFunc));
			col.Resizable = true;
			
			col = view.AppendColumn (GettextCatalog.GetString ("File"), view.TextRenderer);
			col.SetCellDataFunc (view.TextRenderer, new Gtk.TreeCellDataFunc (FileDataFunc));
			col.Resizable = true;
			
			col = view.AppendColumn (GettextCatalog.GetString ("Project"), view.TextRenderer);
			col.SetCellDataFunc (view.TextRenderer, new Gtk.TreeCellDataFunc (ProjectDataFunc));
			col.Resizable = true;
			
			col = view.AppendColumn (GettextCatalog.GetString ("Path"), view.TextRenderer);
			col.SetCellDataFunc (view.TextRenderer, new Gtk.TreeCellDataFunc (PathDataFunc));
			col.Resizable = true;
		}
		
		static void ToggleDataFunc (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			Gtk.CellRendererToggle toggleRenderer = (Gtk.CellRendererToggle)cell;
			Task task = model.GetValue (iter, DataColumns.Task) as Task; 
			if (task == null)
				return;
			toggleRenderer.Active = task.Completed;
		}
		
		static void LineDataFunc (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			Gtk.CellRendererText textRenderer = (Gtk.CellRendererText)cell;
			Task task = model.GetValue (iter, DataColumns.Task) as Task; 
			if (task == null)
				return;
			SetText (textRenderer, model, iter, task, task.Line != 0 ? task.Line.ToString () : "");
		}
		
		static void DescriptionDataFunc (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			Gtk.CellRendererText textRenderer = (Gtk.CellRendererText)cell;
			Task task = model.GetValue (iter, DataColumns.Task) as Task; 
			if (task == null)
				return;
			SetText (textRenderer, model, iter, task, task.Description);
		}
		
		static void FileDataFunc (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			Gtk.CellRendererText textRenderer = (Gtk.CellRendererText)cell;
			Task task = model.GetValue (iter, DataColumns.Task) as Task; 
			if (task == null)
				return;
			
			string tmpPath = "";
			string fileName = "";
			try {
				tmpPath = GetPath (task);
				fileName = Path.GetFileName (tmpPath);
			} catch (Exception) { 
				fileName =  tmpPath;
			}
			
			SetText (textRenderer, model, iter, task, fileName);
		}
		
		static string GetPath (Task task)
		{
			if (task.WorkspaceObject != null)
				return FileService.AbsoluteToRelativePath (task.WorkspaceObject.BaseDirectory, task.FileName);
			
			return task.FileName;
		}
		
		static void ProjectDataFunc (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			Gtk.CellRendererText textRenderer = (Gtk.CellRendererText)cell;
			Task task = model.GetValue (iter, DataColumns.Task) as Task; 
			if (task == null)
				return;
			SetText (textRenderer, model, iter, task, GetProject(task));
		}
		
		static string GetProject (Task task)
		{
			return (task != null && task.WorkspaceObject is SolutionItem)? task.WorkspaceObject.Name: string.Empty;
		}
		
		static void PathDataFunc (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			Gtk.CellRendererText textRenderer = (Gtk.CellRendererText)cell;
			Task task = model.GetValue (iter, DataColumns.Task) as Task; 
			if (task == null)
				return;
			SetText (textRenderer, model, iter, task, GetPath (task));
		}
		
		static void SetText (CellRendererText textRenderer, TreeModel model, TreeIter iter, Task task, string text)
		{
			textRenderer.Text = text;
			textRenderer.Weight = (int)((bool)model.GetValue (iter, DataColumns.Read) ? Pango.Weight.Normal : Pango.Weight.Bold);
			textRenderer.Strikethrough = task.Completed;
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
				Task task = store.GetValue (iter, DataColumns.Task) as Task;
				if (task == null)
					return true;
				if (task.Severity == TaskSeverity.Error && errorBtn.Active) canShow = true;
				else if (task.Severity == TaskSeverity.Warning && warnBtn.Active) canShow = true;
				else if (task.Severity == TaskSeverity.Information && msgBtn.Active) canShow = true;
			} catch {
				//Not yet fully added
				return false;
			}
			
			return canShow;
		}

		public void ShowResults (object sender, EventArgs e)
		{
			Clear();

			AddTasks (TaskService.Errors);
		}

		private void Clear()
		{
			errorCount = warningCount = infoCount = 0;
			store.Clear ();
			tasks.Clear ();
			UpdateErrorsNum ();
			UpdateWarningsNum ();
			UpdateMessagesNum ();
		}
		
		void TaskChanged (object sender, TaskEventArgs e)
		{
			this.view.QueueDraw ();
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
			if (tasks.Contains (t)) return;
			
			Gdk.Pixbuf stock;
			
			switch (t.Severity) {
				case TaskSeverity.Error:
					stock = iconError;
					errorCount++;
					UpdateErrorsNum ();
					break; 
				case TaskSeverity.Warning:
					stock = iconWarning;
					warningCount++;
					UpdateWarningsNum ();	
					break;
				default:
					stock = iconInfo;
					infoCount++;
					UpdateMessagesNum ();
					break;
			}
			
			tasks [t] = t;
			
			store.AppendValues (stock, false, t);
		}

		void UpdateErrorsNum () 
		{
			errorBtn.Label = " " + string.Format(GettextCatalog.GetPluralString("{0} Error", "{0} Errors", errorCount), errorCount);
			errorBtn.Image.Show ();
		}

		void UpdateWarningsNum ()
		{
			warnBtn.Label = " " + string.Format(GettextCatalog.GetPluralString("{0} Warning", "{0} Warnings", warningCount), warningCount); 
			warnBtn.Image.Show ();
		}

		void UpdateMessagesNum ()
		{
			msgBtn.Label = " " + string.Format(GettextCatalog.GetPluralString("{0} Message", "{0} Messages", infoCount), infoCount);
			msgBtn.Image.Show ();
		}
		
		private void ItemToggled (object o, ToggledArgs args)
		{
			Gtk.TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path)) {
				Task task = (Task)store.GetValue (iter, DataColumns.Task);
				task.Completed = !task.Completed;
				TaskService.FireTaskToggleEvent (this, new TaskEventArgs (task));
			}
		}

		static int SeverityIterSort(TreeModel model, TreeIter a, TreeIter z)
		{
			Task aTask = model.GetValue(a, DataColumns.Task) as Task,
			     zTask = model.GetValue(z, DataColumns.Task) as Task;
			     
			return (aTask != null && zTask != null) ?
			       aTask.Severity.CompareTo(zTask.Severity) :
			       0;
		}
		
		static int ProjectIterSort (TreeModel model, TreeIter a, TreeIter z)
		{
			Task aTask = model.GetValue (a, DataColumns.Task) as Task,
			     zTask = model.GetValue (z, DataColumns.Task) as Task;
			     
			return (aTask != null && zTask != null) ?
			       GetProject (aTask).CompareTo (GetProject (zTask)) :
			       0;
		}
		
		static int FileIterSort (TreeModel model, TreeIter a, TreeIter z)
		{
			Task aTask = model.GetValue (a, DataColumns.Task) as Task,
			     zTask = model.GetValue (z, DataColumns.Task) as Task;
			     
			return (aTask != null && zTask != null) ?
			       aTask.FileName.CompareTo (zTask.FileName) :
			       0;
		}
		
		void HandleLogBtnToggled (object sender, EventArgs e)
		{
			var visible = logBtn.Active;
			PropertyService.Set (outputViewVisiblePropertyName, visible);
			outputView.Visible = visible;
			
			if (initialLogShow && visible && control.IsRealized) {
				initialLogShow = false;
				SetInitialOutputViewSize (control.Allocation.Width);
			}
		}
		
		void SetInitialOutputViewSize (int controlWidth)
		{
			double relPos = PropertyService.Get<double> (logSeparatorPositionPropertyName, 0.5d);
			int pos = (int) (controlWidth * relPos);
			pos = Math.Max (30, Math.Min (pos, controlWidth - 30));
			control.Position = pos;
		}
	}
	
	class ErrorPadLabelProvider: IDockItemLabelProvider
	{
		public Widget CreateLabel (Orientation orientation)
		{
			Gtk.Box box;
			if (orientation == Orientation.Horizontal)
				box = new HBox ();
			else
				box = new VBox ();
			box.Spacing = 3;
			
			Gdk.Pixbuf errorIcon = ImageService.GetPixbuf (MonoDevelop.Ide.Gui.Stock.Error, IconSize.Menu);
			Gdk.Pixbuf noErrorIcon = ImageService.MakeGrayscale (errorIcon); // creates a new pixbuf instance
			Gdk.Pixbuf warningIcon = ImageService.GetPixbuf (MonoDevelop.Ide.Gui.Stock.Warning, IconSize.Menu);
			Gdk.Pixbuf noWarningIcon = ImageService.MakeGrayscale (warningIcon); // creates a new pixbuf instance
			
			Gtk.Image errorImage = new Gtk.Image (errorIcon);
			Gtk.Image warningImage = new Gtk.Image (warningIcon);
			
			box.PackStart (errorImage, false, false, 0);
			Label errors = new Gtk.Label ();
			box.PackStart (errors, false, false, 0);
			
			box.PackStart (warningImage, false, false, 0);
			Label warnings = new Gtk.Label ();
			box.PackStart (warnings, false, false, 0);
			
			TaskEventHandler updateHandler = delegate {
				int ec=0, wc=0;
				foreach (Task t in TaskService.Errors) {
					if (t.Severity == TaskSeverity.Error)
						ec++;
					else if (t.Severity == TaskSeverity.Warning)
						wc++;
				}
				errors.Text = ec.ToString ();
				errorImage.Pixbuf = ec > 0 ? errorIcon : noErrorIcon;
				warnings.Text = wc.ToString ();
				warningImage.Pixbuf = wc > 0 ? warningIcon : noWarningIcon;
			};
			
			updateHandler (null, null);
			
			TaskService.Errors.TasksAdded += updateHandler;
			TaskService.Errors.TasksRemoved += updateHandler;
			
			box.Destroyed += delegate {
				noErrorIcon.Dispose ();
				noWarningIcon.Dispose ();
				TaskService.Errors.TasksAdded -= updateHandler;
				TaskService.Errors.TasksRemoved -= updateHandler;
			};
			return box;
		}
	}
}
