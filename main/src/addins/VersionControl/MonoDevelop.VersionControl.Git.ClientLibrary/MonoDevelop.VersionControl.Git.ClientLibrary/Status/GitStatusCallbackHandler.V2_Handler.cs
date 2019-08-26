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
using System.IO;

namespace MonoDevelop.VersionControl.Git.ClientLibrary
{
	partial class GitStatusCallbackHandler
	{
		class V2_Handler : StatusCallbackHandler
		{
			protected override bool TryParseState (string data, int start, ref int end, out GitFileState state, out string path)
			{
				/*
Ordinary changed entries have the following format:
    1 <XY> <sub> <mH> <mI> <mW> <hH> <hI> <path>
Renamed or copied entries have the following format:
    2 <XY> <sub> <mH> <mI> <mW> <hH> <hI> <X><score> <path><sep><origPath>
Unmerged entries have the following format; the first character is a "u" to distinguish from ordinary changed entries.
    u <xy> <sub> <m1> <m2> <m3> <mW> <h1> <h2> <h3> <path>
Untracked items have the following format:
    ? <path>
Ignored items have the following format:
    ! <path>

				 * */
				switch (data [start]) {
				case '1': return TryParseOrdinaryEntry (data, start + 2, end, out state, out path);
				case '2': return TryParseRenamedEntry (data, start + 2, ref end, out state, out path);
				case 'u': return TryParseUnmergedEntry (data, start + 2, end, out state, out path);
				case '?':
					state = UntrackedState;
					path = data.Substring (start + 2, end - 2 - start);
					return true;

				case '!':
					state = IgnoredState;
					path = data.Substring (start + 2, end - 2 - start);
					return true;
				case '#': // Header: Ignore for now
					state = UntrackedState;
					path = null;
					return false;
				}

				state = UntrackedState;
				path = null;

				return false;
			}

			bool TryParseOrdinaryEntry (string data, int start, int end, out GitFileState state, out string path)
			{
				//<XY> <sub> <mH> <mI> <mW> <hH> <hI> <path>
				var k = start;
				char x = data [k++]; // <XY>
				char y = data [k++];
				k++; // skip space
				char subIndicator = data [k++]; // <sub> A 4 character field describing the submodule state.
				char subHasCommitChanged = data [k++];
				char subHasTrackedChanges = data [k++];
				char subHasUntrackedChanges = data [k++];
				k++; // skip space
				var fileModeHead = ParseOctalNumber (data, ref k);     // <mH>
				var fileModeIndex = ParseOctalNumber (data, ref k);    // <mI>
				var fileModeWorkTree = ParseOctalNumber (data, ref k); // <mW>

				while (data [k] != ' ') k++; // <hH> skip octal file mode in WorkTree
				k++; // skip space

				while (data [k] != ' ') k++; // <hI> skip octal file mode in WorkTree
				k++; // skip space

				path = data.Substring (k, end - k);
				state = GitFileState.CreateOrdinaryState (GetState (x, y), GetSubmoduleState (subIndicator, subHasCommitChanged, subHasTrackedChanges, subHasUntrackedChanges), fileModeHead, fileModeIndex, fileModeWorkTree);
				return true;
			}

