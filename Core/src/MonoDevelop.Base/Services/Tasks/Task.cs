// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.CodeDom.Compiler;
using MonoDevelop.Internal.Project;
using MonoDevelop.Gui;

namespace MonoDevelop.Services
{
	public enum TaskType {
		Error,
		Warning,
		Comment,
		SearchResult
	}
	
	public class Task 
	{
		string   description;
		string   fileName;
		TaskType type;
		Project project;
		int      line;
		int      column;
		

		public override string ToString()
		{
			return String.Format("[Task:File={0}, Line={1}, Column={2}, Type={3}, Description={4}",
			                     fileName,
			                     line,
			                     column,
			                     type,
			                     description);
		}
		
		public Project Project {
			get {
				return project;
			}
		}
		
		public int Line {
			get {
				return line;
			}
		}
		
		public int Column {
			get {
				return column;
			}
		}
		
		public string Description {
			get {
				return description;
			}
		}
		
		public string FileName {
			get {
				return fileName;
			}
			set {
				fileName = value;
			}
		}
		
		public TaskType TaskType {
			get {
				return type;
			}
		}
		
		public Task(string fileName, string description, int column, int line)
		{
			if (fileName != null)
				type = TaskType.SearchResult;
			else
				type = TaskType.Comment;
			this.fileName    = fileName;
			this.description = description.Trim();
			this.column      = column;
			this.line        = line;
		}
		
		public Task(Project project, CompilerError error)
		{
			this.project = project;
			type        = error.IsWarning ? error.ErrorNumber == "COMMENT" ? TaskType.Comment : TaskType.Warning : TaskType.Error;
			column      = error.Column;
			line        = error.Line;
			description = error.ErrorText;
			if (error.ErrorNumber != String.Empty)
				description += "(" + error.ErrorNumber + ")";
			fileName    = error.FileName;
		}
		
		public void JumpToPosition()
		{
			if (fileName != null && fileName.Length > 0) {
				IAsyncOperation op = Runtime.FileService.OpenFile (fileName);
				op.Completed += new OperationHandler (OnFileOpened);
			}
			
//			CompilerResultListItem li = (CompilerResultListItem)OpenTaskView.FocusedItem;
//			
//			string filename   = li.FileName;
//			
//			if (filename == null || filename.Equals(""))
//				return;
//			
//			if (File.Exists(filename)) {
//				string directory  = Path.GetDirectoryName(filename);
//				if (directory[directory.Length - 1] != Path.DirectorySeparatorChar) {
//					directory += Path.DirectorySeparatorChar;
//				}
//				
//				ContentWindow window = OpenWindow(filename);
//			}
		}
		
		private void OnFileOpened (IAsyncOperation op)
		{
			if (!op.Success) return;

			IWorkbenchWindow window = Runtime.FileService.GetOpenFile(fileName);
			if (window == null) {
				return;
			}
			IViewContent content = window.ViewContent;
			if (content is IPositionable) {
				((IPositionable)content).JumpTo(Math.Max(1, line), Math.Max(1, column));
			}
		}
	}
}
