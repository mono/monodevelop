// 
// TaskListEntry.cs
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
using MonoDevelop.Ide.Gui.Pads;

namespace MonoDevelop.Ide.Tasks
{
	public class TaskListEntry
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

		[ItemProperty (DefaultValue = "")]
		string helpKeyword = string.Empty;
		
		[ItemProperty (DefaultValue = TaskPriority.Normal)]
		TaskPriority priority = TaskPriority.Normal;
		
		[ItemProperty (DefaultValue = TaskSeverity.Information)]
		TaskSeverity severity = TaskSeverity.Information;
		
		[ItemProperty (DefaultValue = false)]
		bool completed;

		[ItemProperty (DefaultValue = "")]
		string category = string.Empty;

		object owner;
		WorkspaceObject parentObject;
		internal int SavedLine;

		public TaskListEntry (FilePath file, string description, int column, int line, TaskSeverity severity)
			: this (file, description, column, line, severity, TaskPriority.Normal, null, null)
		{
		}

		public TaskListEntry (FilePath file, string description, int column, int line, TaskSeverity severity, TaskPriority priority)
			: this (file, description, column, line, severity, priority, null, null)
		{
		}
		
		public TaskListEntry (FilePath file, string description, int column, int line, TaskSeverity severity, TaskPriority priority, WorkspaceObject parent)
			: this (file, description, column, line, severity, priority, parent, null)
		{
		}
		
		public TaskListEntry (FilePath file, string description, int column, int line, TaskSeverity severity, TaskPriority priority, WorkspaceObject parent, object owner)
			: this (file, description, column, line, severity, priority, parent, owner, null)
		{
		}

		public TaskListEntry (FilePath file, string description, int column, int line, TaskSeverity severity, TaskPriority priority, WorkspaceObject parent, object owner, string category)
		{
			this.file = file;
			this.description = description;
			this.column = column;
			this.line = line;
			this.severity = severity;
			this.priority = priority;
			this.owner = owner;
			this.parentObject = parent;
			this.category = category;
		}
		
		public TaskListEntry ()
		{
			
		}
		
		public TaskListEntry (BuildError error)
			: this (error, null)
		{
		}
		
		public TaskListEntry (BuildError error, object owner)
		{
			parentObject = error.SourceTarget as WorkspaceObject;
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
			category = error.Subcategory;
			helpKeyword = error.HelpKeyword;
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

		public string HelpKeyword {
			get {
				return helpKeyword;
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
		
		public WorkspaceObject WorkspaceObject {
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

		public string Category {
			get {
				return category;
			}
			set {
				category = value;
			}
		}

		public string DocumentationLink {
			get; set;
		}

		public bool HasDocumentationLink ()
		{
			foreach (Extensions.ErrorDocumentationProvider ext in Mono.Addins.AddinManager.GetExtensionNodes ("/MonoDevelop/Ide/ErrorDocumentationProvider")) {
				var link = ext.GetDocumentationLink (description);
				if (!string.IsNullOrEmpty (link)) {
					DocumentationLink = link;
					return true;
				}
			}
			return false;
		}

		void ShowDocumentation ()
		{
			if (HasDocumentationLink ())
				DesktopService.ShowUrl (DocumentationLink);
		}

		public virtual void JumpToPosition ()
		{
			if (!file.IsNullOrEmpty) {
				if (System.IO.File.Exists (file)) {
					var project = WorkspaceObject as Project;
					IdeApp.Workbench.OpenDocument (file, project, Math.Max (1, line), Math.Max (1, column));
				} else {
					var pad = IdeApp.Workbench.GetPad<ErrorListPad> ()?.Content as ErrorListPad;
					pad?.FocusOutputView ();
					ShowDocumentation ();
				}
			} else if (parentObject != null) {
				Pad pad = IdeApp.Workbench.GetPad<ProjectSolutionPad> ();
				ProjectSolutionPad spad = pad.Content as ProjectSolutionPad;
				ITreeNavigator nav = spad.TreeView.GetNodeAtObject (parentObject, true);
				if (nav != null) {
					nav.ExpandToNode ();
					nav.Selected = true;
					nav.Expanded = true;
				}
				ShowDocumentation ();
			}
			TaskService.InformJumpToTask (this);
		}

		public void SelectInPad()
		{
			var pad = IdeApp.Workbench.GetPad<ErrorListPad> ();
			if (pad == null)
				return;
			pad.BringToFront ();
			var errorList = pad.Content as ErrorListPad;
			errorList?.SelectTaskListEntry (this);
		}

		public bool BelongsToItem (WorkspaceObject item, bool checkHierarchy)
		{
			if (!checkHierarchy)
				return item == parentObject;
			
			WorkspaceObject cit = parentObject;
			do {
				if (cit == item)
					return true;
				if (cit is SolutionFolderItem) {
					var sfi = (SolutionFolderItem) cit;
					if (sfi.ParentFolder != null)
						cit = sfi.ParentFolder;
					else
						cit = sfi.ParentSolution;
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
