//  TaskService.cs
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
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;

using MonoDevelop.Core.Gui;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Tasks
{
	public class TaskService : GuiSyncObject
	{
		
		Dictionary<object,List<Task>> tasks  = new Dictionary<object,List<Task>> ();
		Dictionary<TaskType, int> taskCount = new Dictionary<TaskType, int> ();
		Dictionary<string, TaskPriority> priorities = new Dictionary<string, TaskPriority> ();
		Dictionary<object,List<UserTask>> userTasks = new Dictionary<object,List<UserTask>> ();
		string compilerOutput = String.Empty;
		
		public TaskService ()
		{
			ReloadPriories ();
			
			IdeApp.Workspace.WorkspaceItemLoaded += ProjectServiceSolutionOpened;
			IdeApp.Workspace.WorkspaceItemUnloaded += ProjectServiceSolutionClosed;
			IdeApp.Workspace.FileRenamedInProject += new ProjectFileRenamedEventHandler (ProjectFileRenamed);
			IdeApp.Workspace.FileRemovedFromProject += new ProjectFileEventHandler (ProjectFileRemoved);

			PropertyService.PropertyChanged += (EventHandler<PropertyChangedEventArgs>) DispatchService.GuiDispatch (new EventHandler<PropertyChangedEventArgs> (OnPropertyUpdated));
			IdeApp.Workspace.ParserDatabase.CommentTasksChanged += new CommentTasksChangedEventHandler (OnCommentTasksChanged);
			
			MonoDevelop.Projects.Text.TextFileService.CommitCountChanges += delegate (object sender, MonoDevelop.Projects.Text.TextFileEventArgs args) {
				foreach (Task task in this.Tasks) {
					if (String.IsNullOrEmpty (task.FileName))
						continue;
					if (Path.GetFullPath (task.FileName) == Path.GetFullPath (args.TextFile.Name)) {
						task.SavedLine = -1;
					}
				}
			};
			
			MonoDevelop.Projects.Text.TextFileService.ResetCountChanges += delegate (object sender, MonoDevelop.Projects.Text.TextFileEventArgs args) {
				List<Task> tasks = new List<Task> ();
				foreach (Task task in this.Tasks) {
					if (String.IsNullOrEmpty (task.FileName))
						continue;
					if (Path.GetFullPath (task.FileName) == Path.GetFullPath (args.TextFile.Name)) {
						if (task.SavedLine != -1) {
							task.Line = task.SavedLine;
							task.SavedLine = -1;
							tasks.Add (task);
						}
					}
				}
				OnTaskChanged (new TaskEventArgs (tasks));
			};
			
			MonoDevelop.Projects.Text.TextFileService.LineCountChanged += delegate (object sender, MonoDevelop.Projects.Text.LineCountEventArgs args) {
				if (args.TextFile == null || String.IsNullOrEmpty (args.TextFile.Name))
					return;
				List<Task> tasks = new List<Task> ();
				foreach (Task task in this.Tasks) {
					if (String.IsNullOrEmpty (task.FileName))
						continue;
					if (Path.GetFullPath (task.FileName) == Path.GetFullPath (args.TextFile.Name) && task.Line - 1 > args.LineNumber || (task.Line - 1 == args.LineNumber && task.Column - 1 >= args.Column)) {
						if (task.SavedLine == -1)
							task.SavedLine = task.Line;
						task.Line += args.LineCount;
						tasks.Add (task);
						
					}
				}
				OnTaskChanged (new TaskEventArgs (tasks));
			};
			
			this.tasks [this] = new List<Task> ();
		}

		void ProjectServiceSolutionOpened (object sender, WorkspaceItemEventArgs e)
		{
			Solution sol = e.Item as Solution;
			if (sol != null) {
				// Load all tags that are stored in pidb files
	            foreach (Project p in sol.GetAllProjects ())
	            {
	                IProjectParserContext pContext = IdeApp.Workspace.ParserDatabase.GetProjectParserContext (p);
					if (pContext == null)
						continue;
	                foreach (ProjectFile file in p.Files)
	                {
	                	TagCollection tags = pContext.GetFileSpecialComments (file.Name); 
	                	if (tags !=null)
	                		UpdateCommentTags (sol, file.Name, tags);
	                }
	        	}
			}
			
			List<UserTask> utasks = new List<UserTask> ();
			userTasks [e.Item] = utasks;
			
			List<Task> stasks = new List<Task> ();
			tasks [e.Item] = stasks;
			
        	// Load User Tasks from xml file
        	string fileToLoad = GetUserTasksFilename (e.Item);
        	if (File.Exists (fileToLoad))
        	{
        		try
        		{
        			XmlSerializer serializer = new XmlSerializer (utasks.GetType ());
					Stream stream = new FileStream (fileToLoad, FileMode.Open, FileAccess.Read, FileShare.None);
					utasks.AddRange ((IEnumerable<UserTask>)serializer.Deserialize (stream));
					stream.Close ();
					if (utasks.Count > 0 && UserTasksChanged != null)
						UserTasksChanged (this, EventArgs.Empty);
				}
				catch (Exception ex)
				{
					LoggingService.LogWarning ("Could not load user tasks: " + fileToLoad, ex);
				}
        	}
		}
				
		void ProjectServiceSolutionClosed (object sender, WorkspaceItemEventArgs e)
		{
			// Save UserTasks to xml file
			string fileToSave = GetUserTasksFilename (e.Item);
			try
			{
				List<UserTask> utasks = userTasks [e.Item];
				XmlSerializer serializer = new XmlSerializer (utasks.GetType ());
				Stream stream = new FileStream (fileToSave, FileMode.Create, FileAccess.Write, FileShare.None);
				serializer.Serialize (stream, utasks);
				stream.Close ();
			}
			catch (Exception ex)
			{
				LoggingService.LogWarning ("Could not save user tasks: " + fileToSave, ex);
			}
			
			// Remove solution tasks
			
			List<Task> ttasks;
			if (tasks.TryGetValue (e.Item, out ttasks)) {
				tasks.Remove (e.Item);
			
				foreach (Task t in ttasks)
					taskCount [t.TaskType]--;
				
				OnTaskRemoved (new TaskEventArgs (ttasks.ToArray ()));
			}

			userTasks.Remove (e.Item);
			if (UserTasksChanged != null)
				UserTasksChanged (this, EventArgs.Empty);
		}
		
		void OnPropertyUpdated (object sender, PropertyChangedEventArgs e)
		{
			if (e.Key == "Monodevelop.TaskListTokens" && e.NewValue != e.OldValue)
			{
				ReloadPriories ();
				// update priorities
	            foreach (Project p in IdeApp.Workspace.GetAllProjects ())
	            {
	                IProjectParserContext pContext = IdeApp.Workspace.ParserDatabase.GetProjectParserContext (p);
	                foreach (ProjectFile file in p.Files)
	                {
	                	TagCollection tags = pContext.GetFileSpecialComments (file.Name); 
	                	if (tags !=null)
	                		UpdateCommentTags (p.ParentSolution, file.Name, tags);
	                }
	        	}
			}
		}
		
		void ProjectFileRemoved (object sender, ProjectFileEventArgs e)
		{
			foreach (Task curTask in new List<Task> (Tasks)) {
				if (curTask.FileName == e.ProjectFile.FilePath)
					Remove (curTask);
			}
		}
		
		void ProjectFileRenamed (object sender, ProjectFileRenamedEventArgs e)
		{
			List<Task> ctasks = new List<Task> (Tasks);
			foreach (Task curTask in ctasks) {
				if (curTask.FileName == e.OldName) {
					Remove (curTask);
					curTask.FileName = e.NewName;
					Add (curTask, curTask.OwnerItem);
				}
			}
		}
		
		[AsyncDispatch]
		void OnCommentTasksChanged (object sender, CommentTasksChangedEventArgs e)
		{
			Project p = IdeApp.Workspace.GetProjectContainingFile (e.FileName);
			if (p != null)
				UpdateCommentTags (p.ParentSolution, e.FileName, e.TagComments);
		}
		
		public void ClearExceptCommentTasks ()
		{
			List<Task> tlist = new List<Task> ();
			foreach (Task t in Tasks) {
				if (t.TaskType != TaskType.Comment)
					tlist.Add (t);
			}
			foreach (Task t in tlist)
				InternalRemove (t);
			
			OnTaskRemoved (new TaskEventArgs (tlist.ToArray ()));
		}
		
		string GetUserTasksFilename (WorkspaceItem item)
		{
			string combineFilename = item.FileName;
			string combinePath = Path.GetDirectoryName (combineFilename);
			string userTasksFilename = Path.Combine(combinePath, Path.GetFileNameWithoutExtension(combineFilename) + ".usertasks");
			return userTasksFilename;
		}
		
		void ReloadPriories ()
		{
			priorities.Clear ();
			string tokens = (string)PropertyService.Get ("Monodevelop.TaskListTokens", "FIXME:2;TODO:1;HACK:1;UNDONE:0");
			foreach (string token in tokens.Split (new char[] {';'}, StringSplitOptions.RemoveEmptyEntries))
			{
				int pos = token.IndexOf (':');
				if (pos != -1)
				{
					int priority;
					if (! int.TryParse (token.Substring (pos + 1), out priority))
						priority = 1;
					priorities.Add (token.Substring (0, pos), (TaskPriority)priority);
				} else
				{
					priorities.Add (token, TaskPriority.Normal);
				}
			}
		}
		
		public int TaskCount {
			get {
				int c = 0;
				foreach (List<Task> tt in tasks.Values)
					c += tt.Count;
				return c - GetCount (TaskType.Comment);
			}
		}
		
		public List<Task> Tasks {
			get {
				List<Task> retTasks = new List<Task> ();
				foreach (List<Task> tt in tasks.Values) {
					foreach (Task task in tt) {
						if (task.TaskType != TaskType.Comment)
							retTasks.Add (task);
					}
				}
				return retTasks;
			}
		}
		
		public List<Task> CommentTasks {
			get {
				List<Task> retTasks = new List<Task> ();
				foreach (List<Task> tt in tasks.Values) {
					foreach (Task task in tt) {
						if (task.TaskType == TaskType.Comment) {
							retTasks.Add (task);
						}
					}
				}
				return retTasks;
			}
		}
		
		public List<UserTask> UserTasks {
			get {
				List<UserTask> retTasks = new List<UserTask> ();
				foreach (List<UserTask> tt in userTasks.Values)
					retTasks.AddRange (tt);
				return retTasks;
			} 
		}
		
		int GetCount (TaskType type)
		{
			if (!taskCount.ContainsKey (type)) {
				return 0;
			}
			return taskCount[type];
		}
		
		public int ErrorsCount {
			get {
				return GetCount (TaskType.Error);
			}
		}
		
		public int WarningsCount {
			get {
				return GetCount (TaskType.Warning);
			}
		}
		
		public int MessagesCount {
			get {
				return GetCount (TaskType.Message);
			}
		}
		
		public bool SomethingWentWrong {
			get {
				return GetCount (TaskType.Error) + GetCount (TaskType.Warning) > 0;
			}
		}
		
		public bool HasCriticalErrors (bool treatWarningsAsErrors)
		{
			if (treatWarningsAsErrors) {
				return SomethingWentWrong;
			} else {
				return GetCount (TaskType.Error) > 0;
			}
		}
		
		public void Add (Task task)
		{
			Add (task, this);
		}
		
		public void Add (Task task, object owner)
		{
			task.OwnerItem = owner;
			AddInternal (task);
			OnTaskAdded (new TaskEventArgs (new Task[] {task}));
		}
		
		public void AddRange (IEnumerable<Task> tasks)
		{
			AddRange (tasks, this);
		}
		
		public void AddRange (IEnumerable<Task> tasks, object owner)
		{
			foreach (Task task in tasks) {
				task.OwnerItem = owner;
				AddInternal (task);
			}
			OnTaskAdded (new TaskEventArgs (tasks));
		}
		
		void AddInternal (Task task)
		{
			if (task.OwnerItem == null)
				throw new InvalidOperationException ();
			
			List<Task> tlist;
			if (!tasks.TryGetValue (task.OwnerItem, out tlist))
				return;
			tlist.Add (task);
			int count;
			if (!taskCount.TryGetValue (task.TaskType, out count))
				taskCount[task.TaskType] = 1;
			else
				taskCount[task.TaskType] = count + 1;
		}
		
/*		public void AddUserTasksRange (IEnumerable<UserTask> tasks)
		{
			userTasks.AddRange (tasks);
			if (UserTasksChanged != null)
				UserTasksChanged (this, EventArgs.Empty);
		}
*/
		
		public void Remove (Task task)
		{
			if (InternalRemove (task))
				OnTaskRemoved (new TaskEventArgs (new Task[] {task}));
		}
		
		bool InternalRemove (Task task)
		{
			if (task.OwnerItem != null) {
				List<Task> tlist;
				if (tasks.TryGetValue (task.OwnerItem, out tlist) && tlist.Contains (task)) {
					tlist.Remove (task);
					taskCount[task.TaskType]--;
					return true;
				}
			}
			return false;
		}
		
		public void UpdateCommentTags (Solution sol, string fileName, TagCollection tagComments)
		{
			if (fileName == null) {
				return;
			}
			
			List<Task> newTasks = new List<Task> ();
			if (tagComments != null) {  
				foreach (MonoDevelop.Projects.Parser.Tag tag in tagComments) {
					if (!priorities.ContainsKey (tag.Key))
						continue;
					Task t = new Task (fileName,
					                      tag.Key + tag.CommentString,
					                      tag.Region.BeginColumn - 1,
					                      tag.Region.BeginLine,
					                      TaskType.Comment, priorities[tag.Key]);
					t.OwnerItem = sol;
					newTasks.Add (t);
				}
			}
			List<Task> oldTasks = new List<Task> ();
			
			foreach (Task task in CommentTasks) {
				if (Path.GetFullPath (task.FileName) == Path.GetFullPath (fileName)) {
					oldTasks.Add (task);
				}
			}
			
			for (int i = 0; i < newTasks.Count; ++i) {
				for (int j = 0; j < oldTasks.Count; ++j) {
					if (oldTasks[j] != null &&
					    newTasks[i].Line        == oldTasks[j].Line &&
					    newTasks[i].Column      == oldTasks[j].Column &&
					    newTasks[i].Description == oldTasks[j].Description &&
					    newTasks[i].Priority 	== oldTasks[j].Priority)
					{
						newTasks[i] = null;
						oldTasks[j] = null;
						break;
					}
				}
			}
			
			foreach (Task task in newTasks) {
				if (task != null) {
					Add (task, sol);
				}
			}
			
			foreach (Task task in oldTasks) {
				if (task != null) {
					Remove (task);
				}
			}
		}
		
		public void ShowErrors ()
		{
			DispatchService.GuiDispatch (new MessageHandler (ShowErrorsCallback));
		}
		
		void ShowErrorsCallback ()
		{
			Pad pad = IdeApp.Workbench.GetPad<ErrorListPad> ();
			if (pad != null)
				pad.BringToFront ();
		}
	
		protected void OnTaskAdded (TaskEventArgs e)
		{
			if (TaskAdded != null) {
				TaskAdded (this, e);
			}
		}
		
		protected void OnTaskRemoved (TaskEventArgs e)
		{
			if (TaskRemoved != null) {
				TaskRemoved (this, e);
			}
		}
		
		protected void OnTaskChanged (TaskEventArgs e)
		{
			if (TaskChanged != null) {
				TaskChanged (this, e);
			}
		}
		
		public event TaskEventHandler TaskAdded;
		public event TaskEventHandler TaskRemoved;
		public event TaskEventHandler TaskChanged;
		public event EventHandler UserTasksChanged;
	}

	public delegate void TaskEventHandler (object sender, TaskEventArgs e);
	
	public class TaskEventArgs : EventArgs
	{
		IEnumerable<Task> tasks;
		
		public TaskEventArgs (IEnumerable<Task> tasks)
		{
			this.tasks = tasks;
		}
		
		public IEnumerable<Task> Tasks
		{
			get { return tasks; }
		}
	}
}
