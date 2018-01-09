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

using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.TypeSystem;
using System.Linq;
using MonoDevelop.Ide.Editor;
using System.Threading;
using System.Threading.Tasks;

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

		static readonly string restoreID = "Monodevelop.CommentTasksColumns";

		PadTreeView view;
		ListStore store;
		TreeModelSort sortModel;

		Gdk.Color highPrioColor, normalPrioColor, lowPrioColor;

		Dictionary<ContextMenuItem, int> columnsActions;
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

			IdeApp.Workspace.LastWorkspaceItemClosed += LastWorkspaceItemClosed;
			MonoDevelopWorkspace.LoadingFinished += OnWorkspaceItemLoaded;
			IdeApp.Workspace.WorkspaceItemUnloaded += OnWorkspaceItemUnloaded;

			highPrioColor = StringToColor (IdeApp.Preferences.UserTasksHighPrioColor);
			normalPrioColor = StringToColor (IdeApp.Preferences.UserTasksNormalPrioColor);
			lowPrioColor = StringToColor (IdeApp.Preferences.UserTasksLowPrioColor);

			store = new Gtk.ListStore (
				typeof (int),        // line
				typeof (string),     // desc
				typeof (string),     // file
				typeof (string),     // path
				typeof (TaskListEntry),       // task
				typeof (Gdk.Color),  // foreground color
				typeof (int));       // font weight

			sortModel = new TreeModelSort (store);

			view = new MonoDevelop.Ide.Gui.Components.PadTreeView (sortModel);
			view.RulesHint = true;
			view.SearchColumn = (int)Columns.Description;
			view.DoPopupMenu = ShowPopupMenu;
			view.RowActivated += new RowActivatedHandler (OnRowActivated);

			TreeViewColumn col;
			col = view.AppendColumn (GettextCatalog.GetString ("Line"), view.TextRenderer, "text", Columns.Line, "foreground-gdk", Columns.Foreground, "weight", Columns.Bold);
			col.Clickable = false;

			col = view.AppendColumn (GettextCatalog.GetString ("Description"), view.TextRenderer, "text", Columns.Description, "foreground-gdk", Columns.Foreground, "weight", Columns.Bold);
			col.SortColumnId = (int)Columns.Description;
			col.Resizable = true;

			col = view.AppendColumn (GettextCatalog.GetString ("File"), view.TextRenderer, "text", Columns.File, "foreground-gdk", Columns.Foreground, "weight", Columns.Bold);
			col.SortColumnId = (int)Columns.File;
			col.Resizable = true;

			col = view.AppendColumn (GettextCatalog.GetString ("Path"), view.TextRenderer, "text", Columns.Path, "foreground-gdk", Columns.Foreground, "weight", Columns.Bold);
			col.SortColumnId = (int)Columns.Path;
			col.Resizable = true;

			LoadColumnsVisibility ();

			OnWorkspaceItemLoaded (null, EventArgs.Empty);

			comments.TasksAdded += GeneratedTaskAdded;
			comments.TasksRemoved += GeneratedTaskRemoved;

			IdeApp.Preferences.UserTasksHighPrioColor.Changed += OnPropertyUpdated;
			IdeApp.Preferences.UserTasksNormalPrioColor.Changed += OnPropertyUpdated;
			IdeApp.Preferences.UserTasksLowPrioColor.Changed += OnPropertyUpdated;
			
			// Initialize with existing tags.
			foreach (TaskListEntry t in comments)
				AddGeneratedTask (t);

			view.Destroyed += delegate {
				view.RowActivated -= OnRowActivated;
				TaskService.CommentTasksChanged -= OnCommentTasksChanged;
				CommentTag.SpecialCommentTagsChanged -= OnCommentTagsChanged;
				MonoDevelopWorkspace.LoadingFinished -= OnWorkspaceItemLoaded;
				IdeApp.Workspace.WorkspaceItemUnloaded -= OnWorkspaceItemUnloaded;
				comments.TasksAdded -= GeneratedTaskAdded;
				comments.TasksRemoved -= GeneratedTaskRemoved;

				IdeApp.Preferences.UserTasksHighPrioColor.Changed -= OnPropertyUpdated;
				IdeApp.Preferences.UserTasksNormalPrioColor.Changed -= OnPropertyUpdated;
				IdeApp.Preferences.UserTasksLowPrioColor.Changed -= OnPropertyUpdated;
			};
		}

		void LoadColumnsVisibility ()
		{
			string columns = (string)PropertyService.Get (restoreID, "TRUE;TRUE;TRUE;TRUE");
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
		
		void OnWorkspaceItemLoaded (object sender, EventArgs e)
		{
			comments.BeginTaskUpdates ();
			try {
				foreach (var sln in IdeApp.Workspace.GetAllSolutions ()) {
					CommentTasksProvider.Legacy.LoadSolutionContents (sln);
					loadedSlns.Add (sln);
				}
			}
			finally {
				comments.EndTaskUpdates ();
			}
		}

		void OnWorkspaceItemUnloaded (object sender, WorkspaceItemEventArgs e)
		{
			comments.RemoveItemTasks (e.Item, true);

			if (e.Item is Solution solution) {
				loadedSlns.Remove (solution);
			}
		}
		
		void LastWorkspaceItemClosed (object sender, EventArgs e)
		{
			loadedSlns.Clear ();
		}

		void OnCommentTasksChanged (object sender, CommentTasksChangedEventArgs args)
		{
			Application.Invoke ((o, a) => {
				foreach (var e in args.Changes) {
					//because of parse queueing, it's possible for this event to come in after the solution is closed
					//so we track which solutions are currently open so that we don't leak memory by holding 
					// on to references to closed projects
					if (e.Project != null && e.Project.ParentSolution != null && loadedSlns.Contains (e.Project.ParentSolution)) {
						UpdateCommentTags (e.Project.ParentSolution, e.FileName, e.TagComments);
					}
				}
			});
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
			
			List<TaskListEntry> newTasks = new List<TaskListEntry> ();
			if (tagComments != null) {  
				foreach (Tag tag in tagComments) {
					TaskPriority priority;
					string desc = tag.Text.Trim ();

					if (!priorities.TryGetValue (tag.Key, out priority)) {
						if (!Enum.TryParse (tag.Key, out priority))
							priority = TaskPriority.High;
					} else {
						//prepend the tag if it's not already there
						if (!desc.StartsWith (tag.Key, StringComparison.Ordinal)) {
							if (desc.StartsWith (":", StringComparison.Ordinal))
								desc = tag.Key + desc;
							else if (tag.Key.EndsWith (":", StringComparison.Ordinal))
								desc = tag.Key + " " + desc;
							else
								desc = tag.Key + ": " + desc;
						}
					}
					
					TaskListEntry t = new TaskListEntry (fileName, desc, tag.Region.BeginColumn, tag.Region.BeginLine,
					                   TaskSeverity.Information, priority, wob);
					newTasks.Add (t);
				}
			}
			
			List<TaskListEntry> oldTasks = new List<TaskListEntry> (comments.GetFileTasks (fileName));

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
			foreach (TaskListEntry t in e.Tasks)
				AddGeneratedTask (t);
		}

		void AddGeneratedTask (TaskListEntry t)
		{
			FilePath tmpPath = t.FileName;
			if (t.WorkspaceObject != null)
				tmpPath = tmpPath.ToRelative (t.WorkspaceObject.BaseDirectory);

			store.AppendValues (t.Line, t.Description, tmpPath.FileName, tmpPath.ParentDirectory.FileName, t, GetColorByPriority (t.Priority), (int)Pango.Weight.Bold);
		}

		void GeneratedTaskRemoved (object sender, TaskEventArgs e)
		{
			foreach (TaskListEntry t in e.Tasks)
				RemoveGeneratedTask (t);
		}

		void RemoveGeneratedTask (TaskListEntry t)
		{
			TreeIter iter = FindTask (store, t);
			if (!iter.Equals (TreeIter.Zero))
				store.Remove (ref iter);
		}

		static TreeIter FindTask (ListStore store, TaskListEntry task)
		{
			TreeIter iter;
			if (!store.GetIterFirst (out iter))
				return TreeIter.Zero;
			
			do {
				TaskListEntry t = store.GetValue (iter, (int)Columns.Task) as TaskListEntry;
				if (t == task)
					return iter;
			}
			while (store.IterNext (ref iter));
			
			return TreeIter.Zero;
		}

		void ShowPopupMenu (Gdk.EventButton evnt)
		{
			var menu = new ContextMenu ();
			columnsActions = new Dictionary<ContextMenuItem, int> ();

			var copy = new ContextMenuItem (GettextCatalog.GetString ("Copy Task"));
			copy.Clicked += OnGenTaskCopied;
			menu.Add (copy);

			var jump = new ContextMenuItem (GettextCatalog.GetString ("_Go to Task"));
			jump.Clicked += OnGenTaskJumpto;
			menu.Add (jump);

			var delete = new ContextMenuItem (GettextCatalog.GetString ("_Delete Task"));
			delete.Clicked += OnGenTaskDelete;
			menu.Add (delete);

			var columns = new ContextMenuItem (GettextCatalog.GetString ("Columns"));
			var columnsMenu = new ColumnSelectorMenu (view, restoreID);
			columns.SubMenu = columnsMenu;
			menu.Add (columns);

			copy.Sensitive = jump.Sensitive = delete.Sensitive =
				view.Selection != null &&
				view.Selection.CountSelectedRows () > 0 &&
				view.IsAColumnVisible ();
			
			menu.Show (view, evnt);
		}

		void OnGenTaskCopied (object o, EventArgs args)
		{
			TaskListEntry task = SelectedTask;
			if (task != null) {
				clipboard = Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
				clipboard.Text = task.Description;
				clipboard = Clipboard.Get (Gdk.Atom.Intern ("PRIMARY", false));
				clipboard.Text = task.Description;
			}
		}

		TaskListEntry SelectedTask
		{
			get {
				TreeModel model;
				TreeIter iter;
				if (view.Selection.GetSelected (out model, out iter))
				{
					return (TaskListEntry)model.GetValue (iter, (int)Columns.Task);
				}
				else return null; // no one selected
			}
		}

		void OnGenTaskJumpto (object o, EventArgs args)
		{
			TaskListEntry task = SelectedTask;
			if (task != null)
				task.JumpToPosition ();
		}

		void OnRowActivated (object o, RowActivatedArgs args)
		{
			OnGenTaskJumpto (null, null);
		}

		async void OnGenTaskDelete (object o, EventArgs args)
		{
			TaskListEntry task = SelectedTask;
			if (task != null && ! String.IsNullOrEmpty (task.FileName)) {
				var doc = await IdeApp.Workbench.OpenDocument (task.FileName, null, Math.Max (1, task.Line), Math.Max (1, task.Column));
				if (doc != null && doc.HasProject && doc.Project is DotNetProject) {
					string[] commentTags = doc.CommentTags;
					if (commentTags != null && commentTags.Length == 1) {
						doc.DisableAutoScroll ();
						doc.RunWhenLoaded (() => {
							string line = doc.Editor.GetLineText (task.Line);
							int index = line.IndexOf (commentTags[0]);
							if (index != -1) {
								doc.Editor.CaretLocation = new DocumentLocation (task.Line, task.Column);
								doc.Editor.StartCaretPulseAnimation ();
								line = line.Substring (0, index);
								var ls = doc.Editor.GetLine (task.Line);
								doc.Editor.ReplaceText (ls.Offset, ls.Length, line);
								comments.Remove (task);
							}
						}); 
					}
				}
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
					TaskListEntry task = (TaskListEntry) store.GetValue (iter, (int)Columns.Task);
					store.SetValue (iter, (int)Columns.Foreground, GetColorByPriority (task.Priority));
				} while (store.IterNext (ref iter));
			}
		}
		
		#region ITaskListView members
		Control ITaskListView.Content {
			get {
				CreateView ();
				return view; 
			} 
		}
		
		Control[] ITaskListView.ToolBarItems {
			get { return null; } 
		}
		#endregion
	}
}
