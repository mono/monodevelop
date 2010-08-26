/*
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace GitSharp.Core
{
	/// <summary>
	/// A window of data currently stored within a cache.
	/// <para />
	/// All bytes in the window can be assumed to be "immediately available", that is
	/// they are very likely already in memory, unless the operating system's memory
	/// is very low and has paged part of this process out to disk. Therefore copying
	/// bytes from a window is very inexpensive.
	/// </summary>
	internal abstract class ByteWindow : IDisposable
	{
		protected static readonly byte[] VerifyGarbageBuffer = new byte[2048];

		private readonly PackFile _pack;
		private readonly long _start;
		private readonly long _end;

		protected ByteWindow(PackFile p, long s, long n)
		{
			_pack = p;
			_start = s;
			_end = _start + n;
		}

		internal bool contains(PackFile neededFile, long neededPos)
		{
			return _pack == neededFile && _start <= neededPos && neededPos < _end;
		}

		///	<summary> * Copy bytes from the window to a caller supplied buffer.
		///	</summary>
		///	<param name="pos">offset within the file to start copying from.</param>
		///	<param name="dstbuf">destination buffer to copy into. </param>
		///	<param name="dstoff">
		/// Offset within <paramref name="dstbuf"/> to start copying into.
		/// </param>
		///	<param name="cnt">
		/// number of bytes to copy. This value may exceed the number of
		/// bytes remaining in the window starting at offset
		/// <paramref name="pos" />.
		/// </param>
		///	<returns>
		/// Number of bytes actually copied; this may be less than
		/// <paramref name="cnt" /> if <paramref name="cnt" /> exceeded the number of
		/// bytes available. </returns>
		internal int copy(long pos, byte[] dstbuf, int dstoff, int cnt)
		{
			return copy((int)(pos - _start), dstbuf, dstoff, cnt);
		}

		///	<summary>
		/// Copy bytes from the window to a caller supplied buffer.
		///	</summary>
		///	<param name="pos">
		/// offset within the window to start copying from.
		/// </param>
		///	<param name="dstbuf">destination buffer to copy into.</param>
		///	<param name="dstoff">
		/// offset within <paramref name="dstbuf"/> to start copying into.
		/// </param>
		///	<param name="cnt">
		/// number of bytes to copy. This value may exceed the number of
		/// bytes remaining in the window starting at offset
		/// <paramref name="pos" />.
		/// </param>
		/// <returns> 
		/// Number of bytes actually copied; this may be less than
		/// <paramref name="cnt" /> if <paramref name="cnt" /> exceeded 
		/// the number of bytes available.
		/// </returns>
		protected abstract int copy(int pos, byte[] dstbuf, int dstoff, int cnt);

		///	<summary>
		/// Pump bytes into the supplied inflater as input.
		///	</summary>
		///	<param name="pos">
		/// offset within the file to start supplying input from.
		/// </param>
		///	<param name="dstbuf">
		/// destination buffer the inflater should output decompressed
		/// data to.
		/// </param>
		///	<param name="dstoff">
		/// current offset within <paramref name="dstbuf"/> to inflate into.
		/// </param>
		///	<param name="inf">
		/// the inflater to feed input to. The caller is responsible for
		/// initializing the inflater as multiple windows may need to
		/// supply data to the same inflater to completely decompress
		/// something.
		/// </param>
		///	<returns>
		/// Updated <paramref name="dstoff"/> based on the number of bytes
		/// successfully copied into <paramref name="dstbuf"/> by
		/// <paramref name="inf"/>. If the inflater is not yet finished then
		/// another window's data must still be supplied as input to finish
		/// decompression.
		/// </returns>
		///	<exception cref="InvalidOperationException">
		/// the inflater encountered an invalid chunk of data. Data
		/// stream corruption is likely.
		/// </exception>
		internal int Inflate(long pos, byte[] dstbuf, int dstoff, Inflater inf)
		{
			return Inflate((int)(pos - _start), dstbuf, dstoff, inf);
		}

		///	<summary>
		/// Pump bytes into the supplied inflater as input.
		///	</summary>
		///	<param name="pos">
		/// offset within the file to start supplying input from.
		/// </param>
		///	<param name="dstbuf">
		/// destination buffer the inflater should output decompressed
		/// data to.
		/// </param>
		///	<param name="dstoff">
		/// current offset within <paramref name="dstbuf"/> to inflate into.
		/// </param>
		///	<param name="inf">
		/// the inflater to feed input to. The caller is responsible for
		/// initializing the inflater as multiple windows may need to
		/// supply data to the same inflater to completely decompress
		/// something.
		/// </param>
		///	<returns>
		/// Updated <paramref name="dstoff"/> based on the number of bytes
		/// successfully copied into <paramref name="dstbuf"/> by
		/// <paramref name="inf"/>. If the inflater is not yet finished then
		/// another window's data must still be supplied as input to finish
		/// decompression.
		/// </returns>
		///	<exception cref="InvalidOperationException">
		/// the inflater encountered an invalid chunk of data. Data
		/// stream corruption is likely.
		/// </exception>
		protected abstract int Inflate(int pos, byte[] dstbuf, int dstoff, Inflater inf);

		internal void inflateVerify(long pos, Inflater inf)
		{
			inflateVerify((int)(pos - _start), inf);
		}

		protected abstract void inflateVerify(int pos, Inflater inf);

		public long End
		{
			get { return _end; }
		}

		public long Start
		{
			get { return _start; }
		}

		public PackFile Pack
		{
			get { return _pack; }
		}

		internal int Size
		{
			get { return (int)(_end - _start); }
		}
		
		public virtual void Dispose ()
		{
			_pack.Dispose();
		}
		
	}
}