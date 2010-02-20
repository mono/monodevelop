// 
// MeeGoExecutionHandler.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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
using System.Linq;
using MonoDevelop.Core.Execution;
using System.Collections.Generic;
using Tamir.SharpSsh;
using System.IO;
using MonoDevelop.Core;
using System.Text;

namespace MonoDevelop.MeeGo
{

	public class MeeGoExecutionHandler : IExecutionHandler
	{

		public bool CanExecute (ExecutionCommand command)
		{
			return command is MeeGoExecutionCommand;
		}
		
		public IProcessAsyncOperation Execute (ExecutionCommand command, IConsole console)
		{
			var cmd = (MeeGoExecutionCommand) command;
			var targetDevice = MeeGoDevice.GetChosenDevice ();
			if (targetDevice == null) {
				return new NullProcessAsyncOperation (false);
			}
			
			if (MeeGoUtility.NeedsUploading (cmd.Config)) {
				MeeGoUtility.Upload (targetDevice, cmd.Config, console.Out, console.Error).WaitForCompleted ();
			}
			
			var auth = GetGdmXAuth (targetDevice);
			if (auth == null) {
				console.Error.WriteLine ("Could not obtain single X authority for user '" + targetDevice.Username +"'");
				return new NullProcessAsyncOperation (false);
			}
			
			string exec = GetCommandString (cmd, auth);
			
			var ssh = new SshExec (targetDevice.Address, targetDevice.Username, targetDevice.Password);
			
			var proc = new SshRemoteProcess (ssh, exec.ToString ());
			proc.Run ();
			return proc;
		}
		
		string GetCommandString (MeeGoExecutionCommand cmd, Dictionary<string,string> auth)
		{
			string runtimeArgs = string.IsNullOrEmpty (cmd.RuntimeArguments) ? "--debug" : cmd.RuntimeArguments;
			string executable = cmd.Config.ParentItem.Name + "/" + Path.GetFileName (cmd.Config.CompiledOutputName);
			
			var sb = new StringBuilder ();
			foreach (var arg in cmd.EnvironmentVariables)
				sb.AppendFormat ("{0}='{1}' ", arg.Key, arg.Value);
			foreach (var arg in auth)
				sb.AppendFormat ("{0}='{1}' ", arg.Key, arg.Value);
			sb.AppendFormat ("mono {0} '{1}' {2}", runtimeArgs, executable, cmd.Arguments);
			
			return sb.ToString ();
		}
		
		Dictionary<string,string> GetGdmXAuth (MeeGoDevice targetDevice)
		{
			Sftp sftp = null;
			try {
				sftp = new Sftp (targetDevice.Address, targetDevice.Username, targetDevice.Password);
				sftp.Connect ();
				var files = sftp.GetFileList ("/var/run/gdm/auth-for-" + targetDevice.Username + "*");
				sftp.Close ();
				if (files.Count == 1) {
					return new Dictionary<string, string> () {
						{ "XAUTHLOCALHOSTNAME", "localhost" },
						{ "DISPLAY", ":0.0"}, 
						{ "XAUTHORITY", "/var/run/gdm/" + files[0] +"/database"}
					};
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Error getting xauth via sftp", ex);
				if (sftp != null) {
					try {
						sftp.Close ();
					} catch (Exception ex2) {
						LoggingService.LogError ("Error closing sftp connection", ex2);
					}
				}
			}
			return null;
		}
	}
	
	class SshRemoteProcess : SshOperation<SshExec>, IProcessAsyncOperation
	{
		string command;
		
		public SshRemoteProcess (SshExec ssh, string command) : base (ssh)
		{
			this.command = command;
		}
		
		protected override void RunOperations ()
		{
			Console.WriteLine (Ssh.RunCommand (command));
		}
		
		public int ExitCode { get; private set; }
		public int ProcessId { get; private set; }
		
		public override void Cancel ()
		{
			Ssh.Close ();
		}
	}
}
