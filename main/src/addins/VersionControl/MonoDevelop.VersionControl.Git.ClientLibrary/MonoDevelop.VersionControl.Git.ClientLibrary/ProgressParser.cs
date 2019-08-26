//
// ProgressParser.cs
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
using System.Text.RegularExpressions;

namespace MonoDevelop.VersionControl.Git.ClientLibrary
{
	static class ProgressParser
	{
		static readonly Regex progressRegex = new Regex (".*((?:Receiving|Writing|Compressing|Counting) objects|Resolving deltas|Checking out files): +(\\d{1,3})%j*", RegexOptions.Compiled);

		public static bool ParseProgress (string line, AbstractGitCallbackHandler callbackHandler)
		{
			if (line is null)
				throw new ArgumentNullException (nameof (line));
			if (callbackHandler is null)
				throw new ArgumentNullException (nameof (callbackHandler));

			var match = progressRegex.Match (line);
			if (match.Success) {
				callbackHandler.OnReportProgress (match.Groups [1].Value, int.Parse(match.Groups [2].Value));
				return true;
			}

			return false;
		}
	}
}
