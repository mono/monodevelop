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
using System.CodeDom.Compiler;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.ProgressMonitoring;
using System.Linq;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using IdeTask = MonoDevelop.Ide.Tasks.UserTask;
using MonoDevelop.Ide.Tasks;

namespace MonoDevelop.Ide.CustomTools
{
	public static class CustomToolService
	{
		static readonly Dictionary<string,CustomToolExtensionNode> nodes = new Dictionary<string,CustomToolExtensionNode> ();

		class TaskInfo {
			public Task Task;
			public CancellationTokenSource CancellationTokenSource;
			public SingleFileCustomToolResult Result;
		}

		static readonly Dictionary<string,TaskInfo> runningTasks = new Dictionary<string, TaskInfo> ();
		
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
		
		static ISingleFileCustomTool GetGenerator (string name)
		{
			if (string.IsNullOrEmpty (name))
				return null;

			if (name.StartsWith ("msbuild:", StringComparison.OrdinalIgnoreCase)) {
				string target = name.Substring ("msbuild:".Length).Trim ();
				if (string.IsNullOrEmpty (target))
					return null;
				return new MSBuildCustomTool (target);
			}

			CustomToolExtensionNode node;
			if (nodes.TryGetValue (name, out node)) {
				try {
					return node.Tool;
				} catch (Exception ex) {
					LoggingService.LogError ("Error loading generator '" + name + "'", ex);
					nodes.Remove (name);
				}
			}
			return null;
		}
		
		public async static void Update (ProjectFile file, bool force)
		{
			var tool = GetGenerator (file.Generator);
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
				TaskInfo runningTask;
				if (runningTasks.TryGetValue (file.FilePath, out runningTask)) {
					runningTask.CancellationTokenSource.Cancel ();
					runningTasks.Remove (file.FilePath);
				}
			}

			CancellationTokenSource cs = new CancellationTokenSource ();
			var monitor = IdeApp.Workbench.ProgressMonitors.GetToolOutputProgressMonitor (false).WithCancellationSource (cs);
			var result = new SingleFileCustomToolResult ();
			Task op;
			try {
				monitor.BeginTask (GettextCatalog.GetString ("Running generator '{0}' on file '{1}'...", file.Generator, file.Name), 1);
				op = tool.Generate (monitor, file, result);
				runningTasks.Add (file.FilePath, new TaskInfo {
					Task = op,
					CancellationTokenSource = cs,
					Result = result
				});
			} catch (Exception ex) {
				result.UnhandledException = ex;
				UpdateCompleted (monitor, file, genFile, result);
				return;
			}
			await op;
			lock (runningTasks) {
				TaskInfo runningTask;
				if (runningTasks.TryGetValue (file.FilePath, out runningTask) && runningTask.Task == op) {
					runningTasks.Remove (file.FilePath);
					UpdateCompleted (monitor, file, genFile, result);
				} else {
					//it was cancelled because another was run for the same file, so just clean up
					monitor.EndTask ();
					monitor.ReportWarning (GettextCatalog.GetString ("Cancelled because generator ran again for the same file"));
					monitor.Dispose ();
				}
			}
		}
		
		static void UpdateCompleted (ProgressMonitor monitor, ProjectFile file, ProjectFile genFile, SingleFileCustomToolResult result)
		{
			monitor.EndTask ();

			if (monitor.CancellationToken.IsCancellationRequested) {
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
				
				if (!string.IsNullOrEmpty (genFileName)) {
					bool validName = genFileName.IndexOfAny (new char[] { '/', '\\' }) < 0
						&& FileService.IsValidFileName (genFileName);
					
					if (!broken && !validName) {
						broken = true;
						string msg = GettextCatalog.GetString ("The '{0}' code generator output invalid filename '{1}'",
						                                       file.Generator, result.GeneratedFilePath);
						result.Errors.Add (new CompilerError (file.Name, 0, 0, "", msg));
						monitor.ReportError (msg, null);
					}
				}
				
				if (result.Errors.Count > 0) {
					DispatchService.GuiDispatch (delegate {
						foreach (CompilerError err in result.Errors)
							TaskService.Errors.Add (new UserTask (file.FilePath, err.ErrorText, err.Column, err.Line,
								err.IsWarning? TaskSeverity.Warning : TaskSeverity.Error,
								TaskPriority.Normal, file.Project.ParentSolution, file));
					});
				}
				
				if (broken)
					return;

				if (result.Success)
					monitor.ReportSuccess ("Generated file successfully.");
				else if (result.SuccessWithWarnings)
					monitor.ReportSuccess ("Warnings in file generation.");
				else
					monitor.ReportError ("Errors in file generation.", null);
				
			} finally {
				monitor.Dispose ();
			}

			if (result.GeneratedFilePath.IsNullOrEmpty || !File.Exists (result.GeneratedFilePath))
				return;

			// broadcast a change event so text editors etc reload the file
			FileService.NotifyFileChanged (result.GeneratedFilePath);

			// add file to project, update file properties, etc
			Gtk.Application.Invoke (delegate {
				bool projectChanged = false;
				if (genFile == null) {
					genFile = file.Project.AddFile (result.GeneratedFilePath, result.OverrideBuildAction);
					projectChanged = true;
				} else if (result.GeneratedFilePath != genFile.FilePath) {
					genFile.Name = result.GeneratedFilePath;
					projectChanged = true;
				}

				if (file.LastGenOutput != genFileName) {
					file.LastGenOutput = genFileName;
					projectChanged = true;
				}

				if (genFile.DependsOn != file.FilePath.FileName) {
					genFile.DependsOn = file.FilePath.FileName;
					projectChanged = true;
				}

				if (projectChanged)
					IdeApp.ProjectOperations.SaveAsync (file.Project);
			});
		}
		
		static void HandleRename (ProjectFileRenamedEventArgs e)
		{
			foreach (ProjectFileEventInfo args in e) {
				var file = args.ProjectFile;
				var tool = GetGenerator (file.Generator);
				if (tool == null)
					continue;
			}
		}

		public static string GetFileNamespace (ProjectFile file, string outputFile)
		{
			string ns = file.CustomToolNamespace;
			if (string.IsNullOrEmpty (ns) && !string.IsNullOrEmpty (outputFile)) {
				var dnp = file.Project as DotNetProject;
				if (dnp != null)
					ns = dnp.GetDefaultNamespace (outputFile);
			}
			return ns;
		}

		public static bool WaitForRunningTools (ProgressMonitor monitor)
		{
			TaskInfo[] operations;
			lock (runningTasks) {
				operations = runningTasks.Values.ToArray ();
			}

			if (operations.Length == 0)
				return true;

			monitor.BeginTask ("Waiting for custom tools...", operations.Length);

			var evt = new AutoResetEvent (false);

			foreach (var t in operations) {
				t.Task.ContinueWith (ta => {
					monitor.Step (1);
					if (operations.All (op => op.Task.IsCompleted))
						evt.Set ();
				});
			}

			monitor.CancellationToken.Register (delegate {
				evt.Set ();
			});

			evt.WaitOne ();

			monitor.EndTask ();

			//the tool operations display warnings themselves
			return operations.Any (op => !op.Result.SuccessWithWarnings);
		}
	}
}

