//
// GitProcess.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corporation. All rights reserved.
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
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Sockets;
using System.IO.Pipes;
using System.Linq;

namespace MonoDevelop.VersionControl.Git.ClientLibrary
{
	sealed class GitArguments
	{
		public string GitRootPath { get; }

		StringBuilder arguments = new StringBuilder ();

		public string Arguments { get => arguments.ToString (); }

		public bool TrackProgress { get; set; } = true;

		public GitArguments (string rootPath)
		{
			if (rootPath is null)
				throw new ArgumentNullException (nameof (rootPath));
			if (!Directory.Exists (rootPath))
				throw new DirectoryNotFoundException (rootPath);
			this.GitRootPath = rootPath;
		}

		public void AddArgument (string argument)
		{
			if (arguments.Length > 0)
				arguments.Append (' ');
			arguments.Append (argument);
		}

		public void EndOptions ()
		{
			AddArgument ("--");
		}
	}

	sealed class GitProcess
	{
		public readonly static string [] errorIndicatorTags = {
			"warning:",
			"error:",
			"fatal:",
			"remote: error",
			"Cannot",
			"Could not",
			"Interactive rebase already started",
			"refusing to pull",
			"cannot rebase:",
			"conflict",
			"unable",
			"The file will have its original",
			"runnerw:"
		};

		static TaskFactory exclusiveOperationFactory;
		static TaskFactory concurrentOperationFactory;

		static GitProcess ()
		{
			var scheduler = new ConcurrentExclusiveSchedulerPair ();
			exclusiveOperationFactory = new TaskFactory (scheduler.ExclusiveScheduler);
			concurrentOperationFactory = new TaskFactory (scheduler.ConcurrentScheduler);
		}

		Process process;
		GitCredentials credentials = null;
		AbstractGitCallbackHandler callbacks;
		NamedPipeServerStream server;

		StringBuilder errorText = new StringBuilder ();
		string pipe;

		public GitProcess ()
		{
			pipe = "MD_GIT_Pipe_" + Process.GetCurrentProcess ().Id;
		}

		public bool IsRunning { get => process != null && !process.HasExited; }

		public Task<GitResult> StartAsync (GitArguments arguments, AbstractGitCallbackHandler callbackHandler, bool askPass, CancellationToken cancellationToken = default)
		{
			if (arguments is null)
				throw new ArgumentNullException (nameof (arguments));
			if (callbackHandler is null)
				throw new ArgumentNullException (nameof (callbackHandler));


			return (askPass ? exclusiveOperationFactory : concurrentOperationFactory).StartNew (delegate {
				try {
					this.callbacks = callbackHandler;
					var startInfo = new ProcessStartInfo ("git");
					startInfo.WorkingDirectory = arguments.GitRootPath;
					startInfo.RedirectStandardError = true;
					startInfo.RedirectStandardOutput = true;
					startInfo.UseShellExecute = false;
					startInfo.Arguments = arguments.Arguments;

					if (askPass) {
						var passApp = Path.Combine (Path.GetDirectoryName (typeof (GitProcess).Assembly.Location), "GitAskPass");
						startInfo.EnvironmentVariables.Add ("GIT_ASKPASS", passApp);
						startInfo.EnvironmentVariables.Add ("SSH_ASKPASS", passApp);
						startInfo.EnvironmentVariables.Add ("MONODEVELOP_GIT_ASKPASS_PIPE", pipe);
						server = new NamedPipeServerStream (pipe);
					}

					process = Process.Start (startInfo);
					process.OutputDataReceived += (sender, e) => {
						if (cancellationToken.IsCancellationRequested) {
							SafeKillProcess ();
							cancellationToken.ThrowIfCancellationRequested ();
						}
						if (e?.Data == null)
							return;
						if (arguments.TrackProgress && ProgressParser.ParseProgress (e.Data, callbackHandler))
							return;
						callbackHandler.OnOutput (e.Data);
					};
					process.ErrorDataReceived += (sender, e) => {
						if (cancellationToken.IsCancellationRequested) {
							SafeKillProcess ();
							cancellationToken.ThrowIfCancellationRequested ();
						}
						if (e?.Data == null)
							return;
						if (IsError (e.Data)) {
							errorText.AppendLine (e.Data);
						} else {
							if (arguments.TrackProgress && ProgressParser.ParseProgress (e.Data, callbackHandler))
								return;
							callbackHandler.OnOutput (e.Data);
						}
					};
					if (!process.Start ())
						throw new InvalidOperationException ("Can't start git process.");
					process.BeginOutputReadLine ();
					process.BeginErrorReadLine ();
					cancellationToken.Register (SafeKillProcess);
					if (askPass)
						server.BeginWaitForConnection (HandleAsyncCallback, server);
					process.WaitForExit ();
				} finally {
					SafeKillProcess ();
				}
				return new GitResult { Success = process.ExitCode == 0, ExitCode = process.ExitCode, ErrorMessage = errorText.ToString () };
			}, cancellationToken);
		}

		void SafeKillProcess ()
		{
			try {
				if (!process.HasExited)
					process.Kill ();
				if (server != null) {
					server.Dispose ();
					server = null;
				}
			} catch { }
		}

		void HandleAsyncCallback (IAsyncResult result)
		{
			var ns = server;

			var reader = new GitAskPass.StreamStringReadWriter (ns);
			var request = reader.ReadLine ();
			switch (request) {
			case "Username":
				var url = reader.ReadLine ();
				if (GetCredentials (url)) {
					reader.WriteLine (credentials.Username);
				}
				break;
			case "Password":
				url = reader.ReadLine ();
				if (GetCredentials (url)) {
					reader.WriteLine (credentials.Password);
				}
				break;
			case "Continue connecting":
				reader.WriteLine (callbacks.OnGetContinueConnecting () ? "yes" : "no");
				break;
			case "SSHPassPhrase":
				string key = reader.ReadLine ();
				reader.WriteLine (callbacks.OnGetSSHPassphrase (key));
				break;
			case "SSHPassword":
				string userName = reader.ReadLine ();
				reader.WriteLine (callbacks.OnGetSSHPassword (userName));
				break;
			case "Error":
				throw new GitExternalException (reader.ReadLine ());
			}
			server.Close ();

			server = new NamedPipeServerStream (pipe);
			server.BeginWaitForConnection (HandleAsyncCallback, server);
		}

		bool GetCredentials (string url)
		{
			if (credentials == null) {
				try {
					credentials = callbacks.OnGetCredentials (url);
					if (credentials == null)
						SafeKillProcess ();
				} catch {
					credentials = null;
					SafeKillProcess ();
				}
			}
			return credentials != null;
		}

		public static bool IsError (string line)
		{
			int start = 0;
			while (start + 1 < line.Length && char.IsWhiteSpace (line [start]))
				start++;
			if (start >= line.Length)
				return false;
			foreach (var tag in errorIndicatorTags) {
				if (start + tag.Length >= line.Length)
					continue;
				if (line.IndexOf (tag, start, tag.Length, StringComparison.InvariantCultureIgnoreCase) == start)
					return true;
			}
			return false;
		}
	}
}
