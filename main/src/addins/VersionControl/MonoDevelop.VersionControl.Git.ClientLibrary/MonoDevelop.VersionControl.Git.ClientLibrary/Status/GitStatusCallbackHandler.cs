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
using System.Collections.Generic;

namespace MonoDevelop.VersionControl.Git.ClientLibrary
{
	public enum StatusVersion
	{
		v1,
		v2
	}

	abstract class StatusCallbackHandler : GitCallbackHandler
	{
		protected readonly static GitFileState IgnoredState = GitFileState.CreateSimpleState (GitStatusCode.Ignored);
		protected readonly static GitFileState UntrackedState = GitFileState.CreateSimpleState (GitStatusCode.Untracked);


		Dictionary<string, GitFileState> fileList = new Dictionary<string, GitFileState> ();

		public Dictionary<string, GitFileState> FileList {
			get {
				return fileList;
			}
		}

		public sealed override void OnOutput (string line)
		{
			int i = 0;
			while (i < line.Length) {
				int next = line.IndexOf ('\u0000', i);
				if (next < 0 || next >= line.Length)
					break;
				if (TryParseState (line, i, ref next, out var state, out var path)) {
					fileList [path] = state;
				}
				i = next + 1;
			}
		}

		protected abstract bool TryParseState (string data, int start, ref int end, out GitFileState state, out string path);


		protected static GitStatusCode GetState (char x, char y)
		{
			var result = GitStatusCode.Unmodified;

			switch (x) {
			case ' ':
			case '.': break;
			case 'M':
				result |= GitStatusCode.IndexModified;
				break;
			case 'A':
				result |= GitStatusCode.IndexAdded;
				break;
			case 'D':
				result |= GitStatusCode.IndexDeleted;
				break;
			case 'R':
				result |= GitStatusCode.IndexRenamed;
				break;
			case 'C':
				result |= GitStatusCode.IndexCopied;
				break;
			case 'U':
				result |= GitStatusCode.IndexUnmerged;
				break;
			case 'T':
				result |= GitStatusCode.IndexTypeChanged;
				break;
			case 'X':
				result |= GitStatusCode.IndexUnknown;
				break;
			case '?':
				switch (y) {
				case '?': return GitStatusCode.Untracked;
				default:
					throw new InvalidOperationException ("Unknown y state: " + y + " x state: " + x);
				}
			case '!':
				switch (y) {
				case '!': return GitStatusCode.Ignored;
				default:
					throw new InvalidOperationException ("Unknown y state: " + y + " x state: " + x);
				}
			default:
				throw new InvalidOperationException ("Unknown x state: " + x + " (y state: " + y + ")");
			}

			switch (y) {
			case ' ':
			case '.': break;
			case 'M':
				result |= GitStatusCode.WorktreeModified;
				break;
			case 'A':
				result |= GitStatusCode.WorktreeAdded;
				break;
			case 'D':
				result |= GitStatusCode.WorktreeDeleted;
				break;
			case 'R':
				result |= GitStatusCode.WorktreeRenamed;
				break;
			case 'C':
				result |= GitStatusCode.WorktreeCopied;
				break;
			case 'U':
				result |= GitStatusCode.WorktreeUnmerged;
				break;
			case 'T':
				result |= GitStatusCode.WorktreeTypeChanged;
				break;
			case 'X':
				result |= GitStatusCode.WorktreeUnknown;
				break;
			case '?': // should've been handled above.
			case '!':
				throw new InvalidOperationException ("Invalid y state: " + y + " (x state: " + x +")");
			default:
				throw new InvalidOperationException ("Unknown y state: " + y + " (x state: " + x + ")");
			}

			return result;
		}
	}

	partial class GitStatusCallbackHandler
	{
		public static StatusCallbackHandler CreateCallbackHandler (StatusVersion version)
		{
			switch (version) {
			case StatusVersion.v1:
				return new V1_Handler ();
			case StatusVersion.v2:
				return new V2_Handler ();
			default:
				throw new InvalidOperationException ("Git status :" + version + " unknown.");
			}
		}
	}
}
