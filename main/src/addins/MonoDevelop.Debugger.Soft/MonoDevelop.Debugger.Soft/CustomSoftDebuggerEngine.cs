// 
// CustomSoftDebuggerEngine.cs
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
using System.Diagnostics;
using Mono.Debugging.Client;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using System.Net;
using System.Collections.Generic;
using Mono.Debugging.Soft;

namespace MonoDevelop.Debugger.Soft
{
	class CustomSoftDebuggerEngine: DebuggerEngineBackend
	{
		bool? available;
		
		public override bool CanDebugCommand (ExecutionCommand cmd)
		{
			// This isn't polished enough to show it by default. GUI needs work, and it should be exposed
			// via "run with->custom parameters", not a toplevel command and dialog.
			// That would also make it possible to save settings.
			if (!available.HasValue) {
				available = !string.IsNullOrEmpty (Environment.GetEnvironmentVariable ("MONODEVELOP_SDB_TEST"));
			}
			return available.Value;
		}

		public override DebuggerSession CreateSession ()
		{
			return new CustomSoftDebuggerSession ();
		}
		
		public override DebuggerStartInfo CreateDebuggerStartInfo (ExecutionCommand c)
		{
			//WORKAROUND: explicit generic type argument works around a gmcs 2.6.x type inference bug 
			var dsi = InvokeSynch<SoftDebuggerStartInfo> (GetDebuggerInfo);
			//HACK: flag object so we can cancel the session
			if (dsi == null)
				return new DebuggerStartInfo ();

			var cmd = (DotNetExecutionCommand) c;
			SoftDebuggerEngine.SetUserAssemblyNames (dsi, cmd.UserAssemblyPaths);
			return dsi;
		}
		
		static SoftDebuggerStartInfo GetDebuggerInfo ()
		{
			var dlg = new DebuggerOptionsDialog ();
			try {
				return dlg.Run ();
			} finally {
				dlg.Destroy ();
			}
		}
		
		static T InvokeSynch<T> (Func<T> func)
		{
			if (Runtime.IsMainThread)
				return func ();
			
			var ev = new System.Threading.ManualResetEvent (false);
			T val = default (T);
			Exception caught = null;
			Gtk.Application.Invoke ((o, args) => {
				try {
					val = func ();
				} catch (Exception ex) {
					caught = ex;
				} finally {
					ev.Set ();
				}
			});
			ev.WaitOne ();
			if (caught != null)
				throw caught;
			return val;
		}
		
		class CustomSoftDebuggerSession : SoftDebuggerSession
		{
			ProcessAsyncOperation process;
			bool usingExternalConsole;
			
			protected override void OnRun (DebuggerStartInfo startInfo)
			{
				var info = startInfo as SoftDebuggerStartInfo;
				if (info == null) {
					EndSession ();
					return;
				}
				
				StartProcess (info);
				
				if (info.StartArgs is SoftDebuggerConnectArgs) {
					//connecting to the process, so give it a moment to start listening
					System.Threading.Thread.Sleep (200);
				}
				
				base.OnRun (startInfo);
			}
			
			void StartProcess (SoftDebuggerStartInfo info)
			{
				if (string.IsNullOrEmpty (info.Command))
					return;
			
				if (info.UseExternalConsole) {
					usingExternalConsole = true;
					var console = ExternalConsoleFactory.Instance.CreateConsole (info.CloseExternalConsoleOnExit);
					process = Runtime.ProcessService.StartConsoleProcess (
						info.Command, info.Arguments, info.WorkingDirectory, console, info.EnvironmentVariables);
				} else {
					var psi = new ProcessStartInfo (info.Command, info.Arguments) {
						WorkingDirectory = info.WorkingDirectory,
						UseShellExecute = false
					};
					foreach (KeyValuePair<string,string> kvp in info.EnvironmentVariables)
						psi.EnvironmentVariables [kvp.Key] = kvp.Value;
					
					process = Runtime.ProcessService.StartProcess (psi, ProcessOutput, ProcessError, null).ProcessAsyncOperation;
				}
			}
			
