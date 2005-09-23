//
// Commands.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Internal.Project;
using MonoDevelop.Services;
using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Gui.Pads;
using MonoDevelop.Gui.Pads.ProjectPad;
using MonoDevelop.Commands;

namespace MonoDeveloper
{	
	public enum Commands
	{
		Install,
		SvnDiff,
		SvnUpdate,
		SvnStat,
		SvnInfo,
		SvnAdd,
		SvnRevert,
		SvnCommit
	}
	
	public class InstallHandler: CommandHandler
	{
		protected override void Run ()
		{
			MonoProject p = Runtime.ProjectService.CurrentSelectedProject as MonoProject;
			if (p != null)
				Runtime.DispatchService.BackgroundDispatch (new StatefulMessageHandler (Install), p);
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Visible = Runtime.ProjectService.CurrentSelectedProject is MonoProject;
		}
		
		void Install (object prj)
		{
			MonoProject p = prj as MonoProject;
			using (IProgressMonitor monitor = Runtime.TaskService.GetBuildProgressMonitor ()) {
				p.Install (monitor);
			}
		}
	}

	public class MonoProjectBuilder: NodeBuilderExtension
	{
		public override bool CanBuildNode (Type dataType)
		{
			return typeof(MonoProject).IsAssignableFrom (dataType) ||
					typeof(ProjectFolder).IsAssignableFrom (dataType) ||
					typeof(ProjectFile).IsAssignableFrom (dataType);
		}
		
		public override Type CommandHandlerType {
			get { return typeof(MonoProjectCommandHandler); }
		}
	}
	
	public class MonoProjectCommandHandler: NodeCommandHandler
	{
		[CommandHandler (Commands.SvnDiff)]
		public void SvnDiff ()
		{
			string path = GetPath ();
			if (path == null) return;
			Runtime.DispatchService.BackgroundDispatch (new StatefulMessageHandler (RunDiffAsync), path);
		}
		
		public void RunDiffAsync (object pa)
		{
			string path = (string) pa;
			using (IProgressMonitor monitor = Runtime.TaskService.GetOutputProgressMonitor ("Subversion Output", "", true, true)) {
				monitor.Log.WriteLine ("Running: svn diff " + path + " ...");
				StreamWriter w = new StreamWriter ("/tmp/tmp.diff");
				ProcessWrapper p = Runtime.ProcessService.StartProcess ("svn", "diff " + path, null, w, monitor.Log, null);
				p.WaitForOutput ();
				w.Close ();
				Runtime.FileService.OpenFile ("/tmp/tmp.diff");
				monitor.Log.WriteLine ();
				monitor.Log.WriteLine ("Done.");
			}
		}
		
		[CommandHandler (Commands.SvnUpdate)]
		public void SvnUpdate ()
		{
			SvnRun ("up {0}");
		}
		
		[CommandHandler (Commands.SvnStat)]
		public void SvnStat ()
		{
			SvnRun ("stat {0}");
		}
		
		[CommandHandler (Commands.SvnInfo)]
		public void SvnInfo ()
		{
			SvnRun ("info {0}");
		}
		
		[CommandHandler (Commands.SvnAdd)]
		public void SvnAdd ()
		{
			SvnRun ("add {0}");
		}
		
		[CommandHandler (Commands.SvnRevert)]
		public void SvnRevert ()
		{
			if (Runtime.MessageService.AskQuestion ("Do you really want to revert " + GetPath() + "?"))
				SvnRun ("revert {0}");
		}
		
		[CommandHandler (Commands.SvnCommit)]
		public void SvnCommit ()
		{
			IConsole console = ExternalConsoleFactory.Instance.CreateConsole (false);
			Runtime.ProcessService.StartConsoleProcess ("svnci", GetPath(), null, console, null);
		}
		
		public string GetPath ()
		{
			string path;
			if (CurrentNode.DataItem is ProjectFolder)
				path = ((ProjectFolder)CurrentNode.DataItem).Path;
			else if (CurrentNode.DataItem is Project)
				path = ((Project)CurrentNode.DataItem).BaseDirectory;
			else if (CurrentNode.DataItem is ProjectFile)
				path = ((ProjectFile)CurrentNode.DataItem).Name;
			else
				return null;
			return path;
		}
		
		public void SvnRun (string cmd)
		{
			string path = GetPath ();
			if (path == null) return;
			Runtime.DispatchService.BackgroundDispatch (new StatefulMessageHandler (RunAsync), new SvnCommand (cmd, path));
		}
		
		public virtual void RunAsync (object pa)
		{
			SvnCommand c = (SvnCommand) pa;
			string cmd = string.Format (c.Command, c.Path);
			using (IProgressMonitor monitor = Runtime.TaskService.GetOutputProgressMonitor ("Subversion Output", "", true, true)) {
				monitor.Log.WriteLine ("Running: svn " + cmd + " ...");
				ProcessWrapper p = Runtime.ProcessService.StartProcess ("svn", cmd, null, monitor.Log, monitor.Log, null);
				p.WaitForOutput ();
				monitor.Log.WriteLine ();
				monitor.Log.WriteLine ("Done.");
			}
		}
		
	}
	
	class SvnCommand
	{
		public SvnCommand (string cmd, string path)
		{
			Command = cmd;
			Path = path;
		}
		
		public string Path;
		public string Command;
	}
}
