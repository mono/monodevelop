//
// GitVersion.cs
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
	public static class GitVersion
	{
		public static async Task<Version> GetVersionAsync (CancellationToken cancellationToken = default)
		{
			var handler = new GitOutputTrackerCallbackHandler ();
			var arguments = new GitArguments (".");
			arguments.AddArgument ("--version");

			var result = await new GitProcess ().StartAsync (arguments, handler, false, cancellationToken);

			return ParseVersionString (handler.Output);
		}

		static readonly Regex versionRegex = new Regex ("git version (\\d+)\\.(\\d+)(?:\\.(\\d+))?(?:\\.(\\d+))?(.*)");

		internal static Version ParseVersionString (string output)
		{
			var match = versionRegex.Match (output);
			if (!match.Success)
				throw new InvalidOperationException ("Can't parse : " + output);

			int major = ParseGroup (match, 1);
			int minor = ParseGroup (match, 2);
			int build = ParseGroup (match, 3);
			int revision = ParseGroup (match, 4);

			return new Version (major, minor, build, revision);
		}

		static int ParseGroup (Match match, int grp)
		{
			return !string.IsNullOrEmpty (match.Groups[grp].Value) ? int.Parse (match.Groups[grp].Value) : 0;
		}
	}
}