			void ProcessOutput (object sender, string message)
			{
				OnTargetOutput (false, message);
			}
			
			void ProcessError (object sender, string message)
			{
				OnTargetOutput (true, message);
			}
			
			protected override void EndSession ()
			{
				base.EndSession ();
				EndProcess ();
			}
			
			void EndProcess ()
			{
				if (process == null)
					return;
				
				var p = process;
				process = null;
				
				if (usingExternalConsole || p.IsCompleted)
					return;
				
				try {
					p.Cancel ();
				} catch {}
			}
			
			protected override void EnsureExited ()
			{
				EndProcess ();
			}
		}
		
		class DebuggerOptionsDialog : Gtk.Dialog
		{
			MonoDevelop.Components.FileEntry commandEntry = new MonoDevelop.Components.FileEntry ();
			Gtk.Entry argsEntry = new Gtk.Entry ();
			Gtk.Entry ipEntry = new Gtk.Entry ();
			Gtk.Entry portEntry = new Gtk.Entry ();
			Gtk.Entry consolePortEntry = new Gtk.Entry ();
			Gtk.Button listenButton = new Gtk.Button  ("Listen");
			Gtk.Button connectButton = new Gtk.Button ("Connect");
			
			const Gtk.ResponseType listenResponse = Gtk.ResponseType.Yes;
			const Gtk.ResponseType connectResponse = Gtk.ResponseType.Ok;
			
			IPAddress ip;
			string command, args;
			int? port, consolePort;
			
			Properties properties;
			
			//TODO: dropdown menus for picking string substitutions. also substitutions for port, ip
			public DebuggerOptionsDialog () : base (
				"Launch Soft Debugger", MonoDevelop.Ide.MessageService.RootWindow,
				Gtk.DialogFlags.DestroyWithParent | Gtk.DialogFlags.Modal)
			{
				properties = PropertyService.Get ("MonoDevelop.Debugger.Soft.CustomSoftDebugger", new Properties());
				
				AddActionWidget (new Gtk.Button (Gtk.Stock.Cancel), Gtk.ResponseType.Cancel);
				AddActionWidget (listenButton, listenResponse);
				AddActionWidget (connectButton, connectResponse);
				
				var table = new Gtk.Table (5, 2, false);
				table.BorderWidth = 6;
				VBox.PackStart (table, true, true, 0);
				
				table.Attach (new Gtk.Label ("Command:") { Xalign = 0 },   0, 1, 0, 1);
				table.Attach (new Gtk.Label ("Arguments:") { Xalign = 0 }, 0, 1, 1, 2);
				table.Attach (new Gtk.Label ("IP:") { Xalign = 0 },        0, 1, 2, 3);
				table.Attach (new Gtk.Label ("Port:") { Xalign = 0 },      0, 1, 3, 4);
				table.Attach (new Gtk.Label ("Output:") { Xalign = 0 },    0, 1, 4, 5);
				
				table.Attach (commandEntry,     1, 2, 0, 1);
				table.Attach (argsEntry,        1, 2, 1, 2);
				table.Attach (ipEntry,          1, 2, 2, 3);
				table.Attach (portEntry,        1, 2, 3, 4);
				table.Attach (consolePortEntry, 1, 2, 4, 5);
				
				argsEntry.WidthRequest = 500;
				
				commandEntry.PathChanged += delegate {
					try {
						//check it parses
						MonoDevelop.Core.StringParserService.Parse (commandEntry.Path);
						command = commandEntry.Path;
					} catch {
						command = null;
					}
					CheckValid ();
				};
				
				argsEntry.Changed += delegate {
					try {
						//check it parses
						MonoDevelop.Core.StringParserService.Parse (argsEntry.Text);
						args = argsEntry.Text;
					} catch {
						args = null;
					}
					CheckValid ();
				};
				
				ipEntry.Changed += delegate {
					if (string.IsNullOrEmpty (ipEntry.Text)) {
						ip = IPAddress.Loopback;
					} else if (!IPAddress.TryParse (ipEntry.Text, out ip)) {
						ip = null;
					}
					CheckValid ();
				};
				
				portEntry.Changed += delegate {
					port = ParsePort (portEntry.Text);
					CheckValid ();
				};
				
				consolePortEntry.Changed += delegate {
					consolePort = ParsePort (consolePortEntry.Text);
					CheckValid ();
				};
				
				command = properties.Get ("Command", "");
				args = properties.Get ("Arguments", "");
				if (!IPAddress.TryParse (properties.Get ("IpAddress", "127.0.0.1"), out ip) || ip == null)
					ip = IPAddress.Loopback;
				string portStr = properties.Get<string> ("Port");
				port = ParsePort (portStr) ?? 10000;
				string consolePortStr = properties.Get<string> ("ConsolePort");
				consolePort = ParsePort (consolePortStr);
				
				commandEntry.Path = command;
				argsEntry.Text = args;
				ipEntry.Text = ip.ToString ();
				portEntry.Text = PortToString (port) ?? "";
				consolePortEntry.Text = PortToString (consolePort) ?? "";
				
				CheckValid ();
				
				VBox.ShowAll ();
			}
			
