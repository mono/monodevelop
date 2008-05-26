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
		
		List<Task> tasks  = new List<Task> ();
		Dictionary<TaskType, int> taskCount = new Dictionary<TaskType, int> ();
		Dictionary<string, TaskPriority> priorities = new Dictionary<string, TaskPriority> ();
		List<UserTask> userTasks = new List<UserTask> ();
		string compilerOutput = String.Empty;
		Combine combine;
		
		public TaskService ()
		{
			ReloadPriories ();
			
			IdeApp.ProjectOperations.CombineOpened += new CombineEventHandler (ProjectServiceSolutionOpened);
			IdeApp.ProjectOperations.CombineClosed += new CombineEventHandler (ProjectServiceSolutionClosed);
			IdeApp.ProjectOperations.FileRenamedInProject += new ProjectFileRenamedEventHandler (ProjectFileRenamed);
			IdeApp.ProjectOperations.FileRemovedFromProject += new ProjectFileEventHandler (ProjectFileRemoved);

			PropertyService.PropertyChanged += (EventHandler<PropertyChangedEventArgs>) DispatchService.GuiDispatch (new EventHandler<PropertyChangedEventArgs> (OnPropertyUpdated));
			
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
		}

		void ProjectServiceSolutionOpened (object sender, CombineEventArgs e)
		{
			combine = e.Combine;
			IdeApp.ProjectOperations.ParserDatabase.CommentTasksChanged += new CommentTasksChangedEventHandler (OnCommentTasksChanged);
			
			// Load all tags that are stored in pidb files
            foreach (Project p in IdeApp.ProjectOperations.CurrentOpenCombine.GetAllProjects ())
            {
                IProjectParserContext pContext = IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (p);
                foreach (ProjectFile file in p.ProjectFiles)
                {
                	TagCollection tags = pContext.GetFileSpecialComments (file.Name); 
                	if (tags !=null)
                		UpdateCommentTags (file.Name, tags);
                }
        	}
        	
        	// Load User Tasks from xml file
        	string fileToLoad = GetUserTasksFilename (e.Combine);
        	if (File.Exists (fileToLoad))
        	{
        		try
        		{
        			XmlSerializer serializer = new XmlSerializer (userTasks.GetType ());
					Stream stream = new FileStream (fileToLoad, FileMode.Open, FileAccess.Read, FileShare.None);
					userTasks = new List<UserTask>((IEnumerable<UserTask>)serializer.Deserialize (stream));
					stream.Close ();
					if (userTasks.Count > 0 && UserTasksChanged != null)
						UserTasksChanged (this, EventArgs.Empty);
				}
				catch (Exception ex)
				{
					LoggingService.LogWarning ("Could not load user tasks: " + fileToLoad, ex);
				}
        	}
		}
				
		void ProjectServiceSolutionClosed (object sender, CombineEventArgs e)
		{
			combine = null;
			IdeApp.ProjectOperations.ParserDatabase.CommentTasksChanged -= new CommentTasksChangedEventHandler (OnCommentTasksChanged);
			
			// Save UserTasks to xml file
			string fileToSave = GetUserTasksFilename (e.Combine);
			try
			{
				XmlSerializer serializer = new XmlSerializer (userTasks.GetType ());
				Stream stream = new FileStream (fileToSave, FileMode.Create, FileAccess.Write, FileShare.None);
				serializer.Serialize (stream, userTasks);
				stream.Close ();
			}
			catch (Exception ex)
			{
				LoggingService.LogWarning ("Could not save user tasks: " + fileToSave, ex);
			}

			//Cleanup
			Clear ();			
			userTasks.Clear ();
			if (UserTasksChanged != null)
				UserTasksChanged (this, EventArgs.Empty);
		}
		
		void OnPropertyUpdated (object sender, PropertyChangedEventArgs e)
		{
			if (e.Key == "Monodevelop.TaskListTokens" && e.NewValue != e.OldValue)
			{
				ReloadPriories ();
				if (combine != null)
				{
					// update priorities
		            foreach (Project p in IdeApp.ProjectOperations.CurrentOpenCombine.GetAllProjects ())
		            {
		                IProjectParserContext pContext = IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (p);
		                foreach (ProjectFile file in p.ProjectFiles)
		                {
		                	TagCollection tags = pContext.GetFileSpecialComments (file.Name); 
		                	if (tags !=null)
		                		UpdateCommentTags (file.Name, tags);
		                }
		        	}
				}
			}
		}
		
		void ProjectFileRemoved (object sender, ProjectFileEventArgs e)
		{
			for (int i = 0; i < tasks.Count; ++i) {
				Task curTask = tasks[i];
				string fname = e.ProjectFile.Name;
				// The method GetFullPath only works if the file exists
				if (File.Exists (fname))
					fname = Path.GetFullPath (fname);
				if (curTask.FileName == fname) {
					Remove (curTask);
					--i;
				}
			}
		}
		
		void ProjectFileRenamed (object sender, ProjectFileRenamedEventArgs e)
		{
			for (int i = 0; i < tasks.Count; ++i) {
				Task curTask = tasks[i];
				if (!string.IsNullOrEmpty (curTask.FileName) && Path.GetFullPath (curTask.FileName) == Path.GetFullPath (e.OldName)) {
					Remove (curTask);
					curTask.FileName = Path.GetFullPath (e.NewName);
					Add (curTask);
					--i;
				}
			}
		}
		
		[AsyncDispatch]
		void OnCommentTasksChanged (object sender, CommentTasksChangedEventArgs e)
		{
			UpdateCommentTags (e.FileName, e.TagComments);
		}

		string GetUserTasksFilename (Combine combine)
		{
			string combineFilename = combine.FileName;
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
				return tasks.Count - GetCount (TaskType.Comment);
			}
		}
		
		public List<Task> Tasks {
			get {
				List<Task> retTasks = new List<Task> ();
				foreach (Task task in tasks) {
					if (task.TaskType != TaskType.Comment) {
						retTasks.Add (task);
					}
				}
				return retTasks;
			}
		}
		
		public List<Task> CommentTasks {
			get {
				List<Task> retTasks = new List<Task> ();
				foreach (Task task in tasks) {
					if (task.TaskType == TaskType.Comment) {
						retTasks.Add (task);
					}
				}
				return retTasks;
			}
		}
		
		public List<UserTask> UserTasks { get { return userTasks; } }
		
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
		
		public string CompilerOutput {
			get {
				return compilerOutput;
			}
			set {
				compilerOutput = value;
				OnCompilerOutputChanged (null);
			}
		}
		
		public void Add (Task task)
		{
			AddInternal (task);
			OnTaskAdded (new TaskEventArgs (new Task[] {task}));
		}
		
		public void AddRange (IEnumerable<Task> tasks)
		{
			foreach (Task task in tasks) {
				AddInternal (task);
			}
			OnTaskAdded (new TaskEventArgs (tasks));
		}
		
		void AddInternal (Task task)
		{
			tasks.Add (task);
			if (!taskCount.ContainsKey (task.TaskType)) {
				taskCount[task.TaskType] = 1;
			} else {
				taskCount[task.TaskType]++;
			}
		}
		
		public void AddUserTasksRange (IEnumerable<UserTask> tasks)
		{
			userTasks.AddRange (tasks);
			if (UserTasksChanged != null)
				UserTasksChanged (this, EventArgs.Empty);
		}
		
		public void Remove (Task task)
		{
			if (tasks.Contains (task)) {
				tasks.Remove (task);
				taskCount[task.TaskType]--;
				OnTaskRemoved (new TaskEventArgs (new Task[] {task}));
			}
		}
		
		public void Clear ()
		{
			taskCount.Clear ();
			tasks.Clear ();
			OnTasksCleared (EventArgs.Empty);
		}
		
		public void ClearExceptCommentTasks ()
		{
			List<Task> commentTasks = new List<Task> (CommentTasks);
			Clear ();
			foreach (Task t in commentTasks) {
				Add (t);
			}
		}

		public void UpdateCommentTags (string fileName, TagCollection tagComments)
		{
			if (fileName == null) {
				return;
			}
			
			List<Task> newTasks = new List<Task> ();
			if (tagComments != null) {  
				foreach (MonoDevelop.Projects.Parser.Tag tag in tagComments) {
					if (!priorities.ContainsKey (tag.Key))
						continue;
					newTasks.Add (new Task (fileName,
					                      tag.Key + tag.CommentString,
					                      tag.Region.BeginColumn - 1,
					                      tag.Region.BeginLine,
					                      TaskType.Comment, priorities[tag.Key]));
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
					Add (task);
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
	
		protected virtual void OnCompilerOutputChanged (EventArgs e) 
		{
			if (CompilerOutputChanged != null) {
				CompilerOutputChanged (this, e);
			}
		}
		
		protected virtual void OnTaskAdded (TaskEventArgs e)
		{
			if (TaskAdded != null) {
				TaskAdded (this, e);
			}
		}
		
		protected virtual void OnTaskRemoved (TaskEventArgs e)
		{
			if (TaskRemoved != null) {
				TaskRemoved (this, e);
			}
		}
		
		protected virtual void OnTaskChanged (TaskEventArgs e)
		{
			if (TaskChanged != null) {
				TaskChanged (this, e);
			}
		}
		
		protected virtual void OnTasksCleared (EventArgs e)
		{
			if (TasksCleared != null) {
				TasksCleared (this, e);
			}
		}
		
		public event TaskEventHandler TaskAdded;
		public event TaskEventHandler TaskRemoved;
		public event TaskEventHandler TaskChanged;
		public event EventHandler TasksCleared;
		public event EventHandler CompilerOutputChanged;
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
