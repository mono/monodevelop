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

using ICSharpCode.SharpZipLib.Zip.Compression;
using NGit.Storage.File;
using NGit.Storage.Pack;
using Sharpen;

namespace NGit.Storage.File
{
	/// <summary>A window of data currently stored within a cache.</summary>
	/// <remarks>
	/// A window of data currently stored within a cache.
	/// <p>
	/// All bytes in the window can be assumed to be "immediately available", that is
	/// they are very likely already in memory, unless the operating system's memory
	/// is very low and has paged part of this process out to disk. Therefore copying
	/// bytes from a window is very inexpensive.
	/// </p>
	/// </remarks>
	internal abstract class ByteWindow
	{
		protected internal readonly PackFile pack;

		protected internal readonly long start;

		protected internal readonly long end;

		protected internal ByteWindow(PackFile p, long s, int n)
		{
			pack = p;
			start = s;
			end = start + n;
		}

		internal int Size()
		{
			return (int)(end - start);
		}

		internal bool Contains(PackFile neededFile, long neededPos)
		{
			return pack == neededFile && start <= neededPos && neededPos < end;
		}

		/// <summary>Copy bytes from the window to a caller supplied buffer.</summary>
		/// <remarks>Copy bytes from the window to a caller supplied buffer.</remarks>
		/// <param name="pos">offset within the file to start copying from.</param>
		/// <param name="dstbuf">destination buffer to copy into.</param>
		/// <param name="dstoff">offset within <code>dstbuf</code> to start copying into.</param>
		/// <param name="cnt">
		/// number of bytes to copy. This value may exceed the number of
		/// bytes remaining in the window starting at offset
		/// <code>pos</code>.
		/// </param>
		/// <returns>
		/// number of bytes actually copied; this may be less than
		/// <code>cnt</code> if <code>cnt</code> exceeded the number of
		/// bytes available.
		/// </returns>
		internal int Copy(long pos, byte[] dstbuf, int dstoff, int cnt)
		{
			return Copy((int)(pos - start), dstbuf, dstoff, cnt);
		}

		/// <summary>Copy bytes from the window to a caller supplied buffer.</summary>
		/// <remarks>Copy bytes from the window to a caller supplied buffer.</remarks>
		/// <param name="pos">offset within the window to start copying from.</param>
		/// <param name="dstbuf">destination buffer to copy into.</param>
		/// <param name="dstoff">offset within <code>dstbuf</code> to start copying into.</param>
		/// <param name="cnt">
		/// number of bytes to copy. This value may exceed the number of
		/// bytes remaining in the window starting at offset
		/// <code>pos</code>.
		/// </param>
		/// <returns>
		/// number of bytes actually copied; this may be less than
		/// <code>cnt</code> if <code>cnt</code> exceeded the number of
		/// bytes available.
		/// </returns>
		protected internal abstract int Copy(int pos, byte[] dstbuf, int dstoff, int cnt);

		/// <exception cref="System.IO.IOException"></exception>
		internal abstract void Write(PackOutputStream @out, long pos, int cnt, MessageDigest
			 md);

		/// <exception cref="ICSharpCode.SharpZipLib.SharpZipBaseException"></exception>
		internal int SetInput(long pos, Inflater inf)
		{
			return SetInput((int)(pos - start), inf);
		}

		/// <exception cref="ICSharpCode.SharpZipLib.SharpZipBaseException"></exception>
		protected internal abstract int SetInput(int pos, Inflater inf);
	}
}
