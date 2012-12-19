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
using System.Collections.Generic;
using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects.Text;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;
using System.Linq;

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

		MonoDevelop.Ide.Gui.Components.PadTreeView view;
		ListStore store;

		Gdk.Color highPrioColor, normalPrioColor, lowPrioColor;

		Dictionary<ToggleAction, int> columnsActions = new Dictionary<ToggleAction, int> ();
		Clipboard clipboard;
		
		TaskStore comments = new TaskStore ();
		Dictionary<string, TaskPriority> priorities = new Dictionary<string, TaskPriority> ();
		HashSet<Solution> loadedSlns = new HashSet<Solution> ();

		public CommentTasksView ()
		{
		}
		
		void CreateView ()
		{
			if (view != null)
				return;
			
			ReloadPriorities ();
			
			TaskService.CommentTasksChanged += OnCommentTasksChanged;
			CommentTag.SpecialCommentTagsChanged += OnCommentTagsChanged;
			IdeApp.Workspace.WorkspaceItemLoaded += OnWorkspaceItemLoaded;
			IdeApp.Workspace.WorkspaceItemUnloaded += OnWorkspaceItemUnloaded;
			
			highPrioColor = StringToColor ((string)PropertyService.Get ("Monodevelop.UserTasksHighPrioColor", ""));
			normalPrioColor = StringToColor ((string)PropertyService.Get ("Monodevelop.UserTasksNormalPrioColor", ""));
			lowPrioColor = StringToColor ((string)PropertyService.Get ("Monodevelop.UserTasksLowPrioColor", ""));

			store = new Gtk.ListStore (
				typeof (int),        // line
				typeof (string),     // desc
				typeof (string),     // file
				typeof (string),     // path
				typeof (Task),       // task
				typeof (Gdk.Color),  // foreground color
				typeof (int));       // font weight

			view = new MonoDevelop.Ide.Gui.Components.PadTreeView (store);
			view.RulesHint = true;
			view.SearchColumn = (int)Columns.Description;
			view.DoPopupMenu = (evt) => IdeApp.CommandService.ShowContextMenu (view, evt, CreateMenu ());
			view.RowActivated += new RowActivatedHandler (OnRowActivated);

			TreeViewColumn col;
			col = view.AppendColumn (GettextCatalog.GetString ("Line"), view.TextRenderer, "text", Columns.Line, "foreground-gdk", Columns.Foreground, "weight", Columns.Bold);
			col.Clickable = false;

			col = view.AppendColumn (GettextCatalog.GetString ("Description"), view.TextRenderer, "text", Columns.Description, "foreground-gdk", Columns.Foreground, "weight", Columns.Bold);
			col.Clickable = true;
			col.SortColumnId = (int)Columns.Description;
			col.Resizable = true;
			col.Clicked += Resort;

			col = view.AppendColumn (GettextCatalog.GetString ("File"), view.TextRenderer, "text", Columns.File, "foreground-gdk", Columns.Foreground, "weight", Columns.Bold);
			col.Clickable = true;
			col.SortColumnId = (int)Columns.File;
			col.Resizable = true;
			col.Clicked += Resort;

			col = view.AppendColumn (GettextCatalog.GetString ("Path"), view.TextRenderer, "text", Columns.Path, "foreground-gdk", Columns.Foreground, "weight", Columns.Bold);
			col.Clickable = true;
			col.SortColumnId = (int)Columns.Path;
			col.Resizable = true;
			col.Clicked += Resort;

			LoadColumnsVisibility ();
			
			comments.BeginTaskUpdates ();
			try {
				foreach (var item in IdeApp.Workspace.Items) {
					LoadWorkspaceItemContents (item);
				}
			} finally {
				comments.EndTaskUpdates ();
			}

			comments.TasksAdded += DispatchService.GuiDispatch<TaskEventHandler> (GeneratedTaskAdded);
			comments.TasksRemoved += DispatchService.GuiDispatch<TaskEventHandler> (GeneratedTaskRemoved);

			PropertyService.PropertyChanged += DispatchService.GuiDispatch<EventHandler<PropertyChangedEventArgs>> (OnPropertyUpdated);
			
			// Initialize with existing tags.
			foreach (Task t in comments)
				AddGeneratedTask (t);
		}

		void LoadColumnsVisibility ()
		{
			string columns = (string)PropertyService.Get ("Monodevelop.CommentTasksColumns", "TRUE;TRUE;TRUE;TRUE");
			string[] tokens = columns.Split (new char[] {';'}, StringSplitOptions.RemoveEmptyEntries);
			if (tokens.Length == 4 && view != null && view.Columns.Length == 4)
			{
				for (int i = 0; i < 4; i++)
				{
					bool visible;
					if (bool.TryParse (tokens[i], out visible))
						view.Columns[i].Visible = visible;
				}
			}
		}

		void StoreColumnsVisibility ()
		{
			string columns = String.Format ("{0};{1};{2};{3}",
			                                view.Columns[(int)Columns.Line].Visible,
			                                view.Columns[(int)Columns.Description].Visible,
			                                view.Columns[(int)Columns.File].Visible,
			                                view.Columns[(int)Columns.Path].Visible);
			PropertyService.Set ("Monodevelop.CommentTasksColumns", columns);
		}
		
		void OnWorkspaceItemLoaded (object sender, WorkspaceItemEventArgs e)
		{
			comments.BeginTaskUpdates ();
			try {
				LoadWorkspaceItemContents (e.Item);
			}
			finally {
				comments.EndTaskUpdates ();
			}
		}
		
		void LoadWorkspaceItemContents (WorkspaceItem wob)
		{
			foreach (var sln in wob.GetAllSolutions ())
				LoadSolutionContents (sln);
		}
		
		void LoadSolutionContents (Solution sln)
		{
			loadedSlns.Add (sln);
			System.Threading.ThreadPool.QueueUserWorkItem (delegate {
				// Load all tags that are stored in pidb files
				foreach (Project p in sln.GetAllProjects ()) {
					var pContext = TypeSystemService.GetProjectContentWrapper (p);
					if (pContext == null) {
						continue;
					}
					var tags = pContext.GetExtensionObject<ProjectCommentTags> ();
					if (tags == null) {
						tags = new ProjectCommentTags ();
						pContext.UpdateExtensionObject (tags);
						tags.Update (pContext.Project);
					} else {
						foreach (var kv in tags.Tags) {
							UpdateCommentTags (sln, kv.Key, kv.Value);
						}
					}
				}
			});
		}
		
		
		static IEnumerable<Tag> GetSpecialComments (IProjectContent ctx, string name)
		{
			var doc = ctx.GetFile (name) as ParsedDocument;
			if (doc == null)
				return Enumerable.Empty<Tag> ();
			return (IEnumerable<Tag>)doc.TagComments;
		}
		
		void OnWorkspaceItemUnloaded (object sender, WorkspaceItemEventArgs e)
		{
			foreach (var sln in e.Item.GetAllSolutions ())
				loadedSlns.Remove (sln);
			comments.RemoveItemTasks (e.Item, true);
		}		

		void OnCommentTasksChanged (object sender, CommentTasksChangedEventArgs e)
		{
			//because of parse queueing, it's possible for this event to come in after the solution is closed
			//so we track which solutions are currently open so that we don't leak memory by holding 
			// on to references to closed projects
			if (e.Project != null && e.Project.ParentSolution != null && loadedSlns.Contains (e.Project.ParentSolution)) {
				Application.Invoke (delegate {
					UpdateCommentTags (e.Project.ParentSolution, e.FileName, e.TagComments);
				});
			}
		}
		
		void OnCommentTagsChanged (object sender, EventArgs e)
		{
			ReloadPriorities ();
		}
		
		void UpdateCommentTags (Solution wob, FilePath fileName, IEnumerable<Tag> tagComments)
		{
			if (fileName == FilePath.Null)
				return;
			
			fileName = fileName.FullPath;
			
			List<Task> newTasks = new List<Task> ();
			if (tagComments != null) {  
				foreach (Tag tag in tagComments) {
					if (!priorities.ContainsKey (tag.Key))
						continue;
					
					//prepend the tag if it's not already there
					string desc = tag.Text.Trim ();
					if (!desc.StartsWith (tag.Key)) {
						if (desc.StartsWith (":"))
							desc = tag.Key + desc;
						else if (tag.Key.EndsWith (":"))
							desc = tag.Key + " " + desc;
						else
							desc = tag.Key + ": " + desc;
					}
					
					Task t = new Task (fileName, desc, tag.Region.BeginColumn, tag.Region.BeginLine,
					                   TaskSeverity.Information, priorities[tag.Key], wob);
					newTasks.Add (t);
				}
			}
			
			List<Task> oldTasks = new List<Task> (comments.GetFileTasks (fileName));

			for (int i = 0; i < newTasks.Count; ++i) {
				for (int j = 0; j < oldTasks.Count; ++j) {
					if (oldTasks[j] != null &&
					    newTasks[i].Line == oldTasks[j].Line &&
					    newTasks[i].Column == oldTasks[j].Column &&
					    newTasks[i].Description == oldTasks[j].Description &&
					    newTasks[i].Priority == oldTasks[j].Priority)
					{
						newTasks.RemoveAt (i);
						oldTasks.RemoveAt (j);
						i--;
						break;
					}
				}
			}
			
			comments.BeginTaskUpdates ();
			try {
				comments.AddRange (newTasks);
				comments.RemoveRange (oldTasks);
			} finally {
				comments.EndTaskUpdates ();
			}
		}
		
		void ReloadPriorities ()
		{
			priorities.Clear ();
			foreach (var tag in CommentTag.SpecialCommentTags)
				priorities.Add (tag.Tag, (TaskPriority) tag.Priority);
		}		
		
		void GeneratedTaskAdded (object sender, TaskEventArgs e)
		{
			foreach (Task t in e.Tasks)
				AddGeneratedTask (t);
		}

		void AddGeneratedTask (Task t)
		{
			FilePath tmpPath = t.FileName;
			if (t.WorkspaceObject != null)
				tmpPath = tmpPath.ToRelative (t.WorkspaceObject.BaseDirectory);

			store.AppendValues (t.Line, t.Description, tmpPath.FileName, tmpPath.ParentDirectory.FileName, t, GetColorByPriority (t.Priority), (int)Pango.Weight.Bold);
		}

		void GeneratedTaskRemoved (object sender, TaskEventArgs e)
		{
			foreach (Task t in e.Tasks)
				RemoveGeneratedTask (t);
		}

		void RemoveGeneratedTask (Task t)
		{
			TreeIter iter = FindTask (store, t);
			if (!iter.Equals (TreeIter.Zero))
				store.Remove (ref iter);
		}

		static TreeIter FindTask (ListStore store, Task task)
		{
			TreeIter iter;
			if (!store.GetIterFirst (out iter))
				return TreeIter.Zero;
			
			do {
				Task t = store.GetValue (iter, (int)Columns.Task) as Task;
				if (t == task)
					return iter;
			}
			while (store.IterNext (ref iter));
			
			return TreeIter.Zero;
		}

		Menu CreateMenu ()
		{
			var group = new ActionGroup ("Popup");
			
			var copy = new Gtk.Action ("copy", GettextCatalog.GetString ("_Copy"),
				GettextCatalog.GetString ("Copy comment task"), Gtk.Stock.Copy);
			copy.Activated += OnGenTaskCopied;
			group.Add (copy, "<Control><Mod2>c");

			var jump = new Gtk.Action ("jump", GettextCatalog.GetString ("_Go to"),
				GettextCatalog.GetString ("Go to comment task"), Gtk.Stock.JumpTo);
			jump.Activated += OnGenTaskJumpto;
			group.Add (jump);

			var delete = new Gtk.Action ("delete", GettextCatalog.GetString ("_Delete"),
				GettextCatalog.GetString ("Delete comment task"), Gtk.Stock.Delete);
			delete.Activated += OnGenTaskDelete;
			group.Add (delete);

			var columns = new Gtk.Action ("columns", GettextCatalog.GetString ("Columns"));
			group.Add (columns, null);

			var columnLine = new ToggleAction ("columnLine", GettextCatalog.GetString ("Line"),
				GettextCatalog.GetString ("Toggle visibility of Line column"), null);
			columnLine.Toggled += OnColumnVisibilityChanged;
			columnsActions[columnLine] = (int)Columns.Line;
			group.Add (columnLine);

			var columnDescription = new ToggleAction ("columnDescription", GettextCatalog.GetString ("Description"),
				GettextCatalog.GetString ("Toggle visibility of Description column"), null);
			columnDescription.Toggled += OnColumnVisibilityChanged;
			columnsActions[columnDescription] = (int)Columns.Description;
			group.Add (columnDescription);

			var columnFile = new ToggleAction ("columnFile", GettextCatalog.GetString ("File"),
				GettextCatalog.GetString ("Toggle visibility of File column"), null);
			columnFile.Toggled += OnColumnVisibilityChanged;
			columnsActions[columnFile] = (int)Columns.File;
			group.Add (columnFile);

			var columnPath = new ToggleAction ("columnPath", GettextCatalog.GetString ("Path"),
				GettextCatalog.GetString ("Toggle visibility of Path column"), null);
			columnPath.Toggled += OnColumnVisibilityChanged;
			columnsActions[columnPath] = (int)Columns.Path;
			group.Add (columnPath);

			UIManager uiManager = new UIManager ();
			uiManager.InsertActionGroup (group, 0);
			
			string uiStr = "<ui><popup name='popup'>"
				+ "<menuitem action='copy'/>"
				+ "<menuitem action='jump'/>"
				+ "<menuitem action='delete'/>"
				+ "<separator/>"
				+ "<menu action='columns'>"
				+ "<menuitem action='columnLine' />"
				+ "<menuitem action='columnDescription' />"
				+ "<menuitem action='columnFile' />"
				+ "<menuitem action='columnPath' />"
				+ "</menu>"
				+ "</popup></ui>";

			uiManager.AddUiFromString (uiStr);
			var menu = (Menu)uiManager.GetWidget ("/popup");
			menu.ShowAll ();

			menu.Shown += delegate (object o, EventArgs args)
			{
				columnLine.Active = view.Columns[(int)Columns.Line].Visible;
				columnDescription.Active = view.Columns[(int)Columns.Description].Visible;
				columnFile.Active = view.Columns[(int)Columns.File].Visible;
				columnPath.Active = view.Columns[(int)Columns.Path].Visible;
				copy.Sensitive = jump.Sensitive = delete.Sensitive =
					view.Selection != null &&
					view.Selection.CountSelectedRows () > 0 &&
					(columnLine.Active ||
					columnDescription.Active ||
					columnFile.Active ||
					columnPath.Active);
			};
			return menu;
		}

		void OnGenTaskCopied (object o, EventArgs args)
		{
			Task task = SelectedTask;
			if (task != null) {
				clipboard = Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
				clipboard.Text = task.ToString ();
				clipboard = Clipboard.Get (Gdk.Atom.Intern ("PRIMARY", false));
				clipboard.Text = task.ToString ();
			}
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

		void OnGenTaskJumpto (object o, EventArgs args)
		{
			Task task = SelectedTask;
			if (task != null)
				task.JumpToPosition ();
		}

		void OnRowActivated (object o, RowActivatedArgs args)
		{
			OnGenTaskJumpto (null, null);
		}

		void OnGenTaskDelete (object o, EventArgs args)
		{
			Task task = SelectedTask;
			if (task != null && ! String.IsNullOrEmpty (task.FileName)) {
				Document doc = IdeApp.Workbench.OpenDocument (task.FileName, Math.Max (1, task.Line), Math.Max (1, task.Column));
				if (doc != null && doc.HasProject && doc.Project is DotNetProject) {
					string[] commentTags = doc.CommentTags;
					if (commentTags != null && commentTags.Length == 1) {
						string line = doc.Editor.GetLineText (task.Line);
						int index = line.IndexOf (commentTags[0]);
						if (index != -1) {
							doc.Editor.SetCaretTo (task.Line, task.Column);
							line = line.Substring (0, index);
							var ls = doc.Editor.Document.GetLine (task.Line);
							doc.Editor.Replace (ls.Offset, ls.Length, line);
							comments.Remove (task);
						}
					}
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
						Task task = (Task) store.GetValue (iter, (int)Columns.Task);
						store.SetValue (iter, (int)Columns.Foreground, GetColorByPriority (task.Priority));
					} while (store.IterNext (ref iter));
				}
			}
		}
		
		#region ITaskListView members
		TreeView ITaskListView.Content {
			get {
				CreateView ();
				return view; 
			} 
		}
		
		Widget[] ITaskListView.ToolBarItems {
			get { return null; } 
		}
		#endregion
	}
}
