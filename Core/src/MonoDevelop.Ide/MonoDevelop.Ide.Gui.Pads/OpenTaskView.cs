// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Drawing;
using System.CodeDom.Compiler;
using System.Collections;
using System.IO;
using System.Diagnostics;

using MonoDevelop.Core;
using MonoDevelop.Core.Properties;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Tasks;

using Gtk;

namespace MonoDevelop.Ide.Gui.Pads
{
	internal class OpenTaskView : IPadContent
	{
		VBox control;
		ScrolledWindow sw;
		Gtk.TreeView view;
		ListStore store;
		TreeModelFilter filter;
		ToggleToolButton errorBtn, warnBtn, msgBtn;
		Gtk.Tooltips tips = new Gtk.Tooltips ();
		int errors = 0, warns = 0, msgs = 0;
		Clipboard clipboard;
		Hashtable tasks = new Hashtable ();
		IPadWindow window;
		
		
		void IPadContent.Initialize (IPadWindow window)
		{
			this.window = window;
			window.Title = GettextCatalog.GetString ("Task List");
			window.Icon = MonoDevelop.Core.Gui.Stock.TaskListIcon;
		}
		
		public Gtk.Widget Control {
			get {
				return control;
			}
		}

		public string Id {
			get { return "MonoDevelop.Ide.Gui.Pads.OpenTaskView"; }
		}
		
		public string DefaultPlacement {
			get { return "Bottom"; }
		}
		
		public void RedrawContent()
		{
			// FIXME
		}

		const int COL_TYPE = 0, COL_LINE = 1, COL_DESC = 2, COL_FILE = 3, COL_PATH = 4, COL_TASK = 5, COL_READ = 6, COL_MARKED = 7, COL_READ_WEIGHT = 8;
		
		public OpenTaskView()
		{
			control = new VBox ();

			Toolbar toolbar = new Toolbar ();
			toolbar.IconSize = IconSize.SmallToolbar;
			control.PackStart (toolbar, false, false, 0);
			
			errorBtn = new ToggleToolButton ();
			UpdateErrorsNum();
			errorBtn.Active = (bool)Runtime.Properties.GetProperty ("SharpDevelop.TaskList.ShowErrors", true);
			errorBtn.IconWidget = new Gtk.Image (Gtk.Stock.DialogError, Gtk.IconSize.Button);
			errorBtn.IsImportant = true;
			errorBtn.Toggled += new EventHandler (FilterChanged);
			errorBtn.SetTooltip (tips, GettextCatalog.GetString ("Show Errors"), GettextCatalog.GetString ("Show Errors"));
			toolbar.Insert (errorBtn, -1);
			
			warnBtn = new ToggleToolButton ();
			UpdateWarningsNum();
			warnBtn.Active = (bool)Runtime.Properties.GetProperty ("SharpDevelop.TaskList.ShowWarnings", true);
			warnBtn.IconWidget = new Gtk.Image (Gtk.Stock.DialogWarning, Gtk.IconSize.Button);
			warnBtn.IsImportant = true;
			warnBtn.Toggled += new EventHandler (FilterChanged);
			warnBtn.SetTooltip (tips, GettextCatalog.GetString ("Show Warnings"), GettextCatalog.GetString ("Show Warnings"));
			toolbar.Insert (warnBtn, -1);
			
			msgBtn = new ToggleToolButton ();
			UpdateMessagesNum();
			msgBtn.Active = (bool)Runtime.Properties.GetProperty ("SharpDevelop.TaskList.ShowMessages", true);
			msgBtn.IconWidget = new Gtk.Image (Gtk.Stock.DialogInfo, Gtk.IconSize.Button);
			msgBtn.IsImportant = true;
			msgBtn.Toggled += new EventHandler (FilterChanged);
			msgBtn.SetTooltip (tips, GettextCatalog.GetString ("Show Messages"), GettextCatalog.GetString ("Show Messages"));
			toolbar.Insert (msgBtn, -1);
			
			store = new Gtk.ListStore (
                typeof (Gdk.Pixbuf), // image
				typeof (int),        // line
				typeof (string),     // desc
				typeof (string),     // file
				typeof (string),     // path
				typeof (Task),       // task
				typeof (bool),       // read?
				typeof (bool),       // marked?
				typeof (int));       // read? -- use Pango weight

			TreeIterCompareFunc sortFunc = new TreeIterCompareFunc (TaskSortFunc);
			store.SetSortFunc (COL_TASK, sortFunc);
			store.DefaultSortFunc = sortFunc;
			store.SetSortColumnId (COL_TASK, SortType.Ascending);
			
			TreeModelFilterVisibleFunc filterFunct = new TreeModelFilterVisibleFunc (FilterTaskTypes); 
			filter = new TreeModelFilter (store, null);
            filter.VisibleFunc = filterFunct;
			
			view = new Gtk.TreeView (filter);
			view.RulesHint = true;
			view.PopupMenu += new PopupMenuHandler (OnPopupMenu);
			view.ButtonPressEvent += new ButtonPressEventHandler (OnButtonPressed);
			view.HeadersClickable = true;
			AddColumns ();
			
			sw = new Gtk.ScrolledWindow ();
			sw.ShadowType = ShadowType.In;
			sw.Add (view);
			
			Services.TaskService.TasksChanged     += (EventHandler) Services.DispatchService.GuiDispatch (new EventHandler (ShowResults));
			Services.TaskService.TaskAdded        += (TaskEventHandler) Services.DispatchService.GuiDispatch (new TaskEventHandler (TaskAdded));
			IdeApp.ProjectOperations.EndBuild      += (ProjectCompileEventHandler) Services.DispatchService.GuiDispatch (new ProjectCompileEventHandler (SelectTaskView));
			IdeApp.ProjectOperations.CombineOpened += (CombineEventHandler) Services.DispatchService.GuiDispatch (new CombineEventHandler (OnCombineOpen));
			IdeApp.ProjectOperations.CombineClosed += (CombineEventHandler) Services.DispatchService.GuiDispatch (new CombineEventHandler (OnCombineClosed));
			view.RowActivated            += new RowActivatedHandler (OnRowActivated);
						
			control.Add (sw);
			toolbar.ToolbarStyle = ToolbarStyle.BothHoriz;
			toolbar.ShowArrow = false;
			Control.ShowAll ();
		}

