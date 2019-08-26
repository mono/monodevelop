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
using System.Text;

namespace MonoDevelop.VersionControl.Git.ClientLibrary
{
	class GitCallbackHandlerDecorator : AbstractGitCallbackHandler
	{
		readonly AbstractGitCallbackHandler callbackHandler;

		public GitCallbackHandlerDecorator (AbstractGitCallbackHandler callbackHandler)
		{
			this.callbackHandler = callbackHandler;
		}

		public override bool OnGetContinueConnecting () => callbackHandler?.OnGetContinueConnecting () == true;

		public override GitCredentials OnGetCredentials (string url) => callbackHandler?.OnGetCredentials (url);

		public override string OnGetSSHPassphrase (string key) => callbackHandler?.OnGetSSHPassword (key);

		public override string OnGetSSHPassword (string userName) => callbackHandler?.OnGetSSHPassword (userName);

		public override void OnOutput (string line) => callbackHandler?.OnOutput (line);

		public override void OnReportProgress (string operation, int percentage) => callbackHandler?.OnReportProgress (operation, percentage);

	}

	class GitOutputTrackerCallbackHandler : GitCallbackHandlerDecorator
	{
		StringBuilder output = new StringBuilder ();

  		public string Output { get => output.ToString (); }

		public GitOutputTrackerCallbackHandler (AbstractGitCallbackHandler callbackHandler = null) : base (callbackHandler)
		{
		}

		public override void OnOutput (string line)
		{
			output.AppendLine (line);
			base.OnOutput (line);
		}
	}
}
