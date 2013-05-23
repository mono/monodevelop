// 
// Task.cs
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
using System.Collections;
using System.CodeDom.Compiler;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.Ide.Tasks
{
	public class Task
	{
		[ItemProperty]
		FilePath file;
		
		[ItemProperty (DefaultValue = 0)]
		int line;
		
		[ItemProperty (DefaultValue = 0)]
		int column;
		
		[ItemProperty (DefaultValue = "")]
		string description = string.Empty;
		
		[ItemProperty (DefaultValue = "")]
		string code = string.Empty;
		
		[ItemProperty (DefaultValue = TaskPriority.Normal)]
		TaskPriority priority = TaskPriority.Normal;
		
		[ItemProperty (DefaultValue = TaskSeverity.Information)]
		TaskSeverity severity = TaskSeverity.Information;
		
		[ItemProperty (DefaultValue = false)]
		bool completed;
		
		object owner;
		IWorkspaceObject parentObject;
		internal int SavedLine;

		public Task (FilePath file, string description, int column, int line, TaskSeverity severity)
			: this (file, description, column, line, severity, TaskPriority.Normal, null, null)
		{
		}

		public Task (FilePath file, string description, int column, int line, TaskSeverity severity, TaskPriority priority)
			: this (file, description, column, line, severity, priority, null, null)
		{
		}
		
		public Task (FilePath file, string description, int column, int line, TaskSeverity severity, TaskPriority priority, IWorkspaceObject parent)
			: this (file, description, column, line, severity, priority, parent, null)
		{
		}
		
		public Task (FilePath file, string description, int column, int line, TaskSeverity severity, TaskPriority priority, IWorkspaceObject parent, object owner)
		{
			this.file = file;
			this.description = description;
			this.column = column;
			this.line = line;
			this.severity = severity;
			this.priority = priority;
			this.owner = owner;
			this.parentObject = parent;
		}
		
		public Task ()
		{
			
		}
		
		public Task (BuildError error)
			: this (error, null)
		{
		}
		
		public Task (BuildError error, object owner)
		{
			parentObject = error.SourceTarget;
			file = error.FileName;
			this.owner = owner;
			description = error.ErrorText;
			column = error.Column;
			line = error.Line;
			if (!string.IsNullOrEmpty (error.ErrorNumber))
				description += " (" + error.ErrorNumber + ")";
			if (error.IsWarning)
				severity = error.ErrorNumber == "COMMENT" ? TaskSeverity.Information : TaskSeverity.Warning;
			else
				severity = TaskSeverity.Error;
			priority = TaskPriority.Normal;
			code = error.ErrorNumber;
		}
		
		public int Column {
			get {
				return column;
			}
		}
		
		public bool Completed {
			get {
				return completed;
			}
			set {
				completed = value;
			}
		}
									
		public string Description {
			get {
				return description;
			}
			set {
				description = value;
			}
		}
		
		public string Code {
			get {
				return code;
			}
		}
		
		public FilePath FileName {
			get {
				return file;
			}
			internal set {
				file = value;
			}
		}
		
		public int Line {
			get {
				return line;
			}
			internal set {
				line = value;
			}
		}
		
		public object Owner {
			get {
				return owner;
			}
			internal set {
				owner = value;
			}
		}
		
		public IWorkspaceObject WorkspaceObject {
			get {
				return parentObject;
			}
			set {
				if (parentObject != null)
					throw new InvalidOperationException ("Owner already set");
				parentObject = value;
			}
		}
		
		public TaskPriority Priority {
			get {
				return priority;
			}
			set {
				priority = value;
			}
		}
		
		public TaskSeverity Severity {
			get {
				return severity;
			}
		}
				
		
		public virtual void JumpToPosition()
		{
			if (!file.IsNullOrEmpty) {
				var doc = IdeApp.Workbench.OpenDocument (file, Math.Max (1, line), Math.Max (1, column));
				var project = WorkspaceObject as Project;
				if (doc != null && project != null)
					doc.SetProject (project);
			} else if (parentObject != null) {
				Pad pad = IdeApp.Workbench.GetPad<ProjectSolutionPad> ();
				ProjectSolutionPad spad = pad.Content as ProjectSolutionPad;
				ITreeNavigator nav = spad.TreeView.GetNodeAtObject (parentObject, true);
				if (nav != null) {
					nav.ExpandToNode ();
					nav.Selected = true;
					nav.Expanded = true;
				}
			}
			TaskService.InformJumpToTask (this);
		}
		
		public bool BelongsToItem (IWorkspaceObject item, bool checkHierarchy)
		{
			if (!checkHierarchy)
				return item == parentObject;
			
			IWorkspaceObject cit = parentObject;
			do {
				if (cit == item)
					return true;
				if (cit is SolutionItem) {
					SolutionItem si = (SolutionItem) cit;
					if (si.ParentFolder != null)
						cit = si.ParentFolder;
					else
						cit = si.ParentSolution;
				}
				else if (cit is WorkspaceItem) {
					cit = ((WorkspaceItem)cit).ParentWorkspace;
				}
				else
					cit = null;
			} while (cit != null);
			
			return false;
		}
	}
}
