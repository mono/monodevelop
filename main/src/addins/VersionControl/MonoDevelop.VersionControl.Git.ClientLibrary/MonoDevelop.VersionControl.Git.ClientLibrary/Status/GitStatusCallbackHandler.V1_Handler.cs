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

namespace MonoDevelop.VersionControl.Git.ClientLibrary
{
	partial class GitStatusCallbackHandler
	{
		class V1_Handler : StatusCallbackHandler
		{
			protected override bool TryParseState (string data, int start, ref int end, out GitFileState state, out string path)
			{
				var (sx, sy, origPath, path2) = ParsePath (data, start, end);
				path = path2;
				if (origPath == null) {
					state = GitFileState.CreateSimpleState (GetState (sx, sy));
				} else {
					state = GitFileState.CreateRenamedState (GetState (sx, sy), GitSubmoduleState.NoSubmodule, 0, 0, 0, origPath, 0);
				}
				return true;
			}

			internal static (char sx, char sy, string origPath, string path) ParsePath (string text, int from, int to)
			{
				// format to parse is:
				// XY PATH
				// XY ORIG_PATH -> PATH

				char sx = text [from];
				char sy = text [from + 1];
				string origPath = null;
				string path = null;
				const int headerLength = 3;
				var arrowIdx = text.IndexOf (" -> ", from + headerLength, to - from - headerLength, StringComparison.Ordinal);
				if (arrowIdx >= 0) {
					origPath = text.Substring (from + headerLength, arrowIdx - from - headerLength);
					const int arrowLength = 4;  // " -> ".Length;
					path = text.Substring (arrowIdx + arrowLength, to - arrowIdx - arrowLength);
				} else {
					path = text.Substring (from + headerLength, to - from - headerLength);
				}
				return (sx, sy, origPath, path);
			}
		}
	}
}
