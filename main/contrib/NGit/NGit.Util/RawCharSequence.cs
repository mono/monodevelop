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

using System.Text;
using Sharpen;

namespace NGit.Util
{
	/// <summary>A rough character sequence around a raw byte buffer.</summary>
	/// <remarks>
	/// A rough character sequence around a raw byte buffer.
	/// <p>
	/// Characters are assumed to be 8-bit US-ASCII.
	/// </remarks>
	public sealed class RawCharSequence : CharSequence
	{
		/// <summary>A zero-length character sequence.</summary>
		/// <remarks>A zero-length character sequence.</remarks>
		public static readonly NGit.Util.RawCharSequence EMPTY = new NGit.Util.RawCharSequence
			(null, 0, 0);

		internal readonly byte[] buffer;

		internal readonly int startPtr;

		internal readonly int endPtr;

		/// <summary>Create a rough character sequence around the raw byte buffer.</summary>
		/// <remarks>Create a rough character sequence around the raw byte buffer.</remarks>
		/// <param name="buf">buffer to scan.</param>
		/// <param name="start">starting position for the sequence.</param>
		/// <param name="end">ending position for the sequence.</param>
		public RawCharSequence(byte[] buf, int start, int end)
		{
			buffer = buf;
			startPtr = start;
			endPtr = end;
		}

		public char CharAt(int index)
		{
			return (char)(buffer[startPtr + index] & unchecked((int)(0xff)));
		}

		public int Length
		{
			get
			{
				return endPtr - startPtr;
			}
		}

		public CharSequence SubSequence(int start, int end)
		{
			return new NGit.Util.RawCharSequence(buffer, startPtr + start, startPtr + end);
		}

		public override string ToString()
		{
			int n = Length;
			StringBuilder b = new StringBuilder(n);
			for (int i = 0; i < n; i++)
			{
				b.Append(CharAt (i));
			}
			return b.ToString();
		}
	}
}
