//
// GitFetch.cs
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
using System.Threading.Tasks;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace MonoDevelop.VersionControl.Git.ClientLibrary
{
	public class GitFetchOptions
	{
		/// <summary>
		/// The "remote" repository that is the source of a fetch or pull operation. This parameter can be either a URL or the name of a remote.
		/// </summary>
		/// <value>The repository.</value>
		public string Repository { get; set; }

		/// <summary>
		/// Specifies which refs to fetch and which local refs to update.
		/// </summary>
		public IEnumerable<string> RefSpec { get; set; }

		/// <summary>
		/// Fetch all remotes.
		/// </summary>
		public bool FetchAll { get; set; }

		/// <summary>
		///  When git fetch is used with &lt;src&gt;:&lt;dst&gt; refspec it may refuse to update the local branch as discussed in the &lt;refspec&gt; part below.This option overrides that check.
		/// </summary>
		public bool Force { get; set; }
	}

	public static class GitFetch
	{
		public static async Task<GitResult> FetchAsync (string rootPath, GitFetchOptions options, AbstractGitCallbackHandler callbackHandler, CancellationToken cancellationToken = default)
		{
			var arguments = new GitArguments (rootPath);
			arguments.AddArgument ("fetch");
			if (options.FetchAll)
				arguments.AddArgument ("--all");
			if (options.Force)
				arguments.AddArgument ("-f");
			arguments.EndOptions ();
			if (!string.IsNullOrEmpty (options.Repository))
				arguments.AddArgument (options.Repository);
			if (options.RefSpec != null) {
				foreach (var refSpec in options.RefSpec)
					arguments.AddArgument (refSpec);
			}
			return await new GitProcess ().StartAsync (arguments, callbackHandler, false, cancellationToken);
		}
	}
}