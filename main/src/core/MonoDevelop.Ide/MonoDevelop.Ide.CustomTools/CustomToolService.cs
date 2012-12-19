// 
// CustomToolService.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using MonoDevelop.Ide.Extensions;
using System.Collections.Generic;
using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Projects;
using System.IO;
using MonoDevelop.Ide.Tasks;
using System.CodeDom.Compiler;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.ProgressMonitoring;

namespace MonoDevelop.Ide.CustomTools
{
	public static class CustomToolService
	{
		static Dictionary<string,CustomToolExtensionNode> nodes = new Dictionary<string,CustomToolExtensionNode> ();
		
		static Dictionary<string,IAsyncOperation> runningTasks = new Dictionary<string, IAsyncOperation> ();
		
		static CustomToolService ()
		{
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/CustomTools", delegate(object sender, ExtensionNodeEventArgs args) {
				var node = (CustomToolExtensionNode)args.ExtensionNode;
				switch (args.Change) {
				case ExtensionChange.Add:
					if (nodes.ContainsKey (node.Name))
						LoggingService.LogError ("Duplicate custom tool name '{0}'", node.Name);
					else
						nodes.Add (node.Name, node);
					break;
				case ExtensionChange.Remove:
					nodes.Remove (node.Name);
					break;
				}
			});
			IdeApp.Workspace.FileChangedInProject += delegate (object sender, ProjectFileEventArgs args) {
				foreach (ProjectFileEventInfo e in args)
					Update (e.ProjectFile, false);
			};
			IdeApp.Workspace.FilePropertyChangedInProject += delegate (object sender, ProjectFileEventArgs args) {
				foreach (ProjectFileEventInfo e in args)
					Update (e.ProjectFile, false);
			};
			//FIXME: handle the rename
			//MonoDevelop.Ide.Gui.IdeApp.Workspace.FileRenamedInProject
		}
		
		internal static void Init ()
		{
			//forces static ctor to run
		}
		
		static ISingleFileCustomTool GetGenerator (ProjectFile file)
		{
			CustomToolExtensionNode node;
			if (!string.IsNullOrEmpty (file.Generator) && nodes.TryGetValue (file.Generator, out node)) {
				try {
					return node.Tool;
				} catch (Exception ex) {
					LoggingService.LogError ("Error loading generator '" + file.Generator + "'", ex);
					nodes.Remove (file.Generator);
				}
			}
			return null;
		}
		
		public static void Update (ProjectFile file, bool force)
		{
			var tool = GetGenerator (file);
			if (tool == null)
				return;
			
			ProjectFile genFile = null;
			if (!string.IsNullOrEmpty (file.LastGenOutput))
				genFile = file.Project.Files.GetFile (file.FilePath.ParentDirectory.Combine (file.LastGenOutput));
			
			if (!force && genFile != null && File.Exists (genFile.FilePath) && 
			    File.GetLastWriteTime (file.FilePath) < File.GetLastWriteTime (genFile.FilePath)) {
				return;
			}
			
			TaskService.Errors.ClearByOwner (file);
			
			//if this file is already being run, cancel it
			lock (runningTasks) {
				IAsyncOperation runningTask;
				if (runningTasks.TryGetValue (file.FilePath, out runningTask)) {
					runningTask.Cancel ();
					runningTasks.Remove (file.FilePath);
				}
			}
			
			var monitor = IdeApp.Workbench.ProgressMonitors.GetToolOutputProgressMonitor (false);
			var result = new SingleFileCustomToolResult ();
			var aggOp = new AggregatedOperationMonitor (monitor);
			try {
				monitor.BeginTask (GettextCatalog.GetString ("Running generator '{0}' on file '{1}'...", file.Generator, file.Name), 1);
				var op = tool.Generate (monitor, file, result);
				runningTasks.Add (file.FilePath, op);
				aggOp.AddOperation (op);
				op.Completed += delegate {
					lock (runningTasks) {
						IAsyncOperation runningTask;
						if (runningTasks.TryGetValue (file.FilePath, out runningTask) && runningTask == op) {
							runningTasks.Remove (file.FilePath);
							UpdateCompleted (monitor, aggOp, file, genFile, result);
						} else {
							//it was cancelled because another was run for the same file, so just clean up
							aggOp.Dispose ();
							monitor.EndTask ();
							monitor.ReportWarning (GettextCatalog.GetString ("Cancelled because generator ran again for the same file"));
							monitor.Dispose ();
						}
					}
				};
			} catch (Exception ex) {
				result.UnhandledException = ex;
				UpdateCompleted (monitor, aggOp, file, genFile, result);
			}
		}
		
		static void UpdateCompleted (IProgressMonitor monitor, AggregatedOperationMonitor aggOp,
		                             ProjectFile file, ProjectFile genFile, SingleFileCustomToolResult result)
		{
			monitor.EndTask ();
			aggOp.Dispose ();
			
			if (monitor.IsCancelRequested) {
				monitor.ReportError (GettextCatalog.GetString ("Cancelled"), null);
				monitor.Dispose ();
				return;
			}
			
			string genFileName;
			try {
				
				bool broken = false;
				
				if (result.UnhandledException != null) {
					broken = true;
					string msg = GettextCatalog.GetString ("The '{0}' code generator crashed", file.Generator);
					result.Errors.Add (new CompilerError (file.Name, 0, 0, "", msg + ": " + result.UnhandledException.Message));
					monitor.ReportError (msg, result.UnhandledException);
					LoggingService.LogError (msg, result.UnhandledException);
				}
				
				genFileName = result.GeneratedFilePath.IsNullOrEmpty?
					null : result.GeneratedFilePath.ToRelative (file.FilePath.ParentDirectory);
				
				bool validName = !string.IsNullOrEmpty (genFileName)
					&& genFileName.IndexOfAny (new char[] { '/', '\\' }) < 0
					&& FileService.IsValidFileName (genFileName);
				
				if (!broken && !validName) {
					broken = true;
					string msg = GettextCatalog.GetString ("The '{0}' code generator output invalid filename '{1}'",
					                                       file.Generator, result.GeneratedFilePath);
					result.Errors.Add (new CompilerError (file.Name, 0, 0, "", msg));
					monitor.ReportError (msg, null);
				}
				
				if (result.Errors.Count > 0) {
					foreach (CompilerError err in result.Errors)
						TaskService.Errors.Add (new Task (file.FilePath, err.ErrorText, err.Column, err.Line,
							                                  err.IsWarning? TaskSeverity.Warning : TaskSeverity.Error,
							                                  TaskPriority.Normal, file.Project.ParentSolution, file));
				}
				
				if (broken)
					return;
				
				if (result.Success)
					monitor.ReportSuccess ("Generated file successfully.");
				else
					monitor.ReportError ("Failed to generate file. See error pad for details.", null);
				
			} finally {
				monitor.Dispose ();
			}
			
			if (!result.GeneratedFilePath.IsNullOrEmpty && File.Exists (result.GeneratedFilePath)) {
				Gtk.Application.Invoke (delegate {
					if (genFile == null) {
						genFile = file.Project.AddFile (result.GeneratedFilePath);
					} else if (result.GeneratedFilePath != genFile.FilePath) {
						genFile.Name = result.GeneratedFilePath;
					}
					file.LastGenOutput = genFileName;
					genFile.DependsOn = file.FilePath.FileName;
					
					IdeApp.ProjectOperations.Save (file.Project);
				});
			}
		}
		
		public static void HandleRename (ProjectFileRenamedEventArgs e)
		{
			foreach (ProjectFileEventInfo args in e) {
				var file = args.ProjectFile;
				var tool = GetGenerator (file);
				if (tool == null)
					continue;
			}
		}
	}
}

