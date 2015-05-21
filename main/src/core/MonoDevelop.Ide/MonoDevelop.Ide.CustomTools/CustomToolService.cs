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
using System.Collections.Generic;
using System.IO;
using System.CodeDom.Compiler;
using Task = System.Threading.Tasks.Task;
using IdeTask = MonoDevelop.Ide.Tasks.TaskListEntry;
using System.Linq;
using System.Threading;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Projects;

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

		public async static Task Update (IEnumerable<ProjectFile> files, bool force)
		{
			var monitor = IdeApp.Workbench.ProgressMonitors.GetToolOutputProgressMonitor (false);

			IEnumerator<ProjectFile> fileEnumerator;

			//begin root task
			monitor.BeginTask (GettextCatalog.GetString ("Generate templates"), 1);

			if (files == null || !(fileEnumerator = files.GetEnumerator ()).MoveNext ()) {
				monitor.EndTask ();
				monitor.ReportSuccess (GettextCatalog.GetString ("No templates found"));
				monitor.Dispose ();
			} else {
				await Update (monitor, fileEnumerator, force, 0, 0, 0);
			}
		}

		static bool ShouldRunGenerator (ProjectFile file, bool force, out ISingleFileCustomTool tool, out ProjectFile genFile)
		{
			tool = null;
			genFile = null;

			//ignore cloned file from shared project in context of referencing project
			if ((file.Flags & ProjectItemFlags.DontPersist) != 0) {
				return false;
			}

			tool = GetGenerator (file.Generator);
			if (tool == null) {
				return false;
			}

			//ignore MSBuild tool for projects that aren't MSBuild or can't build
			//we could emit a warning but this would get very annoying for Xamarin Forms + SAP
			//in future we could consider running the MSBuild generator in context of every referencing project
			if (tool is MSBuildCustomTool) {
				if (!file.Project.SupportsBuild ()) {
					return false;
				}
				bool byDefault, require;
				MonoDevelop.Projects.Formats.MSBuild.MSBuildProjectService.CheckHandlerUsesMSBuildEngine (file.Project, out byDefault, out require);
				var usesMSBuild = require || (file.Project.UseMSBuildEngine ?? byDefault);
				if (!usesMSBuild) {
					return false;
				}
			}

			if (!string.IsNullOrEmpty (file.LastGenOutput)) {
				genFile = file.Project.Files.GetFile (file.FilePath.ParentDirectory.Combine (file.LastGenOutput));
			}

			return force
				|| genFile == null
				|| !File.Exists (genFile.FilePath)
				|| File.GetLastWriteTime (file.FilePath) > File.GetLastWriteTime (genFile.FilePath);
		}

		static async Task Update (ProgressMonitor monitor, IEnumerator<ProjectFile> fileEnumerator, bool force, int succeeded, int warnings, int errors)
		{
			ProjectFile file = fileEnumerator.Current;
			ISingleFileCustomTool tool;
			ProjectFile genFile;

			bool shouldRun;
			while (!(shouldRun = ShouldRunGenerator (file, force, out tool, out genFile)) && fileEnumerator.MoveNext ())
				continue;

			//no files which can be generated in remaining elements of the collection, nothing to do
			if (!shouldRun) {
				WriteSummaryResults (monitor, succeeded, warnings, errors);
				return;
			}

			TaskService.Errors.ClearByOwner (file);

			var result = new SingleFileCustomToolResult ();
			monitor.BeginTask (GettextCatalog.GetString ("Running generator '{0}' on file '{1}'...", file.Generator, file.Name), 1);

			try {
				await tool.Generate (monitor, file, result);
				if (!monitor.HasErrors && !monitor.HasWarnings) {
					monitor.Log.WriteLine (GettextCatalog.GetString ("File '{0}' was generated successfully.", result.GeneratedFilePath));
					succeeded++;
				} else if (!monitor.HasErrors) {
					monitor.Log.WriteLine (GettextCatalog.GetString ("File '{0}' was generated with warnings.", result.GeneratedFilePath));
					warnings++;
				} else {
					monitor.Log.WriteLine (GettextCatalog.GetString ("Errors in file '{0}' generation.", result.GeneratedFilePath));
					errors++;
				}

				//check that we can process further. If UpdateCompleted returns `true` this means no errors or non-fatal errors occured
				if (UpdateCompleted (monitor, file, genFile, result, true) && fileEnumerator.MoveNext ())
					await Update (monitor, fileEnumerator, force, succeeded, warnings, errors);
				else
					WriteSummaryResults (monitor, succeeded, warnings, errors);
			} catch (Exception ex) {
				result.UnhandledException = ex;
				UpdateCompleted (monitor, file, genFile, result, true);
			}
		}

		static void WriteSummaryResults (ProgressMonitor monitor, int succeeded, int warnings, int errors)
		{
			monitor.Log.WriteLine ();

			int total = succeeded + warnings + errors;

			//this might not be correct for languages where pluralization affects the other arguments
			//but gettext doesn't really have an answer for sentences with multiple plurals
			monitor.Log.WriteLine (
				GettextCatalog.GetPluralString (
					"{0} file processed total. {1} generated successfully, {2} with warnings, {3} with errors",
					"{0} files processed total. {1} generated successfully, {2} with warnings, {3} with errors",
					total,
					total, succeeded, warnings, errors)
			);
			//ends the root task
			monitor.EndTask ();

			if (errors > 0)
				monitor.ReportError (GettextCatalog.GetString ("Errors in file generation."), null);
			else if (warnings > 0)
				monitor.ReportSuccess (GettextCatalog.GetString ("Warnings in file generation."));
			else
				monitor.ReportSuccess (GettextCatalog.GetString ("Generated files successfully."));

			monitor.Dispose ();
		}

		public static async void Update (ProjectFile file, bool force)
		{
			ISingleFileCustomTool tool;
			ProjectFile genFile;
			if (!ShouldRunGenerator (file, force, out tool, out genFile)) {
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
			try {
				monitor.BeginTask (GettextCatalog.GetString ("Running generator '{0}' on file '{1}'...", file.Generator, file.Name), 1);
				var op = tool.Generate (monitor, file, result);
				lock (runningTasks) {
					runningTasks.Add (file.FilePath, new TaskInfo { Task = op, CancellationTokenSource = cs, Result = result });
				}
				await op;
				lock (runningTasks) {
					TaskInfo runningTask;
					if (runningTasks.TryGetValue (file.FilePath, out runningTask) && runningTask.Task == op) {
						runningTasks.Remove (file.FilePath);
						UpdateCompleted (monitor, file, genFile, result, false);
					} else {
						//it was cancelled because another was run for the same file, so just clean up
						monitor.EndTask ();
						monitor.ReportWarning (GettextCatalog.GetString ("Cancelled because generator ran again for the same file"));
						monitor.Dispose ();
					}
				}
			} catch (Exception ex) {
				result.UnhandledException = ex;
				UpdateCompleted (monitor, file, genFile, result, false);
			}
		}
		
		static bool UpdateCompleted (ProgressMonitor monitor,
		                             ProjectFile file, ProjectFile genFile, SingleFileCustomToolResult result,
		                             bool runMultipleFiles)
		{
			monitor.EndTask ();

			if (monitor.CancellationToken.IsCancellationRequested) {
				monitor.ReportError (GettextCatalog.GetString ("Cancelled"), null);
				monitor.Dispose ();
				return false;
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
					bool validName = genFileName.IndexOfAny (new [] { '/', '\\' }) < 0
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
							TaskService.Errors.Add (new TaskListEntry (file.FilePath, err.ErrorText, err.Column, err.Line,
								err.IsWarning? TaskSeverity.Warning : TaskSeverity.Error,
								TaskPriority.Normal, file.Project.ParentSolution, file));
					});
				}
				
				if (broken)
					return true;

				if (!runMultipleFiles) {
					if (result.Success)
						monitor.ReportSuccess ("Generated file successfully.");
					else if (result.SuccessWithWarnings)
						monitor.ReportSuccess ("Warnings in file generation.");
					else
						monitor.ReportError ("Errors in file generation.", null);
				}
			} finally {
				if (!runMultipleFiles)
					monitor.Dispose ();
			}

			if (result.GeneratedFilePath.IsNullOrEmpty || !File.Exists (result.GeneratedFilePath))
				return true;

			// broadcast a change event so text editors etc reload the file
			FileService.NotifyFileChanged (result.GeneratedFilePath);

			// add file to project, update file properties, etc
			Gtk.Application.Invoke (async delegate {
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
					await IdeApp.ProjectOperations.SaveAsync (file.Project);
			});

			return true;
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

		public static string GetFileNamespace (ProjectFile file, string outputFile, bool useVisualStudioNamingPolicy = false)
		{
			string ns = file.CustomToolNamespace;
			if (!string.IsNullOrEmpty (ns) || string.IsNullOrEmpty (outputFile))
				return ns;
			var dnfc = file.Project as IDotNetFileContainer;
			if (dnfc != null)
				return dnfc.GetDefaultNamespace (outputFile, useVisualStudioNamingPolicy);
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