			int? ParsePort (string port)
			{
				if (string.IsNullOrEmpty (port))
					return null;
				int value;
				if (!int.TryParse (port, out value))
					return -1;
				return value;
			}
			
			string PortToString (int? port)
			{
				return port.HasValue? port.Value.ToString () : null; 
			}
			
			public new SoftDebuggerStartInfo Run ()
			{
				var response = (Gtk.ResponseType) MonoDevelop.Ide.MessageService.RunCustomDialog (this);
				if (response != listenResponse && response != connectResponse)
					return null;
				
				properties.Set ("Command", command);
				properties.Set ("Arguments", args);
				properties.Set ("IpAddress", ip.ToString ());
				properties.Set ("Port", PortToString (port));
				properties.Set ("ConsolePort", PortToString (consolePort));
				
				var name = string.IsNullOrEmpty (command)? "" : command;
				bool listen = response == listenResponse;
				var agentArgs = string.Format ("transport=dt_socket,address={0}:{1}{2}", ip, port, listen?"":",server=y");
				
				var customArgsTags = new string[,] {
					{ "AgentArgs", agentArgs },
					{ "IP", ip.ToString () },
					{ "Port", PortToString (port) },
					{ "Console", PortToString (consolePort) },
				};
				
				SoftDebuggerRemoteArgs startArgs;
				if (listen) {
					startArgs = (SoftDebuggerRemoteArgs) new SoftDebuggerListenArgs (name, ip, port.Value, consolePort ?? -1);
				} else {
					startArgs = new SoftDebuggerConnectArgs (name, ip, port.Value, consolePort ?? -1) {
						//infinite connection retries (user can cancel), 800ms between them
						TimeBetweenConnectionAttempts = 800,
						MaxConnectionAttempts = -1,
					};
				};
				
				var dsi = new SoftDebuggerStartInfo (startArgs) {
					Command = StringParserService.Parse (command),
					Arguments = StringParserService.Parse (args, customArgsTags),
				};
				
				//FIXME: GUI for env vars
				//dsi.EnvironmentVariables [kvp.Key] = kvp.Value;
				
				return dsi;
			}
			
			void CheckValid ()
			{
				bool valid = ip != null
					&& (port.HasValue && port.Value >= 0)
					&& (!consolePort.HasValue || consolePort >= 0);
				listenButton.Sensitive = valid;
				connectButton.Sensitive = valid && port > 0 && (!consolePort.HasValue || consolePort > 0);
			}
		}
	}
}