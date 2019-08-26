//
// GitClone.cs
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

namespace MonoDevelop.VersionControl.Git.ClientLibrary
{
	public static class GitClone
	{
		public static Task<GitResult> CloneAsync (string sourceUrl, string outputDir, AbstractGitCallbackHandler callbacks, CancellationToken cancellationToken = default) => CloneAsync (sourceUrl, outputDir, callbacks, GitCloneOptions.Default, cancellationToken);

		public static async Task<GitResult> CloneAsync (string sourceUrl, string outputDir, AbstractGitCallbackHandler callbacks, GitCloneOptions options, CancellationToken cancellationToken = default)
		{
			if (sourceUrl is null)
				throw new ArgumentNullException (nameof (sourceUrl));
			if (outputDir is null)
				throw new ArgumentNullException (nameof (outputDir));
			if (options is null)
				throw new ArgumentNullException (nameof (options));
			var arg = new GitArguments (outputDir);
			arg.AddArgument ("clone");
			arg.AddArgument ("-c credential.helper=");
			arg.AddArgument ("-c core.quotepath=false");
			arg.AddArgument ("-c log.showSignature=false ");
			arg.AddArgument ("--progress");
			if (options.RecurseSubmodules)
				arg.AddArgument ("--recurse-submodules");
			if (options.IsBare)
				arg.AddArgument ("--bare");
			if (!string.IsNullOrEmpty (options.BranchName)) {
				arg.AddArgument ("-- branch");
				arg.AddArgument (options.BranchName);
			}
			arg.EndOptions ();
			arg.AddArgument (sourceUrl);
			arg.AddArgument (outputDir);
			var handler = new CloneCallbackHandler (callbacks);
			var result = await new GitProcess ().StartAsync (arg, handler, true, cancellationToken);
			result.Properties[GitCloneOptions.CloneTargetProperty] = handler.CloneTarget;
			return result;
		}

	}
}