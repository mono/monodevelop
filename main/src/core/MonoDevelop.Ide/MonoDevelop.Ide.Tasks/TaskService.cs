// 
// IdeServices.TaskService.cs
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

using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using System.Threading.Tasks;

namespace MonoDevelop.Ide.Tasks
{
	[DefaultServiceImplementation]
	public class TaskService: Service
	{
		TaskStore errors = new TaskStore ();
		TaskStore userTasks = new TaskStore ();
		
		bool errorBubblesVisible = true;
		bool warningBubblesVisible = true;

		RootWorkspace rootWorkspace;

		protected override Task OnInitialize (ServiceProvider serviceProvider)
		{
			serviceProvider.WhenServiceInitialized<RootWorkspace> (s => {
				rootWorkspace = s;
				rootWorkspace.WorkspaceItemLoaded += OnWorkspaceItemLoaded;
				rootWorkspace.WorkspaceItemUnloaded += OnWorkspaceItemUnloaded;
				CommentTasksProvider.Legacy.Initialize ();
			});
			errors.ItemName = GettextCatalog.GetString ("Warning/Error");
			userTasks.ItemName = GettextCatalog.GetString ("User Task");
			return Task.CompletedTask;
		}

		protected override Task OnDispose ()
		{
			if (rootWorkspace != null) {
				rootWorkspace.WorkspaceItemLoaded -= OnWorkspaceItemLoaded;
				rootWorkspace.WorkspaceItemUnloaded -= OnWorkspaceItemUnloaded;
			}
			errors.Dispose ();
			userTasks.Dispose ();
			return base.OnDispose ();
		}

		public TaskStore Errors {
			get { return errors; }
		}
		
		public TaskStore UserTasks {
			get { return userTasks; }
		}
		
		public bool ErrorBubblesVisible {
			get { return errorBubblesVisible; }
			set {
				if (errorBubblesVisible != value) {
					errorBubblesVisible = value;
					if (BubblesVisibilityChanged != null)
						BubblesVisibilityChanged (null, EventArgs.Empty);
				}
			}
		}
		
		public bool WarningBubblesVisible {
			get { return warningBubblesVisible; }
			set {
				if (warningBubblesVisible != value) {
					warningBubblesVisible = value;
					if (BubblesVisibilityChanged != null)
						BubblesVisibilityChanged (null, EventArgs.Empty);
				}
			}
		}
		
		public event EventHandler BubblesVisibilityChanged;
		
		public void ShowErrors ()
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
		public void ShowStatus (TaskListEntry t)
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
			
		void OnWorkspaceItemLoaded (object sender, WorkspaceItemEventArgs e)
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
		
		void OnWorkspaceItemUnloaded (object sender, WorkspaceItemEventArgs e)
		{
			// Save UserTasks to xml file
			SaveUserTasks (e.Item);
			
			// Remove solution tasks
			
			errors.RemoveItemTasks (e.Item, true);
			userTasks.RemoveItemTasks (e.Item, true);
		}
		
		FilePath GetUserTasksFilename (WorkspaceItem item)
		{
			FilePath combinePath = item.FileName.ParentDirectory;
			return combinePath.Combine (item.FileName.FileNameWithoutExtension + ".usertasks");
		}
		
		internal void SaveUserTasks (WorkspaceObject item)
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
		
		
		public event EventHandler<TaskEventArgs> JumpedToTask;

		internal void InformJumpToTask (TaskListEntry task)
		{
			EventHandler<TaskEventArgs> handler = JumpedToTask;
			if (handler != null)
				handler (null, new TaskEventArgs (task));
		}

		internal void InformCommentTasks (CommentTasksChangedEventArgs args)
		{
			if (args.Changes.Count == 0)
				return;

			var handler = CommentTasksChanged;
			if (handler != null)
				handler (null, args);
		}
		
		public event EventHandler<CommentTasksChangedEventArgs> CommentTasksChanged;

		public event EventHandler<TaskEventArgs> TaskToggled;

		public void FireTaskToggleEvent (object sender, TaskEventArgs e)
		{
			var handler = TaskToggled;
			if (handler != null)
				handler (sender, e);
		}
	}
}

