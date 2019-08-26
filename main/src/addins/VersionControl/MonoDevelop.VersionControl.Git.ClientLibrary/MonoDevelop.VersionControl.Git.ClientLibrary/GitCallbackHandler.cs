//
// GitCallbackHandler.cs
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

namespace MonoDevelop.VersionControl.Git.ClientLibrary
{
	public class GitCallbackHandler : AbstractGitCallbackHandler
	{
		public Func<string, GitCredentials> GetCredentialsHandler;

		public override GitCredentials OnGetCredentials (string url) => GetCredentialsHandler (url);

		public event EventHandler<string> Output;

		public override void OnOutput (string line) => Output?.Invoke (this, line);

		public event EventHandler<GitProgressEventArgs> Progress;

		public override void OnReportProgress (string operation, int percentage) => Progress?.Invoke (this, new GitProgressEventArgs (operation, percentage));

		public Func<string, string> GetSSHPassword;
		public override string OnGetSSHPassword (string userName) => GetSSHPassword (userName);

		public Func<string, string> GetSSHPassphrase;
		public override string OnGetSSHPassphrase (string key) => GetSSHPassphrase (key);

		public Func<bool> GetContinueConnecting;

		public override bool OnGetContinueConnecting () => GetContinueConnecting ();
	}
}