			bool TryParseRenamedEntry (string data, int start, ref int end, out GitFileState state, out string path)
			{ // 2 <XY> <sub> <mH> <mI> <mW> <hH> <hI> <X><score> <path><sep><origPath>
				var k = start;
				char x = data [k++]; // <XY>
				char y = data [k++];
				k++; // skip space
				char subIndicator = data [k++]; // <sub> A 4 character field describing the submodule state.
				char subHasCommitChanged = data [k++];
				char subHasTrackedChanges = data [k++];
				char subHasUntrackedChanges = data [k++];
				k++; // skip space
				var fileModeHead = ParseOctalNumber (data, ref k);     // <mH>
				var fileModeIndex = ParseOctalNumber (data, ref k);    // <mI>
				var fileModeWorkTree = ParseOctalNumber (data, ref k); // <mW>

				while (k < end && data [k] != ' ') k++; // <hH> skip octal file mode in WorkTree
				k++; // skip space

				while (k < end && data [k] != ' ') k++; // <hI> skip octal file mode in WorkTree
				k++; // skip space

				byte renameOrCopyScore = (byte)(data [k++] == 'R' ? 0x80 : 0); // <X>
				int percentage = 0;
				while (k < end) { // <score>
					char ch = data [k];
					if (ch == ' ')
						break;
					percentage = percentage * 10 + (ch - '0');
					k++;
				}
				k++; // skip space

				path = data.Substring (k, end - k);
				end++;
				int next = data.IndexOf ('\u0000', end);

				state = GitFileState.CreateRenamedState (
					GetState (x, y),
					GetSubmoduleState (subIndicator, subHasCommitChanged, subHasTrackedChanges, subHasUntrackedChanges),
					fileModeHead, fileModeIndex, fileModeWorkTree,
					data.Substring (end, next - end),
					(byte)(renameOrCopyScore | (percentage & 0x7F))
				);
				end = next;
				return true;
			}


			bool TryParseUnmergedEntry (string data, int start, int end, out GitFileState state, out string path)
			{
				//<xy> <sub> <m1> <m2> <m3> <mW> <h1> <h2> <h3> <path>
				var k = start;
				char x = data [k++]; // <XY>
				char y = data [k++];
				k++; // skip space
				char subIndicator = data [k++]; // <sub> A 4 character field describing the submodule state.
				char subHasCommitChanged = data [k++];
				char subHasTrackedChanges = data [k++];
				char subHasUntrackedChanges = data [k++];
				k++; // skip space
				var fileModeStage1 = ParseOctalNumber (data, ref k);   // <m1>
				var fileModeStage2 = ParseOctalNumber (data, ref k);   // <m2>
				var fileModeStage3 = ParseOctalNumber (data, ref k);   // <m3>
				var fileModeWorkTree = ParseOctalNumber (data, ref k); // <mW>

				while (data [k] != ' ') k++; // <h1> skip object name in stage 1.
				k++; // skip space

				while (data [k] != ' ') k++; // <h2> skip object name in stage 2.
				k++; // skip space

				while (data [k] != ' ') k++; // <h3> skip object name in stage 3.
				k++; // skip space

				path = data.Substring (k, end - k);
				state = GitFileState.CreateUnmergedState (GetState (x, y), GetSubmoduleState (subIndicator, subHasCommitChanged, subHasTrackedChanges, subHasUntrackedChanges), fileModeStage1, fileModeStage2, fileModeStage3, fileModeWorkTree);
				return true;
			}

			static ushort ParseOctalNumber (string data, ref int k)
			{
				int result = 0;
				while (k < data.Length) {
					char ch = data [k];
					if (ch == ' ') {
						k++; // skip space
						return (ushort)result;
					}
					result = result * 8 + (ch - '0');
					k++;
				}
				return (ushort)result;
			}

			static GitSubmoduleState GetSubmoduleState (char subIndicator, char subHasCommitChanged, char subHasTrackedChanges, char subHasUntrackedChanges)
			{
				if (subIndicator == 'N')
					return GitSubmoduleState.NoSubmodule;
				if (subIndicator != 'S')
					throw new InvalidDataException ("Sub module indicator " + subIndicator + " unknown.");
				var result = GitSubmoduleState.IsSubmodule;
				if (subHasCommitChanged == 'C')
					result |= GitSubmoduleState.CommitChanged;
				if (subHasTrackedChanges == 'M')
					result |= GitSubmoduleState.HasTrackedChanged;
				if (subHasUntrackedChanges == 'U')
					result |= GitSubmoduleState.HasUntrackedChanged;
				return result;
			}
		}
	}
}
