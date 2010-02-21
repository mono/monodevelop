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
using Tamir.SharpSsh.jsch;
using System.IO;
using MonoDevelop.Core;
using System.Text;
using System.Threading;

namespace MonoDevelop.MeeGo
{

	class MeeGoExecutionHandler : IExecutionHandler
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
			
			//if (MeeGoUtility.NeedsUploading (cmd.Config)) {
				MeeGoUtility.Upload (targetDevice, cmd.Config, console.Out, console.Error).WaitForCompleted ();
			//}
			
			var auth = GetGdmXAuth (targetDevice);
			if (auth == null) {
				console.Error.WriteLine ("Could not obtain single X authority for user '" + targetDevice.Username +"'");
				return new NullProcessAsyncOperation (false);
			}
			
			string exec = GetCommandString (cmd, auth);
			
			var ssh = new LiveSshExec (targetDevice.Address, targetDevice.Username, targetDevice.Password);			
			var killExec = new SshExec (targetDevice.Address, targetDevice.Username, targetDevice.Password);
			
			//hacky but openssh seems to ignore signals
			Action kill = delegate {
				killExec.Connect ();
				killExec.RunCommand ("ps x | grep monodevelop_remote | " +
					"grep -v 'grep monodevelop_remote' | awk '{ print $1 }' | xargs kill ");
				killExec.Close ();
			};
			
			Console.WriteLine (exec);
			
			var proc = new SshRemoteProcess (ssh, exec, console, kill);
			proc.Run ();
			return proc;
		}
		
		public static string GetCommandString (MeeGoExecutionCommand cmd, Dictionary<string,string> auth)
		{
			string runtimeArgs = string.IsNullOrEmpty (cmd.RuntimeArguments) ? "--debug" : cmd.RuntimeArguments;
			string executable = cmd.Config.ParentItem.Name + "/" + Path.GetFileName (cmd.Config.CompiledOutputName);
			
			var sb = new StringBuilder ();
			foreach (var arg in cmd.EnvironmentVariables)
				sb.AppendFormat ("{0}='{1}' ", arg.Key, arg.Value);
			foreach (var arg in auth)
				sb.AppendFormat ("{0}='{1}' ", arg.Key, arg.Value);
			sb.Append ("exec -a monodevelop_remote ");
			sb.AppendFormat ("mono {0} '{1}' {2}", runtimeArgs, executable, cmd.Arguments);
			
			return sb.ToString ();
		}
		
		public static Dictionary<string,string> GetGdmXAuth (MeeGoDevice targetDevice)
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
	
	class SshRemoteProcess : SshOperation<LiveSshExec>, IProcessAsyncOperation
	{
		string command;
		IConsole console;
		ChannelExec channel;
		Action kill;
		
		public SshRemoteProcess (LiveSshExec ssh, string command, IConsole console, Action kill) : base (ssh)
		{
			this.command = command;
			this.console = console;
			this.kill = kill;
		}
		
		protected override void RunOperations ()
		{
			try {
				channel = Ssh.GetChannel (command);
				var stdOut = new TextWriterStream (console.Out, Encoding.ASCII);
				var stdErr = new TextWriterStream (console.Error, Encoding.ASCII);
				channel.setErrStream (stdErr);
				channel.setOutputStream (stdOut);
				channel.connect ();
				while (!channel.isEOF ())
					Thread.Sleep (200);
				channel.disconnect ();
			} finally {
				ExitCode = channel.getExitStatus ();
			}
		}
		
		public int ExitCode { get; private set; }
		public int ProcessId { get; private set; }
		
		public override void Cancel ()
		{
			kill ();
			
			channel.sendSignal ("TERM");
			channel.disconnect ();
		}
	}
	
	class LiveSshExec : SshExec
	{
		public LiveSshExec (string address, string username, string password) : base (address, username, password)
		{
		}
		
		public ChannelExec GetChannel (string command)
		{
			return (ChannelExec) (m_channel = GetChannelExec (command));
		}
		
		public ChannelExec Channel {
			get { return (ChannelExec)m_channel; }
		}
	}
	
	class TextWriterStream : Stream
	{
		TextWriter writer;
		Encoding encoding;
		
		public TextWriterStream (TextWriter writer, Encoding incomingEncoding)
		{
			this.writer = writer;
			this.encoding = incomingEncoding;
		}
		
		public override void Write (byte[] buffer, int offset, int count)
		{
			writer.Write (encoding.GetString (buffer, offset, count));
		}
		
		public override bool CanRead { get { return false; } }
		public override bool CanWrite { get { return false; } }
		public override bool CanSeek { get { return false; } }
		public override long Length { get { return -1; } }
		
		public override long Position {
			get { return -1; }
			set { throw new InvalidOperationException (); }
		}
		
		public override long Seek (long offset, SeekOrigin origin)
		{
			throw new InvalidOperationException ();
		}
		
		public override void SetLength (long value)
		{
			throw new InvalidOperationException ();
		}
		
		public override int Read (byte[] buffer, int offset, int count)
		{
			throw new InvalidOperationException ();
		}
		
		public override void Flush ()
		{
		}
	}
}
