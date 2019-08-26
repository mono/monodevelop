//
// GitStatusCode.cs
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
	[Flags]
	public enum GitStatusCode : ushort
	{//                                  X         Y
		Unmodified          = 0b_0000_0000_0000_0000,

		INDEX_MASK          = 0b_1111_1111_0000_0000,
		IndexModified       = 0b_0000_0001_0000_0000, // M
		IndexAdded          = 0b_0000_0010_0000_0000, // A
		IndexDeleted        = 0b_0000_0011_0000_0000, // D
		IndexRenamed        = 0b_0000_0100_0000_0000, // R
		IndexCopied         = 0b_0000_0101_0000_0000, // C
		IndexUnmerged       = 0b_0000_0110_0000_0000, // U
		IndexTypeChanged    = 0b_0000_0111_0000_0000, // T
		IndexUnknown        = 0b_0000_1111_0000_0000, // X (should never happen)

		WORKTREE_MASK       = 0b_0000_0000_1111_1111,
		WorktreeModified    = 0b_0000_0000_0000_0001,
		WorktreeAdded       = 0b_0000_0000_0000_0010,
		WorktreeDeleted     = 0b_0000_0000_0000_0011,
		WorktreeRenamed     = 0b_0000_0000_0000_0100,
		WorktreeCopied      = 0b_0000_0000_0000_0101,
		WorktreeUnmerged    = 0b_0000_0000_0000_0110,
		WorktreeTypeChanged = 0b_0000_0000_0000_0111,
		WorktreeUnknown     = 0b_0000_0000_0000_1111,

		Untracked           = 0b_1111_1110_1111_1110,
		Ignored             = 0b_1111_1111_1111_1111
	}
}
