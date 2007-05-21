//
// ProjectService.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.ProgressMonitoring;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.Ide.Projects
{
	public static class ProjectService
	{
		static Solution solution;
		static IProject activeProject;
		
		public static Solution Solution {
			get {
				return solution;
			}
		}
		
		public static IProject ActiveProject {
			get {
				return activeProject;
			}
			set {
				if (activeProject != value) {
					activeProject = value;
				}
			}
		}
		
		public static IAsyncOperation OpenSolution (string fileName)
		{
			solution = Solution.Load (fileName);
			Console.WriteLine ("loaded : " + solution);
			ActiveProject = null;
			OnSolutionOpened (new SolutionEventArgs (solution));
			return NullAsyncOperation.Success;
		}
		
		public static void CloseSolution ()
		{
			if (Solution != null) {
				ActiveProject = null;
				OnSolutionClosing (new SolutionEventArgs (solution));
				solution = null;
				OnSolutionClosed (EventArgs.Empty);
			}
		}
		
		public static bool IsSolution (string fileName)
		{
			return Path.GetExtension (fileName) == ".sln";
		}
		
		static IAsyncOperation currentBuildOperation = NullAsyncOperation.Success;
		public static IAsyncOperation CurrentBuildOperation {
			get { return currentBuildOperation; }
			set { currentBuildOperation = value; }
		}
		
		public static IAsyncOperation BuildSolution ()
		{
			if (currentBuildOperation != null && !currentBuildOperation.IsCompleted) {
				return currentBuildOperation;
			}
			IProgressMonitor monitor = new MessageDialogProgressMonitor ();
			ExecutionContext context = new ExecutionContext (new DefaultExecutionHandlerFactory (), IdeApp.Workbench.ProgressMonitors);
			Services.DispatchService.ThreadDispatch (new StatefulMessageHandler (BuildSolutionAsync), new object[] {Solution, monitor, context});
			currentBuildOperation = monitor.AsyncOperation;
			
			return currentRunOperation;
		}
		
		static void BuildSolutionAsync (object data)
		{

			object[] array = (object[])data;
			Solution solution = array[0] as Solution;
			if (solution == null) 
				return;
			IProgressMonitor monitor = array[1] as IProgressMonitor;
			ExecutionContext context = array[2] as ExecutionContext;
			
			foreach (SolutionItem item in solution.Items) {
				SolutionProject project = item as SolutionProject;
				if (project == null)
					continue;
				project.Project.Build (null);
			}
		}
		
		public static IAsyncOperation RebuildSolution ()
		{
			if (currentBuildOperation != null && !currentBuildOperation.IsCompleted) {
				return currentBuildOperation;
			}
			return null;
		}
		
		public static IAsyncOperation CleanSolution ()
		{
			return null;
		}
		
		public static IAsyncOperation BuildProject (IProject project)
		{
			if (currentBuildOperation != null && !currentBuildOperation.IsCompleted) {
				return currentBuildOperation;
			}
			IProgressMonitor monitor = new MessageDialogProgressMonitor ();
			ExecutionContext context = new ExecutionContext (new DefaultExecutionHandlerFactory (), IdeApp.Workbench.ProgressMonitors);

			Services.DispatchService.ThreadDispatch (new StatefulMessageHandler (BuildSolutionAsync), new object[] {project, monitor, context});
			currentBuildOperation = monitor.AsyncOperation;
			return currentRunOperation;
			
			project.Build (null);
			return null;
		}
		static void BuildProjectAsync (object data)
		{
			object[] array = (object[])data;
			IProject project = array[0] as IProject;
			if (project == null) 
				return;
			IProgressMonitor monitor = array[1] as IProgressMonitor;
			ExecutionContext context = array[2] as ExecutionContext;
			
			project.Build (null);
		}
		
		public static IAsyncOperation RebuildProject (IProject project)
		{
			if (currentBuildOperation != null && !currentBuildOperation.IsCompleted) {
				return currentBuildOperation;
			}
			
			return null;
		}
		public static IAsyncOperation CleanProject (IProject project)
		{
			return null;
		}
		
		static IAsyncOperation currentRunOperation = NullAsyncOperation.Success;
		public static IAsyncOperation CurrentRunOperation {
			get { return currentRunOperation; }
			set { currentRunOperation = value; }
		}
		
		public static IAsyncOperation StartSolution ()
		{
			foreach (SolutionItem item in Solution.Items) {
				SolutionProject project = item as SolutionProject;
				if (project == null)
					continue;
				return StartProject (project.Project);
			}
			return null;
		}
		
		public static IAsyncOperation StartProject(IProject project)
		{
			if (currentRunOperation != null && !currentRunOperation.IsCompleted) {
				return currentRunOperation;
			}

			IProgressMonitor monitor = new MessageDialogProgressMonitor ();
			ExecutionContext context = new ExecutionContext (new DefaultExecutionHandlerFactory (), IdeApp.Workbench.ProgressMonitors);

			Services.DispatchService.ThreadDispatch (new StatefulMessageHandler (StartProjectAsync), new object[] {project, monitor, context});
			currentRunOperation = monitor.AsyncOperation;
			return currentRunOperation;
		}
		
		static void StartProjectAsync (object data)
		{
			object[] array = (object[])data;
			IProject project = array[0] as IProject;
			if (project == null) 
				return;
			IProgressMonitor monitor = array[1] as IProgressMonitor;
			ExecutionContext context = array[2] as ExecutionContext;
			//OnBeforeStartProject ();
			try {
				project.Start (monitor, context);
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Execution failed."), ex);
			} finally {
				monitor.Dispose ();
			}
		}
		
		public static event EventHandler<ProjectEventArgs>  ActiveProjectChanged;
		public static void OnActiveProjectChanged (ProjectEventArgs e)
		{
			if (ActiveProjectChanged != null)
				ActiveProjectChanged (null, e);
		}
		
		
		public static event EventHandler<SolutionEventArgs> SolutionOpened;
		public static void OnSolutionOpened (SolutionEventArgs e)
		{
			if (SolutionOpened != null)
				SolutionOpened (null, e);
		}
		
		public static event EventHandler<SolutionEventArgs> SolutionClosing;
		public static void OnSolutionClosing (SolutionEventArgs e)
		{
			if (SolutionClosing != null)
				SolutionClosing (null, e);
		}
		
		public static event EventHandler SolutionClosed;
		public static void OnSolutionClosed (EventArgs e)
		{
			if (SolutionClosed != null)
				SolutionClosed (null, e);
		}
	}
}
