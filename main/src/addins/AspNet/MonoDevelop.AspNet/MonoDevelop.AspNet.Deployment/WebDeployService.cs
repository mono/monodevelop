// WebDeployService.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
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

using System;
using System.Threading;
using System.Collections.Generic;
using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Ide.ProgressMonitoring;
using MonoDevelop.Deployment;
using MonoDevelop.Deployment.Gui;
using MonoDevelop.AspNet;
using MonoDevelop.Projects;
using MonoDevelop.Ide;

namespace MonoDevelop.AspNet.Deployment
{
	
	public class WebDeployService
	{
		private WebDeployService ()
		{
		}
		
		static public void Deploy (AspNetAppProject project, WebDeployTarget target, ConfigurationSelector configuration)
		{
			Deploy (project, new WebDeployTarget[] { target }, configuration);
		}
		
		static public void Deploy (AspNetAppProject project, ICollection<WebDeployTarget> targets, ConfigurationSelector configuration)
		{
			//project needs to be built before it can be deployed
			IdeApp.ProjectOperations.Build (project);
			
			//set up and launch a copying thread
			DeployThreadParams threadParams = new DeployThreadParams ();
			threadParams.Context = new DeployContext (new WebDeployResolver (), null, null);
			threadParams.Files = MonoDevelop.Deployment.DeployService.GetDeployFiles (threadParams.Context,
			                                                                          project, configuration);
			
			Dictionary<string, string> taskAliases = new Dictionary<string,string> ();
			foreach (WebDeployTarget target in targets) {
				threadParams.Targets.Add ((WebDeployTarget) target.Clone ());
				taskAliases.Add (target.LocationName, target.GetMarkup ());
			}
			
			var monitor = new MultiTaskDialogProgressMonitor (true, true, true, taskAliases);
			monitor.SetDialogTitle (MonoDevelop.Core.GettextCatalog.GetString ("Web Deployment Progress"));
			monitor.SetOperationTitle (MonoDevelop.Core.GettextCatalog.GetString ("Deploying {0}...", project.Name));
			threadParams.Monitor = monitor;
			
			Thread deployThread = new Thread (new ParameterizedThreadStart (DoDeploy));
			deployThread.Name = "Web deploy";
			deployThread.Start (threadParams);
		}
		
		
		static void DoDeploy (object o)
		{
			var threadParams = (DeployThreadParams) o;
			try {
				IFileReplacePolicy replacePolicy = new DialogFileReplacePolicy ();
				
				foreach (WebDeployTarget target in threadParams.Targets) {
					if (threadParams.Monitor.IsCancelRequested)
						break;
					try {
						
						target.FileCopier.CopyFiles (threadParams.Monitor, replacePolicy,
						                             threadParams.Files, threadParams.Context);
					}
					catch (OperationCanceledException ex) {
						threadParams.Monitor.ReportError (GettextCatalog.GetString ("Web deploy aborted."), ex);
						break;
					}
					catch (InvalidOperationException ex) {
						threadParams.Monitor.ReportError (GettextCatalog.GetString ("Web deploy aborted."), ex);
						break;
					}
				}
			} catch (Exception e) {
				MonoDevelop.Core.LoggingService.LogError ("Unhandled exception in the web deploy thread", e);
				MonoDevelop.Ide.MessageService.ShowException (e, "Web deploy failed due to unhandled exception");
			} finally {
				try {
					threadParams.Monitor.Dispose ();
				} catch (Exception ex2) {
					MonoDevelop.Core.LoggingService.LogError ("Unhandled exception disposing the web deploy thread", ex2);
				}
			}
		}
		
		static public void DeployDialog (AspNetAppProject project)
		{
			var dialog = new WebDeployLaunchDialog (project) {
				Modal = true,
			};
			
			ICollection<WebDeployTarget> targets = null;
			
			var response = ResponseType.None;
			do {
				response = (ResponseType) MessageService.RunCustomDialog (dialog, MessageService.RootWindow);
			} while (response != ResponseType.Ok && response != ResponseType.Cancel && response != ResponseType.DeleteEvent);
			
			if (response == Gtk.ResponseType.Ok)
				targets = dialog.GetSelectedTargets ();
			
			dialog.Destroy ();
			
			if (targets != null && targets.Count > 0)
				Deploy (project, targets, IdeApp.Workspace.ActiveConfiguration);
		}
		
		class DeployThreadParams
		{
			public List<WebDeployTarget> Targets = new List<WebDeployTarget> ();
			public DeployFileCollection Files;
			public IProgressMonitor Monitor;
			public DeployContext Context;
		}
	}
}
