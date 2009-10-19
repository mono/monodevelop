// 
// IPhoneDebuggerSession.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using Mono.Debugger;
using Mono.Debugging;
using Mono.Debugging.Client;
using System.Threading;
using System.Diagnostics;
using MonoDevelop.IPhone;
using System.IO;

namespace MonoDevelop.Debugger.Soft.IPhone
{


	public class IPhoneDebuggerSession : SoftDebuggerSession
	{
		string appName;
		ProcessInfo[] procs;
		Process simProcess;
		
		protected override VirtualMachine LaunchVirtualMachine (DebuggerStartInfo startInfo, out bool startsSuspended)
		{
			startsSuspended = false;
			var dsi = (IPhoneDebuggerStartInfo) startInfo;
			
			appName = dsi.ExecutionCommand.AppPath.FileNameWithoutExtension;
			
			DoMdbCopyHack (dsi.ExecutionCommand);
			
			if (dsi.ExecutionCommand.Simulator) {
				StartSimulatorProcess (dsi.ExecutionCommand);
			} else {
				//FIXME: upload the app
			}
			
			Gtk.Dialog dialog = null;
			
			VirtualMachine vm = null;
			Thread listenThread = new Thread (new ThreadStart (delegate {
				try {
					vm = VirtualMachineManager.Listen (dsi.Endpoint);
					Gtk.Application.Invoke (delegate {
						if (dialog != null)
							dialog.Respond (Gtk.ResponseType.Ok);
					});
				} catch (ThreadAbortException) {
					Thread.ResetAbort ();
				}
				
			}));
			listenThread.Start ();
			
			int response = (int)Gtk.ResponseType.Cancel;
			MonoDevelop.Core.Gui.DispatchService.GuiSyncDispatch (delegate {
				//show a dialog asking the user to connect
				dialog = new Gtk.Dialog () {
					Title = "Waiting for debugger"
				};
				string message = "Waiting for debugger to connect...";
				if (!dsi.ExecutionCommand.Simulator)
					message += "\nPlease start the application on the device";
				
				var label = new Gtk.Alignment (0.5f, 0.5f, 1f, 1f) {
					Child = new Gtk.Label (message),
					BorderWidth = 12
				};
				dialog.VBox.PackStart (label);
				label.ShowAll ();	
				
				dialog.AddButton ("Cancel", Gtk.ResponseType.Cancel);
				
				response = MonoDevelop.Core.Gui.MessageService.ShowCustomDialog (dialog);
			});
			
			if (response == (int) Gtk.ResponseType.Ok) {
				return vm;
			} else {
				EndSimProcess ();
				if (listenThread != null && listenThread.IsAlive)
					listenThread.Abort ();
				return null;
			}
		}
		
		protected override ProcessInfo[] OnGetProcesses ()
		{
			if (procs == null)
				procs = new ProcessInfo[] { new ProcessInfo (0, appName) };
			return procs;
		}
		
		//HACK because mtouch doesn't copy mdbs yet
		void DoMdbCopyHack (IPhoneExecutionCommand cmd)
		{
			foreach (var f in Directory.GetFiles (cmd.AppPath.ParentDirectory, "*.mdb")) {
				var fout = cmd.AppPath.Combine (Path.GetFileName (f));
				if (File.Exists (fout))
					File.Delete (fout);
				File.Copy (f, fout);
			}
		}

		//FIXME: hook up the app's stdin and stdout
		void StartSimulatorProcess (IPhoneExecutionCommand cmd)
		{
			string mtouchPath = cmd.Runtime.GetToolPath (cmd.Framework, "mtouch");
			if (string.IsNullOrEmpty (mtouchPath))
				throw new InvalidOperationException ("Cannot execute iPhone application. mtouch tool is missing.");
			
			var psi = new ProcessStartInfo () {
				FileName = mtouchPath,
				UseShellExecute = false,
				Arguments = string.Format ("-launchsim='{0}'", cmd.AppPath),
				RedirectStandardInput = true,
			};
			simProcess = Process.Start (psi);
			
			simProcess.Exited += delegate {
				MarkAsExited ();
			};
			
			TargetExited += delegate {
				EndSimProcess ();
			};
		}
		
		void EndSimProcess ()
		{
			if (simProcess == null)
				return;
			simProcess.StandardInput.WriteLine ();
			GLib.Timeout.Add (10000, delegate {
				if (!simProcess.HasExited)
					simProcess.Kill ();
				return false;
			});
		}
		
		
		protected override void OnExit ()
		{
			base.OnExit ();
			EndSimProcess ();
		}
	}
}
