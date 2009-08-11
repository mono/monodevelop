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

using MonoDevelop.Core.Gui;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
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
		
		static TaskService ()
		{
			IdeApp.Workspace.WorkspaceItemLoaded += OnWorkspaceItemLoaded;
			IdeApp.Workspace.WorkspaceItemUnloaded += OnWorkspaceItemUnloaded;
		}
		
		public static TaskStore Errors {
			get { return errors; }
		}
		
		public static TaskStore UserTasks {
			get { return userTasks; }
		}
		
		public static void ShowErrors ()
		{
			Pad errorsPad = IdeApp.Workbench.GetPad<MonoDevelop.Ide.Gui.Pads.ErrorListPad> ();
			if (errorsPad != null) {
				errorsPad.Visible = true;
				errorsPad.BringToFront ();
			}
		}
			
		static void OnWorkspaceItemLoaded (object sender, WorkspaceItemEventArgs e)
		{
			string fileToLoad = GetUserTasksFilename (e.Item);
			
			userTasks.BeginTaskUpdates ();
			try {
				// Load User Tasks from xml file
				if (File.Exists (fileToLoad)) {
					XmlDataSerializer serializer = new XmlDataSerializer (new DataContext ());
					List<Task> ts = (List<Task>) serializer.Deserialize (fileToLoad, typeof(List<Task>));
					foreach (Task t in ts) {
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
		
		internal static void SaveUserTasks (IWorkspaceObject item)
		{
			string fileToSave = GetUserTasksFilename ((WorkspaceItem)item);
			try {
				List<Task> utasks = new List<Task> (userTasks.GetItemTasks (item, true));
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
	}
}

