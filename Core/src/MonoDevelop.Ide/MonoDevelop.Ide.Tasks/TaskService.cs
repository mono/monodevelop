// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Properties;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Tasks
{
	public class TaskService : GuiSyncAbstractService
	{
		
		List<Task> tasks  = new List<Task> ();
		Dictionary<TaskType, int> taskCount = new Dictionary<TaskType, int> ();
		Dictionary<string, TaskPriority> priorities = new Dictionary<string, TaskPriority> ();
		List<UserTask> userTasks = new List<UserTask> ();
		string compilerOutput = String.Empty;
		Combine combine;
		
		public TaskService ()
			: base ()
		{
		}
		
		public override void InitializeService ()
		{
			base.InitializeService ();
			ReloadPriories ();
			
//			Disabled until hang is fixed
//			IdeApp.ProjectOperations.CombineOpened += new CombineEventHandler (ProjectServiceSolutionOpened);
//			IdeApp.ProjectOperations.CombineClosed += new CombineEventHandler (ProjectServiceSolutionClosed);

			Runtime.Properties.PropertyChanged += (PropertyEventHandler) Services.DispatchService.GuiDispatch (new PropertyEventHandler (OnPropertyUpdated));
		}

		void ProjectServiceSolutionOpened (object sender, CombineEventArgs e)
		{
			combine = e.Combine;
			e.Combine.FileRenamedInProject += new ProjectFileRenamedEventHandler (ProjectFileRenamed);
			e.Combine.FileRemovedFromProject += new ProjectFileEventHandler (ProjectFileRemoved);
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
					Runtime.LoggingService.Warn ("Could not load user tasks: " + fileToLoad as object, ex);
				}
        	}
		}
				
		void ProjectServiceSolutionClosed (object sender, CombineEventArgs e)
		{
			combine = null;
			e.Combine.FileRenamedInProject -= new ProjectFileRenamedEventHandler (ProjectFileRenamed);
			e.Combine.FileRemovedFromProject -= new ProjectFileEventHandler (ProjectFileRemoved);
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
				Runtime.LoggingService.Warn ("Could not save user tasks: " + fileToSave as object, ex);
			}

			//Cleanup
			Clear ();			
			userTasks.Clear ();
			if (UserTasksChanged != null)
				UserTasksChanged (this, EventArgs.Empty);
		}
		
		void OnPropertyUpdated (object sender, PropertyEventArgs e)
		{
			if (e.Key == "Monodevelop.TaskListTokens" && e.NewValue != e.OldValue)
			{
				ReloadPriories ();
			}
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
		
		void ProjectFileRemoved (object sender, ProjectFileEventArgs e)
		{
			for (int i = 0; i < tasks.Count; ++i) {
				Task curTask = tasks[i];
				if (Path.GetFullPath (curTask.FileName) == Path.GetFullPath (e.ProjectFile.Name)) {
					Remove (curTask);
					--i;
				}
			}
		}
		
		void ProjectFileRenamed (object sender, ProjectFileRenamedEventArgs e)
		{
			for (int i = 0; i < tasks.Count; ++i) {
				Task curTask = tasks[i];
				if (Path.GetFullPath (curTask.FileName) == Path.GetFullPath (e.OldName)) {
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
			string userTasksFilename = Path.Combine(combinePath, combine.Name + ".usertasks");
			return userTasksFilename;
		}
		
		void ReloadPriories ()
		{
			priorities.Clear ();
				string tokens = (string)Runtime.Properties.GetProperty ("Monodevelop.TaskListTokens", "");
				foreach (string token in tokens.Split (';'))
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
			tasks.Add (task);
			if (!taskCount.ContainsKey (task.TaskType)) {
				taskCount[task.TaskType] = 1;
			} else {
				taskCount[task.TaskType]++;
			}
			OnTaskAdded (new TaskEventArgs (task));
		}
		
		public void AddRange (IEnumerable<Task> tasks)
		{
			foreach (Task task in tasks) {
				Add (task);
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
				OnTaskRemoved (new TaskEventArgs (task));
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
			if (fileName == null || tagComments == null) {
				return;
			}
			
			List<Task> newTasks = new List<Task> ();
			foreach (MonoDevelop.Projects.Parser.Tag tag in tagComments) {
				newTasks.Add (new Task (fileName,
				                      tag.Key + tag.CommentString,
				                      tag.Region.BeginColumn - 1,
				                      tag.Region.BeginLine,
				                      TaskType.Comment, priorities[tag.Key]));
			}
			List<Task> oldTasks = new List<Task>();
			
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
			Services.DispatchService.GuiDispatch (new MessageHandler (ShowErrorsCallback));
		}
		
		void ShowErrorsCallback ()
		{
			Pad pad = IdeApp.Workbench.Pads [typeof(ErrorListPad)];
			if (pad != null) pad.BringToFront ();
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
		
		protected virtual void OnTasksCleared (EventArgs e)
		{
			if (TasksCleared != null) {
				TasksCleared (this, e);
			}
		}
		
		public event TaskEventHandler TaskAdded;
		public event TaskEventHandler TaskRemoved;
		public event EventHandler TasksCleared;
		public event EventHandler CompilerOutputChanged;
		public event EventHandler UserTasksChanged;
	}

	public delegate void TaskEventHandler (object sender, TaskEventArgs e);
	
	public class TaskEventArgs : EventArgs
	{
		Task task;
		
		public TaskEventArgs (Task task)
		{
			this.task = task;
		}
		
		public Task Task 
		{
			get { return task; }
		}
	}
}