		[GLib.ConnectBefore]
		void OnButtonPressed (object o, ButtonPressEventArgs args)
		{
			if (args.Event.Button == 3)
				ShowPopup ();
		}

		void OnPopupMenu (object o, PopupMenuArgs args)
		{
			ShowPopup ();
		}

		void ShowPopup ()
		{
			Menu menu = new Menu ();
			menu.AccelGroup = new AccelGroup ();
                        ImageMenuItem copy = new ImageMenuItem (Gtk.Stock.Copy, menu.AccelGroup);
                        copy.Activated += new EventHandler (OnTaskCopied);
			menu.Append (copy);
			menu.Popup (null, null, null, 3, Global.CurrentEventTime);
			menu.ShowAll ();
		}

		void OnTaskCopied (object o, EventArgs args)
		{
			Task task;
			TreeModel model;
			TreeIter iter;

			if (view.Selection.GetSelected (out model, out iter))
			{
				task = (Task) model.GetValue (iter, COL_TASK);
			}
			else
			{
				// no selection
				return;
			}

			clipboard = Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
			clipboard.Text = task.ToString();
			clipboard = Clipboard.Get (Gdk.Atom.Intern ("PRIMARY", false));
			clipboard.Text = task.ToString();
		}
		
		void AddColumns ()
		{
			Gtk.CellRendererPixbuf iconRender = new Gtk.CellRendererPixbuf ();
			
			Gtk.CellRendererToggle toggleRender = new Gtk.CellRendererToggle ();
			toggleRender.Toggled += new ToggledHandler (ItemToggled);
			
			Gtk.CellRendererText line = new Gtk.CellRendererText (), desc = new Gtk.CellRendererText () , path = new Gtk.CellRendererText (),
			  file = new Gtk.CellRendererText ();
			
			TreeViewColumn col;
			col = view.AppendColumn ("!"                                        , iconRender   , "pixbuf", COL_TYPE);
			col.Clickable = true;
			col.Clicked += new EventHandler (OnResortTasks);
			col.SortIndicator = true;
			view.AppendColumn (""                                         , toggleRender , "active"  , COL_MARKED, "activatable", COL_READ);
			view.AppendColumn (GettextCatalog.GetString ("Line")        , line         , "text"    , COL_LINE, "weight", COL_READ_WEIGHT);
			col = view.AppendColumn (GettextCatalog.GetString ("Description") , desc         , "text"    , COL_DESC, "weight", COL_READ_WEIGHT, "strikethrough", COL_MARKED);
			col.Resizable = true;
			col = view.AppendColumn (GettextCatalog.GetString ("File")        , file         , "text"    , COL_FILE, "weight", COL_READ_WEIGHT);
			col.Resizable = true;
			col = view.AppendColumn (GettextCatalog.GetString ("Path")        , path         , "text"    , COL_PATH, "weight", COL_READ_WEIGHT);
			col.Resizable = true;
		}
		
		void OnCombineOpen(object sender, CombineEventArgs e)
		{
			store.Clear ();
		}
		
		void OnCombineClosed(object sender, CombineEventArgs e)
		{
			store.Clear ();
		}
		
		public void Dispose ()
		{
		}
		
		void SelectTaskView (bool success)
		{
			if (Services.TaskService.Tasks.Count > 0) {
				try {
					if (window.Visible)
						window.Activate ();
					else if ((bool) Runtime.Properties.GetProperty ("SharpDevelop.ShowTaskListAfterBuild", true)) {
						window.Visible = true;
						window.Activate ();
					}
				} catch {}
			}
		}
		
