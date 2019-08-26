//
// GitStatus.cs
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
using System.Collections.Generic;

namespace MonoDevelop.VersionControl.Git.ClientLibrary
{

	public static class GitStatus
	{
		public static async Task<Dictionary<string, GitFileState>> GetStatusAsync (string repository, IEnumerable<string> paths = null, StatusVersion version = StatusVersion.v2, CancellationToken cancellationToken = default)
		{
			if (repository is null) 
				throw new ArgumentNullException (nameof (repository));

			var handler = GitStatusCallbackHandler.CreateCallbackHandler (version);

			var args = new GitArguments (repository);
			args.AddArgument ("status");
			args.AddArgument ("--ignored");
			if (version == StatusVersion.v2) {
				args.AddArgument ("--porcelain=v2");
			} else {
				args.AddArgument ("--porcelain");
			}
			args.AddArgument ("-z");
			args.AddArgument ("--untracked-files");
			args.EndOptions ();
			args.TrackProgress = false; // not required
			if (paths != null) {
				foreach (var path in paths) 
					args.AddArgument (path);
			}
			var result = await new GitProcess ().StartAsync (args, handler, false, cancellationToken);

			return handler.FileList;
		}
	}
}
