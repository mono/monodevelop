/*
This code is derived from jgit (http://eclipse.org/jgit).
Copyright owners are documented in jgit's IP log.

This program and the accompanying materials are made available
under the terms of the Eclipse Distribution License v1.0 which
accompanies this distribution, is reproduced below, and is
available at http://www.eclipse.org/org/documents/edl-v10.php

All rights reserved.

Redistribution and use in source and binary forms, with or
without modification, are permitted provided that the following
conditions are met:

- Redistributions of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.

- Redistributions in binary form must reproduce the above
  copyright notice, this list of conditions and the following
  disclaimer in the documentation and/or other materials provided
  with the distribution.

- Neither the name of the Eclipse Foundation, Inc. nor the
  names of its contributors may be used to endorse or promote
  products derived from this software without specific prior
  written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using NGit;
using Sharpen;

namespace NGit.Notes
{
	/// <summary>A tree entry found in a note branch that isn't a valid note.</summary>
	/// <remarks>A tree entry found in a note branch that isn't a valid note.</remarks>
	[System.Serializable]
	internal class NonNoteEntry : ObjectId
	{
		/// <summary>Name of the entry in the tree, in raw format.</summary>
		/// <remarks>Name of the entry in the tree, in raw format.</remarks>
		private readonly byte[] name;

		/// <summary>Mode of the entry as parsed from the tree.</summary>
		/// <remarks>Mode of the entry as parsed from the tree.</remarks>
		private readonly FileMode mode;

		/// <summary>The next non-note entry in the same tree, as defined by tree order.</summary>
		/// <remarks>The next non-note entry in the same tree, as defined by tree order.</remarks>
		internal NGit.Notes.NonNoteEntry next;

		internal NonNoteEntry(byte[] name, FileMode mode, AnyObjectId id) : base(id)
		{
			this.name = name;
			this.mode = mode;
		}

		internal virtual void Format(TreeFormatter fmt)
		{
			fmt.Append(name, mode, this);
		}

		internal virtual int TreeEntrySize()
		{
			return TreeFormatter.EntrySize(mode, name.Length);
		}

		internal virtual int PathCompare(byte[] bBuf, int bPos, int bLen, FileMode bMode)
		{
			return PathCompare(name, 0, name.Length, mode, bBuf, bPos, bLen, bMode);
		}

		//
		private static int PathCompare(byte[] aBuf, int aPos, int aEnd, FileMode aMode, byte
			[] bBuf, int bPos, int bEnd, FileMode bMode)
		{
			while (aPos < aEnd && bPos < bEnd)
			{
				int cmp = (aBuf[aPos++] & unchecked((int)(0xff))) - (bBuf[bPos++] & unchecked((int
					)(0xff)));
				if (cmp != 0)
				{
					return cmp;
				}
			}
			if (aPos < aEnd)
			{
				return (aBuf[aPos] & unchecked((int)(0xff))) - LastPathChar(bMode);
			}
			if (bPos < bEnd)
			{
				return LastPathChar(aMode) - (bBuf[bPos] & unchecked((int)(0xff)));
			}
			return 0;
		}

		private static int LastPathChar(FileMode mode)
		{
			return FileMode.TREE.Equals(mode.GetBits()) ? '/' : '\0';
		}
	}
}