		void OnRowActivated (object o, RowActivatedArgs args)
		{
			Gtk.TreeIter iter;
			if (store.GetIter (out iter, args.Path)) {
				store.SetValue (iter, COL_READ, true);
				store.SetValue (iter, COL_READ_WEIGHT, (int) Pango.Weight.Normal);
				
				((Task) store.GetValue (iter, COL_TASK)).JumpToPosition ();
			}
		}
		
		public CompilerResults CompilerResults = null;
		
		void FilterChanged (object sender, EventArgs e)
		{
			
			Runtime.Properties.SetProperty ("SharpDevelop.TaskList.ShowErrors", errorBtn.Active);
			Runtime.Properties.SetProperty ("SharpDevelop.TaskList.ShowWarnings", warnBtn.Active);
			Runtime.Properties.SetProperty ("SharpDevelop.TaskList.ShowMessages", msgBtn.Active);
			
			filter.Refilter ();
		} 
		
		bool FilterTaskTypes (TreeModel model, TreeIter iter)
        {
        		bool canShow = false;
        	
        		try {
              	Task task = (Task) store.GetValue (iter, COL_TASK);
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
			store.Clear ();
			tasks.Clear ();
			errors = warns = msgs = 0;
			UpdateErrorsNum ();
			UpdateWarningsNum ();
			UpdateMessagesNum ();
			
			foreach (Task t in Services.TaskService.Tasks) {
				AddTask (t);
			}
			SelectTaskView(true);
		}
		
		void TaskAdded (object sender, TaskEventArgs e)
		{
			AddTask (e.Task);
		}
		
		public void AddTask (Task t)
		{
			if (tasks.Contains (t)) return;
			
			
			switch (t.TaskType) {
				case TaskType.Error:
					errors++;
					UpdateErrorsNum ();
					break; 
				case TaskType.Warning:
					warns++;
					UpdateWarningsNum ();	
					break;
				default:
					msgs++;
					UpdateMessagesNum ();
					break;
			}
		
			tasks [t] = t;
			
			Gdk.Pixbuf stock;
			switch (t.TaskType) {
				case TaskType.Warning:
					stock = sw.RenderIcon (Gtk.Stock.DialogWarning, Gtk.IconSize.SmallToolbar, "");
					break;
				case TaskType.Error:
					stock = sw.RenderIcon (Gtk.Stock.DialogError, Gtk.IconSize.SmallToolbar, "");
					break;
				case TaskType.Comment:
					stock = sw.RenderIcon (Gtk.Stock.DialogInfo, Gtk.IconSize.SmallToolbar, "");
					break;
				case TaskType.SearchResult:
					stock = sw.RenderIcon (Gtk.Stock.DialogQuestion, Gtk.IconSize.SmallToolbar, "");
					break;
				default:
					stock = null;
					break;
			}
			
			string tmpPath = t.FileName;
			if (t.Project != null)
				tmpPath = Runtime.FileUtilityService.AbsoluteToRelativePath (t.Project.BaseDirectory, t.FileName);
			
			string fileName = tmpPath;
			string path     = tmpPath;
			
			try {
				fileName = Path.GetFileName(tmpPath);
			} catch (Exception) {}
			
			try {
				path = Path.GetDirectoryName(tmpPath);
			} catch (Exception) {}
			
			store.AppendValues (
				stock,
				t.Line,
				t.Description,
				fileName,
				path,
				t, false, false, (int) Pango.Weight.Bold);
			
			filter.Refilter ();
		}
		
		void UpdateErrorsNum () 
		{
			errorBtn.Label = string.Format(GettextCatalog.GetPluralString("{0} Error", "{0} Errors", errors), errors);
		}
		
		void UpdateWarningsNum ()
		{
			warnBtn.Label = string.Format(GettextCatalog.GetPluralString("{0} Warning", "{0} Warnings", warns), warns); 
		}	
		
		void UpdateMessagesNum ()
		{
			msgBtn.Label = string.Format(GettextCatalog.GetPluralString("{0} Message", "{0} Messages", msgs), msgs);
		}
		
		private void ItemToggled (object o, ToggledArgs args)
		{
			Gtk.TreeIter iter;
			if (store.GetIterFromString(out iter, args.Path)) {
				bool val = (bool) store.GetValue(iter, COL_MARKED);
				store.SetValue(iter, COL_MARKED, !val);
			}
		}

		private SortType ReverseSortOrder (TreeViewColumn col)     {
			if (col.SortIndicator)  {
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
			store.SetSortColumnId (COL_TASK, col.SortOrder);
		}

		private int TaskSortFunc (TreeModel model, TreeIter iter1, TreeIter iter2)
		{
			Task task1 = model.GetValue (iter1, COL_TASK) as Task;
			Task task2 = model.GetValue (iter2, COL_TASK) as Task;

			if (task1 == null && task2 == null) return 0;
			else if (task1 == null) return -1;
			else if (task2 == null) return 1;

			int compare = ((int)task1.TaskType).CompareTo ((int)task2.TaskType);
			if (compare == 0)
				compare = task1.FileName.CompareTo (task2.FileName);
			if (compare == 0)
				compare = task1.Line.CompareTo (task2.Line);
			return compare;
		}
	}
}
