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

namespace MonoDevelop.VersionControl.Git.ClientLibrary
{

	public readonly struct GitFileState
	{
		// data
		readonly uint  stateFlags;     // 20 bit data contains: RenameOrCopyScore (8 bit) GitSubmoduleState (4 bit) GitStageState (16 bit)
		readonly ulong fileMode;       // contains 64 bit data : 4 FileModes = [WorkTree] [Stage3] [Stage2/Index] [Stage1/Head]
		readonly string originalPath;

		// all entries:
		public GitStatusCode StageState { get => (GitStatusCode)(stateFlags & 0xFFFF); }
		public GitSubmoduleState SubmoduleState { get => (GitSubmoduleState)((stateFlags >> 16) & 0xF); }

		// for ordinary entries:
		public ushort FileMode_Head { get => (ushort)fileMode; }
		public ushort FileMode_Index { get => (ushort)(fileMode >> 16); }
		public ushort FileMode_Worktree { get => (ushort)(fileMode >> 48); }

		// For renamed/copied Entries:
		public string OriginalPath { get => originalPath; }
		public GitRenameOrCopyScore RenameOrCopyScore { get => new GitRenameOrCopyScore((byte)(stateFlags >> 20)); }

		// For unmerged entries:
		public ushort FileMode_Stage1 { get => FileMode_Head; }
		public ushort FileMode_Stage2 { get => FileMode_Index; }
		public ushort FileMode_Stage3 { get => (ushort)(fileMode >> 32); }

		GitFileState (GitStatusCode stageState, GitSubmoduleState submoduleState, ushort fileMode_Stage1, ushort fileMode_Stage2, ushort fileMode_Stage3, ushort fileMode_Worktree, string originalPath, byte renameOrCopyScore)
		{
			stateFlags = (uint)stageState | ((uint)((byte)submoduleState & 0xF) << 16) | ((uint)renameOrCopyScore << 20);
			fileMode = fileMode_Stage1 | (ulong)fileMode_Stage2 << 16 | (ulong)fileMode_Stage3 << 32 | (ulong)fileMode_Worktree << 48;
			this.originalPath = originalPath;
		}

		internal static GitFileState CreateSimpleState (GitStatusCode stageState)
		{
			return new GitFileState (stageState, GitSubmoduleState.NoSubmodule, 0, 0, 0, 0, null, 0);
		}

		internal static GitFileState CreateOrdinaryState (GitStatusCode stageState, GitSubmoduleState submoduleState, ushort fileMode_Head, ushort fileMode_Index, ushort fileMode_Worktree)
		{
			return new GitFileState (stageState, submoduleState, fileMode_Head, fileMode_Index, 0, fileMode_Worktree, null, 0);
		}

		internal static GitFileState CreateRenamedState (GitStatusCode stageState, GitSubmoduleState submoduleState, ushort fileMode_Head, ushort fileMode_Index, ushort fileMode_Worktree, string originalPath, byte renameOrCopyScore)
		{
			return new GitFileState (stageState, submoduleState, fileMode_Head, fileMode_Index, 0, fileMode_Worktree, originalPath, renameOrCopyScore);
		}

		internal static GitFileState CreateUnmergedState (GitStatusCode stageState, GitSubmoduleState submoduleState, ushort fileMode_Stage1, ushort fileMode_Stage2, ushort fileMode_Stage3, ushort fileMode_Worktree)
		{
			return new GitFileState (stageState, submoduleState, fileMode_Stage1, fileMode_Stage2, fileMode_Stage3, fileMode_Worktree, null, 0);
		}
	}
}
