// 
// TaskService.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
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
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;

using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Tasks
{
	public enum TaskSeverity
	{
		Error,
		Warning,
		Information,
		Comment
	}
	
	public static class TaskService
	{
		static TaskStore errors = new TaskStore ();
		static TaskStore userTasks = new TaskStore ();
		
		static bool errorBubblesVisible = true;
		static bool warningBubblesVisible = true;
		
		static TaskService ()
		{
			if (IdeApp.Workspace != null) {
				IdeApp.Workspace.WorkspaceItemLoaded += OnWorkspaceItemLoaded;
				IdeApp.Workspace.WorkspaceItemUnloaded += OnWorkspaceItemUnloaded;
				CommentTasksProvider.Initialize ();
			}
			errors.ItemName = GettextCatalog.GetString ("Warning/Error");
			userTasks.ItemName = GettextCatalog.GetString ("User Task");
		}
		
		public static TaskStore Errors {
			get { return errors; }
		}
		
		public static TaskStore UserTasks {
			get { return userTasks; }
		}
		
		public static bool ErrorBubblesVisible {
			get { return errorBubblesVisible; }
			set {
				if (errorBubblesVisible != value) {
					errorBubblesVisible = value;
					if (BubblesVisibilityChanged != null)
						BubblesVisibilityChanged (null, EventArgs.Empty);
				}
			}
		}
		
		public static bool WarningBubblesVisible {
			get { return warningBubblesVisible; }
			set {
				if (warningBubblesVisible != value) {
					warningBubblesVisible = value;
					if (BubblesVisibilityChanged != null)
						BubblesVisibilityChanged (null, EventArgs.Empty);
				}
			}
		}
		
		public static event EventHandler BubblesVisibilityChanged;
		
		public static void ShowErrors ()
		{
			Pad errorsPad = IdeApp.Workbench.GetPad<MonoDevelop.Ide.Gui.Pads.ErrorListPad> ();
			if (errorsPad != null) {
				errorsPad.Visible = true;
				errorsPad.BringToFront ();
			}
		}
		
		/// <summary>
		/// Shows a description of the task in the status bar
		/// </summary>
		public static void ShowStatus (TaskListEntry t)
		{
			if (t == null)
				IdeApp.Workbench.StatusBar.ShowMessage (GettextCatalog.GetString ("No more errors or warnings"));
			else if (t.Severity == TaskSeverity.Error)
				IdeApp.Workbench.StatusBar.ShowError (t.Description);
			else if (t.Severity == TaskSeverity.Warning)
				IdeApp.Workbench.StatusBar.ShowWarning (t.Description);
			else
				IdeApp.Workbench.StatusBar.ShowMessage (t.Description);
		}
			
		static void OnWorkspaceItemLoaded (object sender, WorkspaceItemEventArgs e)
		{
			string fileToLoad = GetUserTasksFilename (e.Item);
			
			userTasks.BeginTaskUpdates ();
			try {
				// Load User Tasks from xml file
				if (File.Exists (fileToLoad)) {
					XmlDataSerializer serializer = new XmlDataSerializer (new DataContext ());
					List<TaskListEntry> ts = (List<TaskListEntry>) serializer.Deserialize (fileToLoad, typeof(List<TaskListEntry>));
					foreach (TaskListEntry t in ts) {
						t.WorkspaceObject = e.Item;
						userTasks.Add (t);
					}
				}
			}
			catch (Exception ex) {
				LoggingService.LogWarning ("Could not load user tasks: " + fileToLoad, ex);
			}
			finally {
				userTasks.EndTaskUpdates ();
			}
		}
		
		static void OnWorkspaceItemUnloaded (object sender, WorkspaceItemEventArgs e)
		{
			// Save UserTasks to xml file
			SaveUserTasks (e.Item);
			
			// Remove solution tasks
			
			errors.RemoveItemTasks (e.Item, true);
			userTasks.RemoveItemTasks (e.Item, true);
		}
		
		static FilePath GetUserTasksFilename (WorkspaceItem item)
		{
			FilePath combinePath = item.FileName.ParentDirectory;
			return combinePath.Combine (item.FileName.FileNameWithoutExtension + ".usertasks");
		}
		
		internal static void SaveUserTasks (WorkspaceObject item)
		{
			string fileToSave = GetUserTasksFilename ((WorkspaceItem)item);
			try {
				List<TaskListEntry> utasks = new List<TaskListEntry> (userTasks.GetItemTasks (item, true));
				if (utasks.Count == 0) {
					if (File.Exists (fileToSave))
						File.Delete (fileToSave);
				} else {
					XmlDataSerializer serializer = new XmlDataSerializer (new DataContext ());
					serializer.Serialize (fileToSave, utasks);
				}
			} catch (Exception ex) {
				LoggingService.LogWarning ("Could not save user tasks: " + fileToSave, ex);
			}
		}
		
		
		public static event EventHandler<TaskEventArgs> JumpedToTask;

		internal static void InformJumpToTask (TaskListEntry task)
		{
			EventHandler<TaskEventArgs> handler = JumpedToTask;
			if (handler != null)
				handler (null, new TaskEventArgs (task));
		}

		internal static void InformCommentTasks (CommentTasksChangedEventArgs args)
		{
			var handler = CommentTasksChanged;
			if (handler != null)
				handler (null, args);
		}
		
		public static event EventHandler<CommentTasksChangedEventArgs> CommentTasksChanged;

		public static event EventHandler<TaskEventArgs> TaskToggled;

		public static void FireTaskToggleEvent (object sender, TaskEventArgs e)
		{
			var handler = TaskToggled;
			if (handler != null)
				handler (sender, e);
		}
	}
}

